using FastDB.NET;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

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
            Database = new FastDatabase("TestDB", Environment.CurrentDirectory);
            Database.Connect();
        }

        static void TestSelect()
        {
            // Select some rows
            List<Row> rows = Database.Select("TBBT_Persos", "Name", DBCondition.Equal, "Leonard");
            foreach (var row in rows)
                Console.WriteLine(row.ToString());

            rows = Database.Select("TBBT_Persos", "Age", DBCondition.GreaterOrEqual, 30);
            foreach (var row in rows)
                Console.WriteLine(row.ToString());
        }

        static void CreateDatabase()
        {
            Database = new NET.FastDatabase("TestDB", Environment.CurrentDirectory);

            // Create table
            Database.CreateTable("TBBT_Persos");
            Table mytable = Database.GetTable("TBBT_Persos");
            mytable.AddField("Name", FastDBType.String);
            mytable.AddField("Age", FastDBType.Integer);

            // Insert some data
            Database.Insert(mytable.Name, "Leonard", 31);
            Database.Insert(mytable.Name, "Panny", 30);
            Database.Insert(mytable.Name, "Sheldon", 29);
            Database.Insert(mytable.Name, "Rajish", 31);
            Database.Insert(mytable.Name, "Howard", 30);
            Database.Insert(mytable.Name, "Will Witon", 29);
            Database.Insert(mytable.Name, "Stuwart", 32);

            Database.Save();
        }
    }
}