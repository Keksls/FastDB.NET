using FastDB.NET.CRUD;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace FastDB.NET
{
    public sealed class Table
    {
        private readonly Dictionary<string, Field> _fields;
        private readonly List<Field> _fieldOrder;
        private readonly List<Row> _rows;
        private readonly Dictionary<string, TableIndex> _indexes;

        public string Name { get; internal set; }
        public IReadOnlyDictionary<string, Field> Fields { get { return _fields; } }
        public IReadOnlyList<Field> FieldDefinitions { get { return _fieldOrder; } }
        public IReadOnlyList<Row> Rows { get { return _rows; } }
        public IReadOnlyCollection<string> IndexedFields { get { return _indexes.Keys.ToArray(); } }
        public int NbFields { get { return _fieldOrder.Count; } }
        public int NbRows { get { return _rows.Count; } }

        internal FastDatabase Database { get; set; }

        public Table(string name)
        {
            ValidateName(name, nameof(name));
            Name = name;
            _fields = new Dictionary<string, Field>(StringComparer.Ordinal);
            _fieldOrder = new List<Field>();
            _rows = new List<Row>();
            _indexes = new Dictionary<string, TableIndex>(StringComparer.Ordinal);
        }

        public bool FieldExists(string name)
        {
            return _fields.ContainsKey(name);
        }

        public int GetFieldIndex(string name)
        {
            if (!_fields.TryGetValue(name, out Field field))
                throw new FieldDontExistExceptions(name);
            return field.FieldIndex;
        }

        public Field GetField(string name)
        {
            if (!_fields.TryGetValue(name, out Field field))
                throw new FieldDontExistExceptions(name);
            return field;
        }

        public Table AddField(string name, FastDBType type, object defaultValue = null)
        {
            ValidateName(name, nameof(name));
            if (_fields.ContainsKey(name))
                throw new FieldAlreadyExistExceptions(name);

            object normalizedDefault = NormalizeValue(type, defaultValue, allowNull: true) ?? GetDefaultValue(type);
            Field field = new Field(name, type, normalizedDefault, _fieldOrder.Count);
            _fields.Add(name, field);
            _fieldOrder.Add(field);

            for (int i = 0; i < _rows.Count; i++)
                _rows[i].AddField(CopyValue(normalizedDefault));

            MarkChanged();
            return this;
        }

        public Table RemoveField(string name)
        {
            Field field = GetField(name);
            int index = field.FieldIndex;

            _fields.Remove(name);
            _fieldOrder.RemoveAt(index);
            _indexes.Remove(name);

            for (int i = 0; i < _rows.Count; i++)
                _rows[i].RemoveField(index);

            ReindexFields();
            RebuildIndexes();
            MarkChanged();
            return this;
        }

        public Table RenameField(string oldName, string newName)
        {
            ValidateName(newName, nameof(newName));
            Field field = GetField(oldName);
            if (_fields.ContainsKey(newName))
                throw new FieldAlreadyExistExceptions(newName);

            _fields.Remove(oldName);
            field.Name = newName;
            _fields.Add(newName, field);

            if (_indexes.TryGetValue(oldName, out TableIndex index))
            {
                _indexes.Remove(oldName);
                index.FieldName = newName;
                _indexes.Add(newName, index);
            }

            MarkChanged();
            return this;
        }

        public bool Insert(params object[] values)
        {
            if (values == null || values.Length != NbFields)
                return false;

            object[] cells = new object[NbFields];
            for (int i = 0; i < NbFields; i++)
                cells[i] = NormalizeValue(_fieldOrder[i].Type, values[i], allowNull: true) ?? CopyValue(_fieldOrder[i].DefaultValue);

            Row row = new Row(this, cells);
            AddRow(row, updateIndexes: true);
            MarkChanged();
            return true;
        }

        public bool Insert(Dictionary<string, object> values)
        {
            return Insert((IDictionary<string, object>)values);
        }

        public bool Insert(IDictionary<string, object> values)
        {
            if (values == null)
                return false;

            foreach (string key in values.Keys)
                if (!_fields.ContainsKey(key))
                    return false;

            object[] cells = new object[NbFields];
            for (int i = 0; i < NbFields; i++)
            {
                Field field = _fieldOrder[i];
                object value = values.TryGetValue(field.Name, out object supplied)
                    ? supplied
                    : CopyValue(field.DefaultValue);
                cells[i] = NormalizeValue(field.Type, value, allowNull: true) ?? CopyValue(field.DefaultValue);
            }

            Row row = new Row(this, cells);
            AddRow(row, updateIndexes: true);
            MarkChanged();
            return true;
        }

        public bool UnsafeInsert(params object[] values)
        {
            if (values == null || values.Length != NbFields)
                return false;

            Row row = new Row(this, values);
            AddRow(row, updateIndexes: true);
            MarkChanged();
            return true;
        }

        public Select Select(params string[] fieldNames)
        {
            return new Select(this, fieldNames);
        }

        public Select GetSelector(params string[] fieldNames)
        {
            return Select(fieldNames);
        }

        public Select GetSelectorAllFields()
        {
            return Select(_fieldOrder.Select(field => field.Name).ToArray());
        }

        public UpdateStatement Update()
        {
            return new UpdateStatement(this);
        }

        public DeleteStatement Delete()
        {
            return new DeleteStatement(this);
        }

        public Table CreateIndex(string fieldName, bool unique = false)
        {
            Field field = GetField(fieldName);
            if (field.Type == FastDBType.ByteArray)
                throw new InvalidOperationException("ByteArray fields cannot be indexed because byte[] equality is reference-based.");

            TableIndex index = new TableIndex(fieldName, field.FieldIndex, unique);
            index.Rebuild(_rows);
            _indexes[fieldName] = index;
            MarkChanged();
            return this;
        }

        public Table DropIndex(string fieldName)
        {
            _indexes.Remove(fieldName);
            MarkChanged();
            return this;
        }

        public Table RebuildIndexes()
        {
            foreach (TableIndex index in _indexes.Values)
                index.FieldIndex = GetFieldIndex(index.FieldName);
            foreach (TableIndex index in _indexes.Values)
                index.Rebuild(_rows);
            return this;
        }

        public bool TryGetByIndex(string fieldName, object value, out Row row)
        {
            row = null;
            if (!_indexes.TryGetValue(fieldName, out TableIndex index))
                return false;

            object normalized = NormalizeValue(GetField(fieldName).Type, value, allowNull: true);
            IReadOnlyList<Row> matches = index.Find(normalized);
            if (matches.Count == 0)
                return false;

            row = matches[0];
            return true;
        }

        public List<Row> Find(string fieldName, DBCondition condition, object value)
        {
            return Select().Where(fieldName, condition, value).Execute();
        }

        public int RemoveWhere(Condition condition)
        {
            return Delete().Where(condition).Execute();
        }

        public void RemoveAt(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _rows.Count)
                throw new ArgumentOutOfRangeException(nameof(rowIndex));

            Row row = _rows[rowIndex];
            foreach (TableIndex index in _indexes.Values)
                index.Remove(row);
            _rows.RemoveAt(rowIndex);
            MarkChanged();
        }

        public void ClearRows()
        {
            _rows.Clear();
            RebuildIndexes();
            MarkChanged();
        }

        public void ExportCsv(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            using (StreamWriter writer = new StreamWriter(path, false, Encoding.UTF8))
            {
                writer.WriteLine(string.Join(",", _fieldOrder.Select(field => EscapeCsv(field.Name))));
                foreach (Row row in _rows)
                {
                    string[] values = new string[NbFields];
                    for (int i = 0; i < NbFields; i++)
                        values[i] = EscapeCsv(FormatValue(row.Get(i), _fieldOrder[i].Type));
                    writer.WriteLine(string.Join(",", values));
                }
            }
        }

        public int ImportCsv(string path, bool clearExisting = false)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            int imported = 0;
            using (StreamReader reader = new StreamReader(path, Encoding.UTF8, true))
            {
                string headerLine = reader.ReadLine();
                if (headerLine == null)
                    return 0;

                string[] headers = ParseCsvLine(headerLine).ToArray();
                if (clearExisting)
                    ClearRows();

                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] values = ParseCsvLine(line).ToArray();
                    Dictionary<string, object> row = new Dictionary<string, object>(StringComparer.Ordinal);
                    for (int i = 0; i < headers.Length && i < values.Length; i++)
                    {
                        Field field = GetField(headers[i]);
                        row[field.Name] = ParseExternalValue(values[i], field.Type);
                    }

                    if (Insert(row))
                        imported++;
                }
            }

            return imported;
        }

        internal IReadOnlyList<Row> Execute(Condition condition)
        {
            if (condition == null || condition.IsAll)
                return _rows.ToArray();

            if (condition.TryCreateIndexedProbe(this, out string fieldName, out IReadOnlyList<object> values)
                && _indexes.TryGetValue(fieldName, out TableIndex index))
            {
                List<Row> indexedRows = new List<Row>();
                HashSet<Row> seen = new HashSet<Row>();
                foreach (object value in values)
                {
                    foreach (Row row in index.Find(value))
                        if (seen.Add(row))
                            indexedRows.Add(row);
                }
                return indexedRows.Where(condition.Matches).ToArray();
            }

            return _rows.Where(condition.Matches).ToArray();
        }

        internal Row ProjectRow(Row source, int[] selectedFields)
        {
            if (selectedFields == null || selectedFields.Length == 0)
                selectedFields = Enumerable.Range(0, NbFields).ToArray();

            object[] cells = new object[selectedFields.Length];
            Dictionary<string, int> lookup = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < selectedFields.Length; i++)
            {
                int sourceIndex = selectedFields[i];
                cells[i] = source.Get(sourceIndex);
                lookup[_fieldOrder[sourceIndex].Name] = i;
            }

            return new Row(cells, lookup);
        }

        internal int[] ResolveSelectedFields(string[] fieldNames)
        {
            if (fieldNames == null || fieldNames.Length == 0)
                return Enumerable.Range(0, NbFields).ToArray();

            int[] fields = new int[fieldNames.Length];
            for (int i = 0; i < fieldNames.Length; i++)
                fields[i] = GetFieldIndex(fieldNames[i]);
            return fields;
        }

        internal void SetCell(Row row, int fieldIndex, object value)
        {
            int rowIndex = _rows.IndexOf(row);
            if (rowIndex < 0)
            {
                row.SetDirect(fieldIndex, value);
                return;
            }

            SetCell(rowIndex, fieldIndex, value);
        }

        internal void SetCell(int rowIndex, int fieldIndex, object value)
        {
            Field field = _fieldOrder[fieldIndex];
            object normalized = NormalizeValue(field.Type, value, allowNull: true);
            Row row = _rows[rowIndex];
            object oldValue = row.Get(fieldIndex);

            if (ValuesEqual(oldValue, normalized))
                return;

            if (_indexes.TryGetValue(field.Name, out TableIndex index))
                index.EnsureCanAdd(normalized, row);

            row.SetDirect(fieldIndex, normalized);

            if (index != null)
            {
                index.Remove(oldValue, row);
                index.Add(normalized, row);
            }

            MarkChanged();
        }

        internal int UpdateWhere(Condition condition, IReadOnlyDictionary<string, object> assignments)
        {
            if (assignments == null || assignments.Count == 0)
                return 0;

            foreach (string fieldName in assignments.Keys)
                GetField(fieldName);

            List<Row> matches = Execute(condition ?? Condition.All).ToList();
            foreach (Row row in matches)
            {
                int rowIndex = _rows.IndexOf(row);
                if (rowIndex < 0)
                    continue;

                foreach (KeyValuePair<string, object> assignment in assignments)
                    SetCell(rowIndex, GetFieldIndex(assignment.Key), assignment.Value);
            }

            return matches.Count;
        }

        internal int DeleteWhere(Condition condition)
        {
            List<Row> matches = Execute(condition ?? Condition.All).ToList();
            if (matches.Count == 0)
                return 0;

            HashSet<Row> remove = new HashSet<Row>(matches);
            _rows.RemoveAll(remove.Contains);
            RebuildIndexes();
            MarkChanged();
            return matches.Count;
        }

        internal void AddLoadedField(string name, FastDBType type, object defaultValue)
        {
            Field field = new Field(name, type, defaultValue, _fieldOrder.Count);
            _fields.Add(name, field);
            _fieldOrder.Add(field);
        }

        internal void AddLoadedRow(object[] cells)
        {
            AddRow(new Row(this, cells), updateIndexes: false);
        }

        internal void AddLoadedIndex(string fieldName, bool unique)
        {
            Field field = GetField(fieldName);
            _indexes[fieldName] = new TableIndex(fieldName, field.FieldIndex, unique);
        }

        internal IEnumerable<TableIndexSnapshot> GetIndexSnapshots()
        {
            foreach (TableIndex index in _indexes.Values)
                yield return new TableIndexSnapshot(index.FieldName, index.Unique);
        }

        internal void FinishLoading(FastDatabase database)
        {
            Database = database;
            foreach (Row row in _rows)
                row.Table = this;
            RebuildIndexes();
        }

        internal void MarkChanged()
        {
            Database?.MarkChanged();
        }

        private void AddRow(Row row, bool updateIndexes)
        {
            if (updateIndexes)
            {
                foreach (TableIndex index in _indexes.Values)
                    index.EnsureCanAdd(row.Get(index.FieldIndex), row);
            }

            _rows.Add(row);

            if (updateIndexes)
            {
                foreach (TableIndex index in _indexes.Values)
                    index.Add(row.Get(index.FieldIndex), row);
            }
        }

        private void ReindexFields()
        {
            for (int i = 0; i < _fieldOrder.Count; i++)
                _fieldOrder[i].FieldIndex = i;
        }

        private static void ValidateName(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Name cannot be empty.", paramName);
        }

        internal static object NormalizeValue(FastDBType type, object value, bool allowNull)
        {
            if (value == null || value == DBNull.Value)
            {
                if (allowNull)
                    return null;
                throw new ArgumentNullException(nameof(value));
            }

            Type targetType = GetClrType(type);
            if (targetType.IsInstanceOfType(value))
                return value;

            try
            {
                switch (type)
                {
                    case FastDBType.String:
                        return Convert.ToString(value, CultureInfo.InvariantCulture);
                    case FastDBType.Integer:
                        return Convert.ToInt32(value, CultureInfo.InvariantCulture);
                    case FastDBType.UnsignedInteger:
                        return Convert.ToUInt32(value, CultureInfo.InvariantCulture);
                    case FastDBType.Float:
                        return Convert.ToSingle(value, CultureInfo.InvariantCulture);
                    case FastDBType.Double:
                        return Convert.ToDouble(value, CultureInfo.InvariantCulture);
                    case FastDBType.Bool:
                        return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
                    case FastDBType.Date:
                        return Convert.ToDateTime(value, CultureInfo.InvariantCulture).Date;
                    case FastDBType.DateTime:
                        return Convert.ToDateTime(value, CultureInfo.InvariantCulture);
                    case FastDBType.ByteArray:
                        if (value is string s)
                            return Convert.FromBase64String(s);
                        break;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidCastException("Value '" + value + "' cannot be converted to " + type + ".", ex);
            }

            throw new InvalidCastException("Value type " + value.GetType().Name + " is not compatible with " + type + ".");
        }

        internal static object GetDefaultValue(FastDBType type)
        {
            switch (type)
            {
                case FastDBType.String:
                    return string.Empty;
                case FastDBType.Integer:
                    return 0;
                case FastDBType.UnsignedInteger:
                    return 0u;
                case FastDBType.Float:
                    return 0f;
                case FastDBType.Double:
                    return 0d;
                case FastDBType.Bool:
                    return false;
                case FastDBType.Date:
                case FastDBType.DateTime:
                    return default(DateTime);
                case FastDBType.ByteArray:
                    return Array.Empty<byte>();
                default:
                    return null;
            }
        }

        internal static Type GetClrType(FastDBType type)
        {
            switch (type)
            {
                case FastDBType.String:
                    return typeof(string);
                case FastDBType.Integer:
                    return typeof(int);
                case FastDBType.UnsignedInteger:
                    return typeof(uint);
                case FastDBType.Float:
                    return typeof(float);
                case FastDBType.Double:
                    return typeof(double);
                case FastDBType.Bool:
                    return typeof(bool);
                case FastDBType.Date:
                case FastDBType.DateTime:
                    return typeof(DateTime);
                case FastDBType.ByteArray:
                    return typeof(byte[]);
                default:
                    return typeof(object);
            }
        }

        internal static bool ValuesEqual(object left, object right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (left == null || right == null)
                return false;
            if (left is byte[] lb && right is byte[] rb)
                return lb.SequenceEqual(rb);
            return left.Equals(right);
        }

        internal static object CopyValue(object value)
        {
            if (value is byte[] bytes)
                return bytes.ToArray();
            return value;
        }

        internal static string FormatValue(object value, FastDBType type)
        {
            if (value == null)
                return string.Empty;
            if (type == FastDBType.ByteArray)
                return Convert.ToBase64String((byte[])value);
            if (value is DateTime dateTime)
                return dateTime.ToString("O", CultureInfo.InvariantCulture);
            if (value is IFormattable formattable)
                return formattable.ToString(null, CultureInfo.InvariantCulture);
            return value.ToString();
        }

        internal static object ParseExternalValue(string value, FastDBType type)
        {
            if (string.IsNullOrEmpty(value))
                return null;
            return NormalizeValue(type, value, allowNull: true);
        }

        private static string EscapeCsv(string value)
        {
            value = value ?? string.Empty;
            if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0)
                return value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static IEnumerable<string> ParseCsvLine(string line)
        {
            List<string> values = new List<string>();
            StringBuilder current = new StringBuilder();
            bool quoted = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (quoted)
                {
                    if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else if (c == '"')
                    {
                        quoted = false;
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
                else if (c == ',')
                {
                    values.Add(current.ToString());
                    current.Length = 0;
                }
                else if (c == '"')
                {
                    quoted = true;
                }
                else
                {
                    current.Append(c);
                }
            }
            values.Add(current.ToString());
            return values;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(Name);
            foreach (Field field in _fieldOrder)
                sb.AppendLine(field.Name + " (" + field.Type + ") : " + (field.DefaultValue ?? "null"));
            return sb.ToString();
        }

        internal sealed class TableIndex
        {
            private static readonly object NullKey = new object();
            private readonly Dictionary<object, List<Row>> _map = new Dictionary<object, List<Row>>();

            public string FieldName { get; set; }
            public int FieldIndex { get; set; }
            public bool Unique { get; }

            public TableIndex(string fieldName, int fieldIndex, bool unique)
            {
                FieldName = fieldName;
                FieldIndex = fieldIndex;
                Unique = unique;
            }

            public void Rebuild(IEnumerable<Row> rows)
            {
                _map.Clear();
                foreach (Row row in rows)
                    Add(row.Get(FieldIndex), row);
            }

            public void EnsureCanAdd(object value, Row row)
            {
                if (!Unique)
                    return;

                object key = ToKey(value);
                if (_map.TryGetValue(key, out List<Row> rows) && rows.Any(existing => !ReferenceEquals(existing, row)))
                    throw new InvalidOperationException("Unique index violation on field '" + FieldName + "'.");
            }

            public void Add(object value, Row row)
            {
                EnsureCanAdd(value, row);
                object key = ToKey(value);
                if (!_map.TryGetValue(key, out List<Row> rows))
                {
                    rows = new List<Row>();
                    _map.Add(key, rows);
                }
                rows.Add(row);
            }

            public void Remove(Row row)
            {
                Remove(row.Get(FieldIndex), row);
            }

            public void Remove(object value, Row row)
            {
                object key = ToKey(value);
                if (!_map.TryGetValue(key, out List<Row> rows))
                    return;
                rows.Remove(row);
                if (rows.Count == 0)
                    _map.Remove(key);
            }

            public IReadOnlyList<Row> Find(object value)
            {
                object key = ToKey(value);
                if (_map.TryGetValue(key, out List<Row> rows))
                    return rows;
                return Array.Empty<Row>();
            }

            private static object ToKey(object value)
            {
                return value ?? NullKey;
            }
        }
    }

    internal readonly struct TableIndexSnapshot
    {
        public string FieldName { get; }
        public bool Unique { get; }

        public TableIndexSnapshot(string fieldName, bool unique)
        {
            FieldName = fieldName;
            Unique = unique;
        }
    }
}
