﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.ExceptionServices;

namespace FastDB.NET
{
    public class FastDatabase : SerializableBlock
    {
        public string DatabaseName { get; private set; }
        public string FilePath { get; private set; }
        public Dictionary<string, Table> Tables { get; set; }

        public FastDatabase(string databaseName, string filePath)
        {
            DatabaseName = databaseName;
            FilePath = filePath;
            Tables = new Dictionary<string, Table>();
        }

        public FastDatabase() { }

        /// <summary>
        /// Open the database binary file and bind this instance from it
        /// </summary>
        /// <returns>this instance</returns>
        public FastDatabase Connect()
        {
            // deserialize the binary data from the db file and bind this instance
            SerializableDatabase.Deserialize(this);
            return this;
        }

        /// <summary>
        /// Save this database instance to binary file
        /// </summary>
        /// <returns>this instance</returns>
        public FastDatabase Save()
        {
            // Serialize this instance of database to the binary file
            SerializableDatabase.Serialize(this);
            return this;
        }

        /// <summary>
        /// Check if a table exist
        /// </summary>
        /// <param name="Name">Name of the table to check</param>
        /// <returns>true if table exist</returns>
        public bool TableExist(string Name)
        {
            return Tables.ContainsKey(Name);
        }

        /// <summary>
        /// Create a new table to this database
        /// </summary>
        /// <param name="Name">Name of the Table to create</param>
        /// <returns>return this instance of Database</returns>
        public FastDatabase CreateTable(string Name)
        {
            // check if data already exist
            if (Tables.ContainsKey(Name))
                throw new TableAlreadyExistExceptions();
            else
                Tables.Add(Name, new Table(Name)); // add the table
            return this;
        }

        /// <summary>
        /// Get a table from it name
        /// </summary>
        /// <param name="Name">Name of the table to get</param>
        /// <returns>The table instance</returns>
        public Table GetTable(string Name)
        {
            // CHeck if table exist
            if (!Tables.ContainsKey(Name))
                throw new TableDontExistExceptions();
            // return the table
            return Tables[Name];
        }

        /// <summary>
        /// Insert some data into this database
        /// </summary>
        /// <param name="TableName">The name of the table to insert data</param>
        /// <param name="Values">the data values to insert (must match with the table definition's fields)</param>
        /// <returns>true if success, false if table don't exist OR values mismatch the fields</returns>
        public bool Insert(string TableName, params IComparable[] Values)
        {
            // check if table exist
            if (!Tables.ContainsKey(TableName))
                return false;
            // insert data values
            return Tables[TableName].Insert(Values);
        }

        /// <summary>
        /// Insert some data into this database
        /// </summary>
        /// <param name="TableName">The name of the table to insert data</param>
        /// <param name="Values">the data values to insert (must match with the table definition's fields)</param>
        /// <returns>true if success, false if table don't exist OR values mismatch the fields</returns>
        public bool Insert(string TableName, Dictionary<string, IComparable> Values)
        {
            // check if table exist
            if (!Tables.ContainsKey(TableName))
                return false;
            // insert data values
            return Tables[TableName].Insert(Values);
        }

        /// <summary>
        /// Select some rows b=that match to the condition
        /// </summary>
        /// <param name="TableName">The name of the table to insert data</param>
        /// <param name="FieldName">Field to check</param>
        /// <param name="Condition">Conditional operator</param>
        /// <param name="TargetValue">target value to match with</param>
        /// <returns>a list of all rows that match</returns>
        public List<Row> Select(string TableName, string FieldName, DBCondition Condition, IComparable TargetValue)
        {
            // check if table exist
            if (!Tables.ContainsKey(TableName))
                throw new TableDontExistExceptions();
            // select rows
            return Tables[TableName].Select(FieldName, Condition, TargetValue);
        }

        public override int GetSize()
        {
            return 0;
        }
    }

    public enum FastDBType
    {
        String = 0,
        Integer = 1,
        Float = 2,
        Bool = 3,
        DateTime = 4
    }

    public enum DBCondition
    {
        [Description(">")]
        GreaterThan = 0,
        [Description("=")]
        Equal = 1,
        [Description("<")]
        LessThan = 2,
        [Description(">=")]
        GreaterOrEqual = 5,
        [Description("<=")]
        LessOrEqual = 6,
        [Description("<>")]
        NotEqual = 7
    }
}