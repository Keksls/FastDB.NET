using System;
using System.Collections.Generic;
using System.Linq;

namespace FastDB.NET.CRUD
{
    public sealed class Select
    {
        private readonly Table _table;
        private readonly int[] _fieldsToSelect;
        private Condition _condition;

        public Select(Table table, params string[] fields)
        {
            _table = table ?? throw new ArgumentNullException(nameof(table));
            _fieldsToSelect = table.ResolveSelectedFields(fields);
            _condition = Condition.All;
        }

        public Condition CreateCondition(string fieldName, DBCondition condition, object value)
        {
            return Condition.Create(_table, fieldName, condition, value);
        }

        public Select Where(string fieldName, DBCondition condition, object value)
        {
            _condition = CreateCondition(fieldName, condition, value);
            return this;
        }

        public Select Where(Condition condition)
        {
            _condition = condition ?? Condition.All;
            return this;
        }

        public Select And(string fieldName, DBCondition condition, object value)
        {
            _condition = _condition.And(CreateCondition(fieldName, condition, value));
            return this;
        }

        public Select Or(string fieldName, DBCondition condition, object value)
        {
            _condition = _condition.Or(CreateCondition(fieldName, condition, value));
            return this;
        }

        public List<Row> Execute()
        {
            return Execute(_condition);
        }

        public List<Row> Execute(Condition condition)
        {
            return _table.Execute(condition ?? Condition.All)
                .Select(row => _table.ProjectRow(row, _fieldsToSelect))
                .ToList();
        }

        public List<T> Execute<T>()
        {
            return Execute<T>(_condition);
        }

        public List<T> Execute<T>(Condition condition)
        {
            if (_fieldsToSelect.Length != 1)
                throw new InvalideQueryException("Typed select requires exactly one selected field.");

            int index = _fieldsToSelect[0];
            return _table.Execute(condition ?? Condition.All)
                .Select(row => row.Get<T>(index))
                .ToList();
        }

        public Row FirstOrDefault()
        {
            return _table.Execute(_condition).Select(row => _table.ProjectRow(row, _fieldsToSelect)).FirstOrDefault();
        }

        public int Count()
        {
            return _table.Execute(_condition).Count;
        }
    }
}
