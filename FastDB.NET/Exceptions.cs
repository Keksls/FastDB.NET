using System;

namespace FastDB.NET
{
    public class TableAlreadyExistExceptions : Exception
    {
        public TableAlreadyExistExceptions() : base("A table with the same name already exists in this database.") { }
        public TableAlreadyExistExceptions(string tableName) : base("Table '" + tableName + "' already exists in this database.") { }
    }

    public class TableDontExistExceptions : Exception
    {
        public TableDontExistExceptions() : base("The requested table does not exist in this database.") { }
        public TableDontExistExceptions(string tableName) : base("Table '" + tableName + "' does not exist in this database.") { }
    }

    public class FieldAlreadyExistExceptions : Exception
    {
        public FieldAlreadyExistExceptions() : base("A field with the same name already exists in this table.") { }
        public FieldAlreadyExistExceptions(string fieldName) : base("Field '" + fieldName + "' already exists in this table.") { }
    }

    public class FieldDontExistExceptions : Exception
    {
        public FieldDontExistExceptions() : base("The requested field does not exist in this table.") { }
        public FieldDontExistExceptions(string fieldName) : base("Field '" + fieldName + "' does not exist in this table.") { }
    }

    public class InvalideQueryException : Exception
    {
        public InvalideQueryException() : base("The query is invalid.") { }
        public InvalideQueryException(string message) : base(message) { }
    }
}
