using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace FastDB.NET
{
    public sealed class FastDatabase
    {
        private string _password;
        private bool _batching;
        private bool _dirtyDuringBatch;

        public string DatabaseName { get; private set; }
        public string FilePath { get; private set; }
        public IReadOnlyDictionary<string, Table> Tables { get { return _tables; } }
        public bool AutoSave { get; set; }
        public bool IsDirty { get; private set; }
        public bool IsLocked { get { return _password != null; } }
        public string FullPath { get { return Path.Combine(FilePath, DatabaseName + ".FastDB"); } }

        private readonly Dictionary<string, Table> _tables;

        public FastDatabase(string databaseName, string filePath)
        {
            ValidateName(databaseName, nameof(databaseName));
            DatabaseName = databaseName;
            FilePath = string.IsNullOrWhiteSpace(filePath) ? Environment.CurrentDirectory : filePath;
            _tables = new Dictionary<string, Table>(StringComparer.Ordinal);
        }

        public FastDatabase Connect(string password = null)
        {
            _tables.Clear();
            SerializableDatabase.Deserialize(this, password);
            _password = password;
            IsDirty = false;
            return this;
        }

        public FastDatabase Open(string password = null)
        {
            return Connect(password);
        }

        public FastDatabase Save()
        {
            SerializableDatabase.Serialize(this, _password);
            IsDirty = false;
            _dirtyDuringBatch = false;
            return this;
        }

        public FastDatabase SaveAs(string databaseName, string filePath = null, string password = null)
        {
            ValidateName(databaseName, nameof(databaseName));
            string previousName = DatabaseName;
            string previousPath = FilePath;
            string previousPassword = _password;
            try
            {
                DatabaseName = databaseName;
                FilePath = filePath ?? FilePath;
                _password = password;
                Save();
                return this;
            }
            finally
            {
                DatabaseName = previousName;
                FilePath = previousPath;
                _password = previousPassword;
            }
        }

        public FastDatabase Lock(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be empty.", nameof(password));

            _password = password;
            MarkChanged();
            return this;
        }

        public FastDatabase UnLock(string password)
        {
            if (_password != null && _password != password)
                throw new UnauthorizedAccessException("Invalid database password.");

            _password = null;
            MarkChanged();
            return this;
        }

        public FastDatabase Unlock(string password)
        {
            return UnLock(password);
        }

        public bool TableExists(string name)
        {
            return _tables.ContainsKey(name);
        }

        public Table CreateTable(string name)
        {
            ValidateName(name, nameof(name));
            if (_tables.ContainsKey(name))
                throw new TableAlreadyExistExceptions(name);

            Table table = new Table(name) { Database = this };
            _tables.Add(name, table);
            MarkChanged();
            return table;
        }

        public FastDatabase RemoveTable(string name)
        {
            if (!_tables.Remove(name))
                throw new TableDontExistExceptions(name);

            MarkChanged();
            return this;
        }

        public Table GetTable(string name)
        {
            if (!_tables.TryGetValue(name, out Table table))
                throw new TableDontExistExceptions(name);
            return table;
        }

        public bool TryGetTable(string name, out Table table)
        {
            return _tables.TryGetValue(name, out table);
        }

        public FastDatabase RenameTable(string oldTableName, string newTableName)
        {
            ValidateName(newTableName, nameof(newTableName));
            if (!_tables.TryGetValue(oldTableName, out Table table))
                throw new TableDontExistExceptions(oldTableName);
            if (_tables.ContainsKey(newTableName))
                throw new TableAlreadyExistExceptions(newTableName);

            _tables.Remove(oldTableName);
            table.Name = newTableName;
            _tables.Add(newTableName, table);
            MarkChanged();
            return this;
        }

        public bool Insert(string tableName, params object[] values)
        {
            return GetTable(tableName).Insert(values);
        }

        public bool Insert(string tableName, Dictionary<string, object> values)
        {
            return GetTable(tableName).Insert(values);
        }

        public bool Insert(string tableName, IDictionary<string, object> values)
        {
            return GetTable(tableName).Insert(values);
        }

        public bool Exists()
        {
            return File.Exists(FullPath);
        }

        public void Batch(Action<FastDatabase> action, bool saveWhenDone = false)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            bool previousBatching = _batching;
            _batching = true;
            try
            {
                action(this);
            }
            finally
            {
                _batching = previousBatching;
            }

            if (!previousBatching && (saveWhenDone || (AutoSave && _dirtyDuringBatch)))
                Save();
        }

        public void ExportJson(string path, bool indented = true)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = indented };
            DatabaseDto dto = ToDto();
            File.WriteAllText(path, JsonSerializer.Serialize(dto, options));
        }

        public static FastDatabase ImportJson(string path, string databaseName, string filePath)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            DatabaseDto dto = JsonSerializer.Deserialize<DatabaseDto>(File.ReadAllText(path));
            if (dto == null)
                throw new InvalidDataException("Invalid FastDB JSON file.");

            FastDatabase database = new FastDatabase(databaseName, filePath);
            database.LoadFromDto(dto);
            database.IsDirty = true;
            return database;
        }

        public void ImportJsonInto(string path, bool clearExisting = true)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            DatabaseDto dto = JsonSerializer.Deserialize<DatabaseDto>(File.ReadAllText(path));
            if (dto == null)
                throw new InvalidDataException("Invalid FastDB JSON file.");

            if (clearExisting)
                _tables.Clear();
            LoadFromDto(dto);
            MarkChanged();
        }

        public void Close()
        {
            _tables.Clear();
            IsDirty = false;
        }

        internal IEnumerable<Table> GetTablesInOrder()
        {
            return _tables.Values;
        }

        internal void AddLoadedTable(Table table)
        {
            table.FinishLoading(this);
            _tables.Add(table.Name, table);
        }

        internal void MarkChanged()
        {
            IsDirty = true;
            if (_batching)
            {
                _dirtyDuringBatch = true;
                return;
            }

            if (AutoSave)
                Save();
        }

        private DatabaseDto ToDto()
        {
            return new DatabaseDto
            {
                Name = DatabaseName,
                Tables = _tables.Values.Select(table => new TableDto
                {
                    Name = table.Name,
                    Fields = table.FieldDefinitions.Select(field => new FieldDto
                    {
                        Name = field.Name,
                        Type = field.Type.ToString(),
                        DefaultValue = Table.FormatValue(field.DefaultValue, field.Type)
                    }).ToList(),
                    Indexes = table.GetIndexSnapshots().Select(index => new IndexDto
                    {
                        FieldName = index.FieldName,
                        Unique = index.Unique
                    }).ToList(),
                    Rows = table.Rows.Select(row =>
                    {
                        Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.Ordinal);
                        foreach (Field field in table.FieldDefinitions)
                            values[field.Name] = Table.FormatValue(row.Get(field.FieldIndex), field.Type);
                        return values;
                    }).ToList()
                }).ToList()
            };
        }

        private void LoadFromDto(DatabaseDto dto)
        {
            foreach (TableDto tableDto in dto.Tables ?? new List<TableDto>())
            {
                Table table = CreateTable(tableDto.Name);
                foreach (FieldDto fieldDto in tableDto.Fields ?? new List<FieldDto>())
                {
                    FastDBType type = ParseType(fieldDto.Type);
                    table.AddField(fieldDto.Name, type, Table.ParseExternalValue(fieldDto.DefaultValue, type));
                }

                foreach (Dictionary<string, string> rowDto in tableDto.Rows ?? new List<Dictionary<string, string>>())
                {
                    Dictionary<string, object> row = new Dictionary<string, object>(StringComparer.Ordinal);
                    foreach (Field field in table.FieldDefinitions)
                    {
                        if (rowDto.TryGetValue(field.Name, out string value))
                            row[field.Name] = Table.ParseExternalValue(value, field.Type);
                    }
                    table.Insert(row);
                }

                foreach (IndexDto indexDto in tableDto.Indexes ?? new List<IndexDto>())
                    table.CreateIndex(indexDto.FieldName, indexDto.Unique);
            }
        }

        private static FastDBType ParseType(string value)
        {
            if (Enum.TryParse(value, ignoreCase: true, out FastDBType type))
                return type;
            throw new InvalidDataException("Unknown FastDBType '" + value + "'.");
        }

        private static void ValidateName(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Name cannot be empty.", paramName);
        }

        private sealed class DatabaseDto
        {
            public string Name { get; set; }
            public List<TableDto> Tables { get; set; }
        }

        private sealed class TableDto
        {
            public string Name { get; set; }
            public List<FieldDto> Fields { get; set; }
            public List<IndexDto> Indexes { get; set; }
            public List<Dictionary<string, string>> Rows { get; set; }
        }

        private sealed class FieldDto
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public string DefaultValue { get; set; }
        }

        private sealed class IndexDto
        {
            public string FieldName { get; set; }
            public bool Unique { get; set; }
        }
    }
}

public enum FastDBType
{
    Null = 0,
    String = 1,
    Integer = 2,
    Float = 3,
    Bool = 4,
    Date = 5,
    DateTime = 6,
    UnsignedInteger = 7,
    Double = 8,
    ByteArray = 9
}

public enum DBCondition
{
    [Description(">")]
    GreaterThan = 0,
    [Description("=")]
    Equal = 1,
    [Description("<")]
    LessThan = 2,
    [Description(">=")]
    GreaterOrEqual = 5,
    [Description("<=")]
    LessOrEqual = 6,
    [Description("<>")]
    NotEqual = 7,
    [Description("IN")]
    IN = 8
}

public enum DBConditionLogical
{
    None = -1,
    AND = 0,
    OR = 1
}
