using System;
using System.Collections.Generic;

namespace FastDB.NET.CRUD
{
    public sealed class UpdateStatement
    {
        private readonly Table _table;
        private readonly Dictionary<string, object> _assignments;
        private Condition _condition;

        internal UpdateStatement(Table table)
        {
            _table = table ?? throw new ArgumentNullException(nameof(table));
            _assignments = new Dictionary<string, object>(StringComparer.Ordinal);
            _condition = Condition.All;
        }

        public UpdateStatement Where(string fieldName, DBCondition condition, object value)
        {
            _condition = Condition.Create(_table, fieldName, condition, value);
            return this;
        }

        public UpdateStatement Where(Condition condition)
        {
            _condition = condition ?? Condition.All;
            return this;
        }

        public UpdateStatement And(string fieldName, DBCondition condition, object value)
        {
            _condition = _condition.And(Condition.Create(_table, fieldName, condition, value));
            return this;
        }

        public UpdateStatement Or(string fieldName, DBCondition condition, object value)
        {
            _condition = _condition.Or(Condition.Create(_table, fieldName, condition, value));
            return this;
        }

        public UpdateStatement Set(string fieldName, object value)
        {
            _table.GetField(fieldName);
            _assignments[fieldName] = value;
            return this;
        }

        public int Execute()
        {
            return _table.UpdateWhere(_condition, _assignments);
        }
    }
}
