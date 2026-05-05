using System;

namespace FastDB.NET.CRUD
{
    public sealed class DeleteStatement
    {
        private readonly Table _table;
        private Condition _condition;

        internal DeleteStatement(Table table)
        {
            _table = table ?? throw new ArgumentNullException(nameof(table));
            _condition = Condition.All;
        }

        public DeleteStatement Where(string fieldName, DBCondition condition, object value)
        {
            _condition = Condition.Create(_table, fieldName, condition, value);
            return this;
        }

        public DeleteStatement Where(Condition condition)
        {
            _condition = condition ?? Condition.All;
            return this;
        }

        public DeleteStatement And(string fieldName, DBCondition condition, object value)
        {
            _condition = _condition.And(Condition.Create(_table, fieldName, condition, value));
            return this;
        }

        public DeleteStatement Or(string fieldName, DBCondition condition, object value)
        {
            _condition = _condition.Or(Condition.Create(_table, fieldName, condition, value));
            return this;
        }

        public int Execute()
        {
            return _table.DeleteWhere(_condition);
        }
    }
}
