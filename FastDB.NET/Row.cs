using System;
using System.Collections.Generic;
using System.Text;

namespace FastDB.NET
{
    public struct Row
    {
        private object[] Cells;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (IComparable value in Cells)
                sb.Append(value.ToString()).Append(" ");
            return sb.ToString();
        }

        public T Get<T>(int fieldIndex) where T : IComparable
        {
            return (T)Cells[fieldIndex];
        }

        public object Get(int fieldIndex)
        {
            return Cells[fieldIndex];
        }

        public void Set(int fieldIndex, object value)
        {
            Cells[fieldIndex] = value;
        }

        public void AddField(object value)
        {
            Array.Resize<object>(ref Cells, Cells.Length);
            Cells[Cells.Length - 1] = value;
        }

        public Row SetCells(object[] values)
        {
            Cells = values;
            return this;
        }

        public object[] GetCells()
        {
            return Cells;
        }

        public void InitializeCells(int nbFields)
        {
            Cells = new object[nbFields];
        }
    }
}