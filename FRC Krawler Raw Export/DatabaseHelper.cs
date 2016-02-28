using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations.Builders;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using SQLite;

namespace FRC_Krawler_Raw_Export
{
    class DatabaseHelper
    {
        private readonly SQLiteConnection connection;

        public DatabaseHelper(string fileName)
        {
            connection = new SQLiteConnection(fileName);

            connection.CreateTable<Event>();
            connection.CreateTable<Team>();
            connection.CreateTable<Metric>();
            connection.CreateTable<MatchData>();
            connection.CreateTable<Robot>();
            connection.CreateTable<RobotEvent>();
            connection.CreateTable<MatchComment>();

            Export();
        }

        private void Export()
        {
            var events = GetAllEvents();
            Event currentEvent;

            if (events.Count > 1)
            {
                foreach (var @event in events)
                {
                    Console.WriteLine($"{events.IndexOf(@event)}: {@event.Name}");
                }
                var index = Convert.ToInt32(Console.ReadLine());
                currentEvent = events[index];
            }
            else
            {
                currentEvent = events[0];
            }

            Console.WriteLine($"Current Event: {currentEvent.Name}");

            var robotEvents = connection.Table<RobotEvent>().Where(re => re.EventId.Equals(currentEvent.Id)).ToList();
            var metrics = connection.Table<Metric>().Where(m => m.GameId.Equals(currentEvent.GameId)).ToList();
            Console.WriteLine($"Number of Metrics: {metrics.Count}");
            Console.WriteLine($"Number of Teams: {robotEvents.Count}");

            List<string> rows = new List<string>();
            var header = new List<string>();
            header.Add("Team");
            header.Add("Match");
            foreach (var metric in metrics)
            {
                header.Add(metric.Name);
            }
            header.Add("Comment");
            rows.Add(string.Join(",", header));
            foreach (var robotEvent in robotEvents)
            {
                var robot = GetRobot(robotEvent.RobotId);
                var team = GetTeam(robot.TeamId);

                Console.WriteLine($"Starting Team {team.Number}");
                // Find all the match numbers
                var allData = connection.Table<MatchData>().Where(data => data.RobotId.Equals(robot.Id)).Where(data => data.EventId.Equals(currentEvent.Id));

                List<int> matchNumbers = new List<int>();
                foreach (var matchData in allData)
                {
                    if (!matchNumbers.Contains(matchData.MatchNumber))
                    {
                        matchNumbers.Add(matchData.MatchNumber);
                    }
                }

                foreach (var matchNumber in matchNumbers)
                {
                    var row = new List<string>();
                    row.Add($"{team.Number}");
                    row.Add($"{matchNumber}");

                    foreach (var metric in metrics)
                    {
                        var rowData = connection.Table<MatchData>()
                            .Where(md => md.EventId.Equals(currentEvent.Id))
                            .Where(md => md.RobotId.Equals(robot.Id))
                            .Where(md => md.MetricId.Equals(metric.Id)).FirstOrDefault(md => md.MatchNumber.Equals(matchNumber));

                        if (row != null)
                        {
                            row.Add($"\"{GetValueFromJson(metric, rowData.Data)}\"");
                        }
                    }
                    var comment = connection
                        .Table<MatchComment>()
                        .Where(mc => mc.EventId.Equals(currentEvent.Id))
                        .Where(mc => mc.RobotId.Equals(robot.Id)).FirstOrDefault(mc => mc.MatchNumber.Equals(matchNumber));
                    if (comment != null)
                    {
                        row.Add(comment.Comment);
                    }
                    rows.Add(string.Join(",", row));
                }
            }
            System.IO.File.WriteAllLines("RawExport.csv", rows);
        }

        public string GetValueFromJson(Metric metric, string json)
        {
            dynamic data = JObject.Parse(json);
            if (metric.Type < 3)
            {
                return data.value;
            }
            else
            {
                dynamic range = JObject.Parse(metric.Data);
                JArray dataIndexes = data.values;
                JArray valueArray = range.values;
                List<string> selected = new List<string>();

                foreach (var dataIndex in dataIndexes.ToList())
                {
                    selected.Add(valueArray.ToList()[(int)dataIndex].ToString());
                }
                return string.Join(", ", selected);
            }
        }

        public Robot GetRobot(int id) => connection.Get<Robot>(id);

        public Team GetTeam(int number) => connection.Get<Team>(number);

        public Metric GetMetMetric(int id) => connection.Get<Metric>(id);

        public List<Event> GetAllEvents() => connection.Table<Event>().ToList();

        public List<Team> GetAllTeams() => connection.Table<Team>().ToList();

        public List<Metric> GetAllMetrics() => connection.Table<Metric>().ToList();
    }

    [Table("EVENT")]
    class Event
    {
        [Column("_id")]
        public int Id { get; set; }
        [Column("NAME")]
        public string Name { get; set; }
        [Column("GAME_ID")]
        public int GameId { get; set; }
    }

    [Table("TEAM")]
    class Team
    {
        [Column("NUMBER"), PrimaryKey]
        public int Number { get; set; }
        [Column("NAME")]
        public string Name { get; set; }
    }

    [Table("METRIC")]
    class Metric
    {
        [Column("_id"), PrimaryKey]
        public int Id { get; set; }

        [Column("NAME")]
        public string Name { get; set; }

        [Column("GAME_ID")]
        public int GameId { get; set; }

        [Column("TYPE")]
        public int Type { get; set; }

        [Column("CATEGORY")]
        public int Category { get; set; }

        [Column("DATA")]
        public String Data { get; set; }
    }

    [Table("MATCH_DATA")]
    class MatchData
    {
        [Column("_id"), PrimaryKey]
        public int Id { get; set; }

        [Column("EVENT_ID")]
        public int EventId { get; set; }

        [Column("METRIC_ID")]
        public int MetricId { get; set; }

        [Column("ROBOT_ID")]
        public int RobotId { get; set; }

        [Column("MATCH_NUMBER")]
        public int MatchNumber { get; set; }

        [Column("DATA")]
        public String Data { get; set; }
    }

    [Table("ROBOT")]
    class Robot
    {
        [Column("_id"), PrimaryKey]
        public int Id { get; set; }

        [Column("TEAM_ID")]
        public int TeamId { get; set; }
    }

    [Table("ROBOT_EVENT")]
    class RobotEvent
    {
        [Column("_id"), PrimaryKey]
        public int Id { get; set; }

        [Column("ROBOT_ID")]
        public int RobotId { get; set; }

        [Column("EVENT_ID")]
        public int EventId { get; set; }
    }

    [Table("MATCH_COMMENT")]
    class MatchComment
    {
        [Column("MATCH_NUMBER")]
        public int MatchNumber { get; set; }

        [Column("ROBOT_ID")]
        public int RobotId { get; set; }

        [Column("EVENT_ID")]
        public int EventId { get; set; }

        [Column("COMMENT")]
        public string Comment { get; set; }
    }
}
