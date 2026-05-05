using System;
using System.Collections.Generic;
using System.Text;

namespace FastDB.NET
{
    public sealed class Row
    {
        private object[] _cells;
        private readonly IReadOnlyDictionary<string, int> _fieldLookup;

        public Table Table { get; internal set; }
        public int CellCount { get { return _cells.Length; } }

        public Row()
        {
            _cells = Array.Empty<object>();
            _fieldLookup = null;
        }

        public Row(Table table)
        {
            Table = table;
            _cells = Array.Empty<object>();
            _fieldLookup = null;
        }

        internal Row(Table table, object[] cells)
        {
            Table = table;
            _cells = cells;
            _fieldLookup = null;
        }

        internal Row(object[] cells, IReadOnlyDictionary<string, int> fieldLookup)
        {
            _cells = cells;
            _fieldLookup = fieldLookup;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _cells.Length; i++)
            {
                if (i > 0)
                    sb.Append(' ');
                sb.Append(_cells[i] ?? "null");
            }
            return sb.ToString();
        }

        public T Get<T>(string fieldName)
        {
            return (T)Get(fieldName);
        }

        public T Get<T>(int fieldIndex)
        {
            return (T)_cells[fieldIndex];
        }

        public byte[] GetByteArray(string fieldName)
        {
            return (byte[])Get(fieldName);
        }

        public byte[] GetByteArray(int fieldIndex)
        {
            return (byte[])_cells[fieldIndex];
        }

        public object Get(string fieldName)
        {
            return _cells[ResolveIndex(fieldName)];
        }

        public object Get(int fieldIndex)
        {
            return _cells[fieldIndex];
        }

        public bool IsNull(string fieldName)
        {
            return Get(fieldName) == null;
        }

        public bool IsNull(int fieldIndex)
        {
            return _cells[fieldIndex] == null;
        }

        public bool isNull(string fieldName)
        {
            return IsNull(fieldName);
        }

        public bool isNull(int fieldIndex)
        {
            return IsNull(fieldIndex);
        }

        public void Set(string fieldName, object value)
        {
            Set(ResolveIndex(fieldName), value);
        }

        public void Set(int fieldIndex, object value)
        {
            if (Table == null)
            {
                _cells[fieldIndex] = value;
                return;
            }

            Table.SetCell(this, fieldIndex, value);
        }

        public Row AddField(object value)
        {
            GrowArray();
            _cells[_cells.Length - 1] = value;
            return this;
        }

        public Row RemoveField(int index)
        {
            if (index < 0 || index >= _cells.Length)
                throw new IndexOutOfRangeException();

            object[] newArray = new object[_cells.Length - 1];
            if (index > 0)
                Array.Copy(_cells, 0, newArray, 0, index);
            if (index < _cells.Length - 1)
                Array.Copy(_cells, index + 1, newArray, index, _cells.Length - index - 1);
            _cells = newArray;
            return this;
        }

        public Row SetCells(object[] values)
        {
            _cells = values ?? throw new ArgumentNullException(nameof(values));
            return this;
        }

        public object[] GetCells()
        {
            return _cells;
        }

        public void InitializeCells(int nbFields)
        {
            if (nbFields < 0)
                throw new ArgumentOutOfRangeException(nameof(nbFields));
            _cells = new object[nbFields];
        }

        internal void SetDirect(int fieldIndex, object value)
        {
            _cells[fieldIndex] = value;
        }

        private int ResolveIndex(string fieldName)
        {
            if (fieldName == null)
                throw new ArgumentNullException(nameof(fieldName));

            if (Table != null)
                return Table.GetFieldIndex(fieldName);

            if (_fieldLookup != null && _fieldLookup.TryGetValue(fieldName, out int index))
                return index;

            throw new FieldDontExistExceptions(fieldName);
        }

        private void GrowArray(int nbToGrow = 1)
        {
            object[] newArray = new object[_cells.Length + nbToGrow];
            Array.Copy(_cells, newArray, _cells.Length);
            _cells = newArray;
        }
    }
}
