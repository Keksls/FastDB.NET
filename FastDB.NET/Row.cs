using System;
using System.Collections.Generic;
using System.Text;

namespace FastDB.NET
{
    public struct Row
    {
        private object[] Cells;
        public Table table;

        public Row(Table table)
        {
            this.table = table;
            Cells = null;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (IComparable value in Cells)
                sb.Append(value.ToString()).Append(" ");
            return sb.ToString();
        }

        public T Get<T>(string FieldName) where T : IComparable
        {
            return (T)Cells[table.Fields[FieldName].FieldIndex];
        }

        public T Get<T>(int fieldIndex) where T : IComparable
        {
            return (T)Cells[fieldIndex];
        }

        public object Get(int fieldIndex)
        {
            return Cells[fieldIndex];
        }

        public bool isNull(string FieldName)
        {
            return Cells[table.Fields[FieldName].FieldIndex] == null;
        }

        public bool isNull(int FieldIndex)
        {
            return Cells[FieldIndex] == null;
        }

        public void Set(int fieldIndex, object value)
        {
            Cells[fieldIndex] = value;
        }

        public Row AddField(object value)
        {
            GrowArray();
            Cells[Cells.Length - 1] = value;
            return this;
        }

        private void GrowArray(int nbToGrow = 1)
        {
            object[] newArray = new object[Cells.Length + nbToGrow];
            Array.Copy(Cells, newArray, Cells.Length);
            Cells = newArray;
        }

        public Row RemoveField(int index)
        {
            if (index == Cells.Length - 1)
                ShrinkArray();
            else
            {
                object lastCell = Cells[Cells.Length - 1];
                ShrinkArray();
                for (int i = index; i < Cells.Length; i++)
                    Cells[i] = Cells[i + 1];
                Cells[Cells.Length - 1] = lastCell;
            }
            return this;
        }

        private void ShrinkArray(int nbToShrink = 1)
        {
            object[] newArray = new object[Cells.Length - nbToShrink];
            Array.Copy(Cells, newArray, newArray.Length);
            Cells = newArray;
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