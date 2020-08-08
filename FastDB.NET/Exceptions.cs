using System;
using System.Collections.Generic;
using System.Text;

namespace FastDB.NET
{
    public class TableAlreadyExistExceptions : Exception
    {
        public override string Message => "A table with the same name already exist in this database.";
    }

    public class TableDontExistExceptions : Exception
    {
        public override string Message => "The table you asked don't exist in this database.";
    }

    public class FieldAlreadyExistExceptions : Exception
    {
        public override string Message => "A field with the same name already exist in this table.";
    }

    public class FieldDontExistExceptions : Exception
    {
        public override string Message => "The field you asked don't exist in this table.";
    }

    public class InvalideQueryException : Exception
    {
        public override string Message => "The condition you are trying to process in invalide.";
    }
}
