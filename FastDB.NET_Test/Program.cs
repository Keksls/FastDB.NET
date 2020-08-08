using FastDB.NET;
using FastDB.NET.CRUD;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace FastDB.NET_Test
{
    class Program
    {
        static FastDatabase Database;

        static void Main(string[] args)
        {
            CreateDatabase();
            LoadDatabase();
            TestSelect();

            Console.ReadKey();
        }

        static void LoadDatabase()
        {
            Console.WriteLine("=================== Loading Database");
            Database = new FastDatabase("TestDB", Environment.CurrentDirectory);
            Console.Write("Deserialization : ");
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Database.Connect();
            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds + " ms");
            DisplayDBInfos();
        }

        static void DisplayDBInfos()
        {
            int nbRows = 0;
            foreach (var table in Database.Tables)
                nbRows += table.Value.NbRows;
            Console.WriteLine("================== INFOS ===============");
            Console.WriteLine("= DBName   : " + Database.DatabaseName);
            Console.WriteLine("= DBPath   : " + Database.FilePath);
            Console.WriteLine("= NbTables : " + Database.Tables.Count);
            Console.WriteLine("= NbRows   : " + nbRows);
            Console.WriteLine("= DBSize   : " + new FileInfo(Path.Combine(Database.FilePath, Database.DatabaseName + ".FastDB")).Length.ToPrettySize());
            Console.WriteLine("= RAM      : " + Process.GetCurrentProcess().PrivateMemorySize64.ToPrettySize());
            Console.WriteLine("========================================");
        }

        static void TestSelect()
        {
            // Select some rows
            Console.WriteLine("=================== Selecting some Rows");
            Console.Write("Preparing SELECT Statement : ");
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Select selector = Database.GetTable("TBBT_Persos").Select("Name", "Age").Where("Name", DBCondition.Equal, "Penny");
            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds + " ms");
            sw.Reset();
            sw.Start();
            Console.Write("Selecting rows : ");
            List<Row> rows = selector.Execute();
            Console.WriteLine(sw.ElapsedMilliseconds + " ms");
            Console.WriteLine("Nb Rows Selected : " + rows.Count);
            rows.Clear();
        }

        static void CreateDatabase()
        {
            Console.WriteLine("========= Creating Database");
            Database = new FastDatabase("TestDB", Environment.CurrentDirectory);

            // Create table
            Console.Write("Inserting rows : ");
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Database.CreateTable("TBBT_Persos");
            Table mytable = Database.GetTable("TBBT_Persos");
            mytable.AddField("Name", FastDBType.String);
            mytable.AddField("Age", FastDBType.Integer);

            // Insert some data
            for (int i = 0; i < 1000000; i++)
            {
                Database.Insert(mytable.Name, "Leonard", 31);
                Database.Insert(mytable.Name, "Penny", 30);
                Database.Insert(mytable.Name, "Sheldon", 29);
                Database.Insert(mytable.Name, "Rajish", 31);
                Database.Insert(mytable.Name, "Howard", 30);
                Database.Insert(mytable.Name, "Will Witon", 29);
                Database.Insert(mytable.Name, "Stuwart", 32);
            }

            // Create table
            Database.CreateTable("TBBT_Accesories");
            mytable = Database.GetTable("TBBT_Accesories");
            mytable.AddField("Name", FastDBType.String);
            mytable.AddField("Date", FastDBType.DateTime);

            // Insert some data
            for (int i = 0; i < 1000000; i++)
            {
                Database.Insert(mytable.Name, "Wine", DateTime.Now);
                Database.Insert(mytable.Name, "Teremine", DateTime.Now);
            }
            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds + " ms");
            sw.Reset();
            sw.Start();
            Console.Write("Serialization : ");
            Database.Save();
            Console.WriteLine(sw.ElapsedMilliseconds + " ms");

            DisplayDBInfos();
        }
    }

    public static class Ext
    {
        private const long OneKb = 1024;
        private const long OneMb = OneKb * 1024;
        private const long OneGb = OneMb * 1024;
        private const long OneTb = OneGb * 1024;

        public static string ToPrettySize(this int value, int decimalPlaces = 2)
        {
            return ((long)value).ToPrettySize(decimalPlaces);
        }

        public static string ToPrettySize(this long value, int decimalPlaces = 2)
        {
            var asTb = Math.Round((double)value / OneTb, decimalPlaces);
            var asGb = Math.Round((double)value / OneGb, decimalPlaces);
            var asMb = Math.Round((double)value / OneMb, decimalPlaces);
            var asKb = Math.Round((double)value / OneKb, decimalPlaces);
            string chosenValue = asTb > 1 ? string.Format("{0}Tb", asTb)
                : asGb > 1 ? string.Format("{0}Gb", asGb)
                : asMb > 1 ? string.Format("{0}Mb", asMb)
                : asKb > 1 ? string.Format("{0}Kb", asKb)
                : string.Format("{0}B", Math.Round((double)value, decimalPlaces));
            return chosenValue;
        }
    }
}