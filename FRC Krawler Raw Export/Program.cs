using System;
using System.Data.SQLite;

namespace FRC_Krawler_Raw_Export
{
    internal class Program
    {
        private string fileName;

        public Program(string[] args)
        {
            fileName = args.Length > 0 ? args[0] : "frc-krawler-database-v3.sqlite";

            var database = new DatabaseHelper(fileName);

            Console.Write("Press Any Key...");
            Console.ReadKey();
        }
      
        private static void Main(string[] args)
        {
            new Program(args);
        }
    }
}
