using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

namespace FastDB.NET
{
    public class FastDatabase : SerializableBlock
    {
        public string DatabaseName { get; private set; }
        public string FilePath { get; private set; }
        public Dictionary<string, Table> Tables { get; private set; }

        public FastDatabase(string databaseName, string filePath)
        {
            DatabaseName = databaseName;
            FilePath = filePath;
            Tables = new Dictionary<string, Table>();
        }

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
        /// Remove a table from this table
        /// </summary>
        /// <param name="Name">the name of the table to remove</param>
        /// <returns>this instance of FastDatabase</returns>
        public FastDatabase RemoveTable(string Name)
        {
            if (!Tables.ContainsKey(Name))
                throw new TableDontExistExceptions();
            Tables.Remove(Name);
            GC.Collect();
            return this;
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
        public void Insert(string TableName, params object[] Values)
        {
            // insert data values
            GetTable(TableName).Insert(Values);
        }

        /// <summary>
        /// Insert some data into this database
        /// </summary>
        /// <param name="TableName">The name of the table to insert data</param>
        /// <param name="Values">the data values to insert (must match with the table definition's fields)</param>
        /// <returns>true if success, false if table don't exist OR values mismatch the fields</returns>
        public void Insert(string TableName, Dictionary<string, object> Values)
        {
            // insert data values
            GetTable(TableName).Insert(Values);
        }

        /// <summary>
        /// Did this database Exist ?
        /// </summary>
        /// <returns></returns>
        public bool Exists()
        {
            return File.Exists(Path.Combine(FilePath, DatabaseName + ".FastDB"));
        }

        /// <summary>
        /// Close this DB
        /// </summary>
        public void Close()
        {
            Tables.Clear();
            Tables = null;
            DatabaseName = null;
            FilePath = null;
        }

        #region Serialization
        internal override int GetSize()
        {
            return 4; //  nbTables
        }

        internal override unsafe void Serialize()
        {
            WriteInt(Tables.Count);
        }

        internal override unsafe void Deserialize()
        {
            Tables = new Dictionary<string, Table>();
        }
        #endregion
    }
}

public enum FastDBType
{
    Null = 0,
    String = 1,
    Integer = 2,
    Float = 3,
    Bool = 4,
    Date = 5,
    DateTime = 6,
    UnsignedInteger = 7
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
    NotEqual = 7,
    [Description("IN")]
    IN = 8
}

public enum DBConditionLogical
{
    None = -1,
    AND = 0,
    OR = 1
}