using System;
using System.Collections.Generic;

namespace FastDB.NET.CRUD
{
    public class Select
    {
        Table Table;
        int[] FieldsToSelect;
        Condition Condition;

        /// <summary>
        /// Create a new Select Statement
        /// </summary>
        /// <param name="table">The table to select</param>
        /// <param name="fields">the fields to select</param>
        public Select(Table table, params string[] fields)
        {
            Table = table;
            FieldsToSelect = new int[fields.Length];
            for (int i = 0; i < fields.Length; i++)
                FieldsToSelect[i] = table.Fields[fields[i]].FieldIndex;
        }

        /// <summary>
        /// Create a condition to this statement
        /// </summary>
        /// <param name="FieldName">The name of the field to filter</param>
        /// <param name="Condition">the logical operator of the condition</param>
        /// <param name="Value">the target value od this condition</param>
        /// <returns>The condition instance</returns>
        public Condition CreateCondition(string FieldName, DBCondition Condition, object Value)
        {
            int fieldIndex = Table.Fields[FieldName].FieldIndex;
            switch (Condition)
            {
                default:
                case DBCondition.Equal:
                    return new Condition() { FieldIndex = fieldIndex, Function = (TargetValue) => { return TargetValue.CompareTo(Value) == 0; } };

                case DBCondition.GreaterThan:
                    return new Condition() { FieldIndex = fieldIndex, Function = (TargetValue) => { return TargetValue.CompareTo(Value) > 0; } };

                case DBCondition.LessThan:
                    return new Condition() { FieldIndex = fieldIndex, Function = (TargetValue) => { return TargetValue.CompareTo(Value) < 0; } };

                case DBCondition.GreaterOrEqual:
                    return new Condition() { FieldIndex = fieldIndex, Function = (TargetValue) => { return TargetValue.CompareTo(Value) >= 0; } };

                case DBCondition.LessOrEqual:
                    return new Condition() { FieldIndex = fieldIndex, Function = (TargetValue) => { return TargetValue.CompareTo(Value) <= 0; } };

                case DBCondition.NotEqual:
                    return new Condition() { FieldIndex = fieldIndex, Function = (TargetValue) => { return TargetValue.CompareTo(Value) != 0; } };
            }
        }

        /// <summary>
        /// Add a condition to this statement
        /// </summary>
        /// <param name="FieldName">The name of the field to filter</param>
        /// <param name="Condition">the logical operator of the condition</param>
        /// <param name="Value">the target value od this condition</param>
        /// <returns>This statement</returns>
        public Select Where(string FieldName, DBCondition DBCondition, IComparable Value)
        {
            Condition = CreateCondition(FieldName, DBCondition, Value);
            return this;
        }

        /// <summary>
        /// Execute this statement
        /// </summary>
        /// <returns>Rows that matches with the conditions</returns>
        public List<Row> Execute()
        {
            return Execute(Condition);
        }

        /// <summary>
        /// Execute this statement
        /// </summary>
        /// <param name="condition">the condition to use for this select</param>
        /// <returns>Rows that matches with the conditions</returns>
        public List<Row> Execute(Condition condition)
        {
            List<Row> retVal = new List<Row>();
            int i = 0;
            foreach (Row row in Table.Rows)
                if (condition.Function((IComparable)row.Get(condition.FieldIndex)))
                {
                    Row r = new Row();
                    r.InitializeCells(FieldsToSelect.Length);
                    for (i = 0; i < FieldsToSelect.Length; i++)
                        r.Set(FieldsToSelect[i], row.Get(FieldsToSelect[i]));
                    retVal.Add(r);
                }
            return retVal;
        }

        /// <summary>
        /// Execute this statement
        /// </summary>
        /// <typeparam name="T">the type of the field to select</typeparam>
        /// <returns>Rows that matches with the conditions</returns>
        public List<T> Execute<T>() where T : IComparable
        {
            return Execute<T>(Condition);
        }

        /// <summary>
        /// Execute this statement
        /// </summary>
        /// <typeparam name="T">the type of the field to select</typeparam>
        /// <param name="condition">the condition to use for this select</param>
        /// <returns>Rows that matches with the conditions</returns>
        public List<T> Execute<T>(Condition condition) where T : IComparable
        {
            if (FieldsToSelect.Length != 1)
                throw new InvalideQueryException();

            int index = FieldsToSelect[0];
            List<T> retVal = new List<T>();
            foreach (Row row in Table.Rows)
                if (condition.Function((IComparable)row.Get(condition.FieldIndex)))
                    retVal.Add(row.Get<T>(index));
            return retVal;
        }
    }
}