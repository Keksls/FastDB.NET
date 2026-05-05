using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace FastDB.NET.CRUD
{
    public sealed class Condition
    {
        private readonly Func<Row, bool> _predicate;
        private readonly Condition _left;
        private readonly Condition _right;
        private readonly DBConditionLogical _logical;
        private readonly bool _isCustom;

        public static readonly Condition All = new Condition(row => true, isAll: true);

        public int FieldIndex { get; private set; }
        public string FieldName { get; private set; }
        public DBCondition Operator { get; private set; }
        public object Value { get; private set; }
        public bool IsAll { get; }

        private Condition(Func<Row, bool> predicate, bool isAll = false)
        {
            _predicate = predicate;
            _isCustom = true;
            _logical = DBConditionLogical.None;
            IsAll = isAll;
        }

        private Condition(string fieldName, int fieldIndex, DBCondition dbCondition, object value)
        {
            FieldName = fieldName;
            FieldIndex = fieldIndex;
            Operator = dbCondition;
            Value = value;
            _logical = DBConditionLogical.None;
        }

        private Condition(Condition left, Condition right, DBConditionLogical logical)
        {
            _left = left ?? All;
            _right = right ?? All;
            _logical = logical;
        }

        public static Condition Create(Table table, string fieldName, DBCondition dbCondition, object value)
        {
            if (table == null)
                throw new ArgumentNullException(nameof(table));

            Field field = table.GetField(fieldName);
            object normalized = NormalizeConditionValue(field.Type, dbCondition, value);
            return new Condition(field.Name, field.FieldIndex, dbCondition, normalized);
        }

        public static Condition Custom(Func<Row, bool> predicate)
        {
            return new Condition(predicate ?? throw new ArgumentNullException(nameof(predicate)));
        }

        public Condition And(Condition other)
        {
            return new Condition(this, other, DBConditionLogical.AND);
        }

        public Condition Or(Condition other)
        {
            return new Condition(this, other, DBConditionLogical.OR);
        }

        public bool Matches(Row row)
        {
            if (IsAll)
                return true;
            if (_isCustom)
                return _predicate(row);
            if (_logical == DBConditionLogical.AND)
                return _left.Matches(row) && _right.Matches(row);
            if (_logical == DBConditionLogical.OR)
                return _left.Matches(row) || _right.Matches(row);

            object current = row.Get(FieldIndex);
            return Compare(current, Operator, Value);
        }

        internal bool TryCreateIndexedProbe(Table table, out string fieldName, out IReadOnlyList<object> values)
        {
            fieldName = null;
            values = null;

            if (IsAll || _isCustom)
                return false;

            if (_logical == DBConditionLogical.AND)
                return _left.TryCreateIndexedProbe(table, out fieldName, out values)
                    || _right.TryCreateIndexedProbe(table, out fieldName, out values);

            if (_logical == DBConditionLogical.OR)
                return false;

            if (Operator == DBCondition.Equal)
            {
                fieldName = FieldName;
                values = new[] { Value };
                return true;
            }

            if (Operator == DBCondition.IN && Value is IReadOnlyList<object> list)
            {
                fieldName = FieldName;
                values = list;
                return true;
            }

            return false;
        }

        private static object NormalizeConditionValue(FastDBType type, DBCondition dbCondition, object value)
        {
            if (dbCondition != DBCondition.IN)
                return Table.NormalizeValue(type, value, allowNull: true);

            if (value is string)
                return new object[] { Table.NormalizeValue(type, value, allowNull: true) };

            if (!(value is IEnumerable enumerable))
                return new object[] { Table.NormalizeValue(type, value, allowNull: true) };

            List<object> values = new List<object>();
            foreach (object item in enumerable)
                values.Add(Table.NormalizeValue(type, item, allowNull: true));
            return values;
        }

        private static bool Compare(object current, DBCondition dbCondition, object value)
        {
            if (dbCondition == DBCondition.IN)
            {
                if (value is IEnumerable<object> values)
                    return values.Any(item => Table.ValuesEqual(current, item));
                return Table.ValuesEqual(current, value);
            }

            if (dbCondition == DBCondition.Equal)
                return Table.ValuesEqual(current, value);
            if (dbCondition == DBCondition.NotEqual)
                return !Table.ValuesEqual(current, value);

            if (current == null || value == null)
                return false;

            if (!(current is IComparable comparable))
                throw new InvalideQueryException("Field value does not implement IComparable.");

            int comparison = comparable.CompareTo(value);
            switch (dbCondition)
            {
                case DBCondition.GreaterThan:
                    return comparison > 0;
                case DBCondition.LessThan:
                    return comparison < 0;
                case DBCondition.GreaterOrEqual:
                    return comparison >= 0;
                case DBCondition.LessOrEqual:
                    return comparison <= 0;
                default:
                    return false;
            }
        }
    }
}
