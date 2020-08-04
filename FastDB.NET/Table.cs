using System;
using System.Collections.Generic;

namespace FastDB.NET
{
    public unsafe class Table : SerializableBlock
    {
        public string Name { get; set; }
        public Dictionary<string, Field> Fields { get; set; }
        public List<Row> Rows { get; set; }
        public int NbFields { get { return Fields.Count; } }
        public int NbRows { get { return Rows.Count; } }

        public Table(string name)
        {
            Name = name;
            Fields = new Dictionary<string, Field>();
            Rows = new List<Row>();
        }

        /// <summary>
        /// Add a field to this table
        /// </summary>
        /// <param name="name">name of the field to add</param>
        /// <param name="type">type of the data it will contains</param>
        /// <param name="size">the size of this fiels (for string only)</param>
        /// <param name="defaultValue">the value to set by default</param>
        /// <returns>this self instance</returns>
        public Table AddField(string name, FastDBType type, IComparable defaultValue = default(IComparable))
        {
            // check if field already exist
            if (Fields.ContainsKey(name))
                throw new FieldAlreadyExistExceptions();
            // insert the field
            Fields.Add(name, new Field(name, type, defaultValue));
            // set default values for already existing rows
            foreach (Row row in Rows)
                row.Cells.Add(name, defaultValue);
            return this;
        }

        /// <summary>
        /// Insert some data to this table
        /// </summary>
        /// <param name="Values">data values to insert</param>
        /// <returns>true if success, false if table don't exist OR values mismatch the fields</returns>
        public bool Insert(params IComparable[] Values)
        {
            // check data length
            if (Values.Length != NbFields)
                return false;
            // prepare row
            Row row = new Row();
            int index = 0;
            foreach (var pair in Fields)
            {
                // Add to row
                row.Cells.Add(pair.Key, Values[index]);
                index++;
            }
            // insert row
            Rows.Add(row);
            return true;
        }

        /// <summary>
        /// Insert some data to this table
        /// </summary>
        /// <param name="Values">data values to insert</param>
        /// <returns>true if success, false if table don't exist OR values mismatch the fields</returns>
        public bool Insert(Dictionary<string, IComparable> Values)
        {
            // check data length
            if (Values.Count != NbFields)
                return false;
            // prepare row
            Row row = new Row();
            foreach (var pair in Fields)
                row.Cells.Add(pair.Key, Values[pair.Key]);
            // insert row
            Rows.Add(row);
            return true;
        }

        /// <summary>
        /// Select some rows b=that match to the condition
        /// </summary>
        /// <param name="FieldName">Field to check</param>
        /// <param name="Condition">Conditional operator</param>
        /// <param name="TargetValue">target value to match with</param>
        /// <returns>a list of all rows that match</returns>
        public List<Row> Select(string FieldName, DBCondition Condition, IComparable TargetValue)
        {
            if (!Fields.ContainsKey(FieldName))
                throw new FieldDontExistExceptions();
            List<Row> rows = new List<Row>();
            Func<IComparable, bool> condition = GetConditionFunction(Condition, TargetValue);
            foreach (var row in Rows)
                if (condition(row.Cells[FieldName]))
                    rows.Add(row);
            return rows;
        }


        /// <summary>
        /// Create the Condition Function
        /// </summary>
        private Func<IComparable, bool> GetConditionFunction(DBCondition condition, IComparable targetValue)
        {
            switch (condition)
            {
                default:
                case DBCondition.Equal:
                    return (Value) => { return Value.CompareTo(targetValue) == 0; };

                case DBCondition.GreaterThan:
                    return (Value) => { return Value.CompareTo(targetValue) > 0; };

                case DBCondition.LessThan:
                    return (Value) => { return Value.CompareTo(targetValue) < 0; };

                case DBCondition.GreaterOrEqual:
                    return (Value) => { return Value.CompareTo(targetValue) >= 0; };

                case DBCondition.LessOrEqual:
                    return (Value) => { return Value.CompareTo(targetValue) <= 0; };

                case DBCondition.NotEqual:
                    return (Value) => { return Value.CompareTo(targetValue) != 0; };
            }
        }

        public override int GetSize()
        {
            int size = 4 + Name.Length * 4; // tableName

            // Fields
            size += 4;
            foreach (var field in Fields)
            {
                size += 4 + field.Value.Name.Length * 4; // name
                size += 4; // type
                // default value
                switch (field.Value.Type)
                {
                    case FastDBType.String:
                        size += 4 + ((string)field.Value.DefaultValue).Length * 4;
                        break;
                    case FastDBType.Integer:
                    case FastDBType.Float:
                        size += 4;
                        break;
                    case FastDBType.Bool:
                        size += 1;
                        break;
                    case FastDBType.DateTime:
                        size += 4 + ((DateTime)field.Value.DefaultValue).ToString().Length * 4;
                        break;
                    default:
                        break;
                }
            }

            // Rows values
            foreach(var row in Rows)
            {
                foreach(var cell in row.Cells)
                {
                    switch (Fields[cell.Key].Type)
                    {
                        case FastDBType.String:
                            size += 4 + ((string)cell.Value).Length * 4;
                            break;
                        case FastDBType.Integer:
                        case FastDBType.Float:
                            size += 4;
                            break;
                        case FastDBType.Bool:
                            size += 1;
                            break;
                        case FastDBType.DateTime:
                            size += 4 + ((DateTime)cell.Value).ToString().Length * 4;
                            break;
                        default:
                            break;
                    }
                }
            }

            return size;
        }

        internal override void Serialize(int* ptr)
        {
            base.Serialize(ptr);
            // Header
            int size = GetSize();
            WriteInt(size);
            WriteInt(Name.Length);
            WriteString(Name);
            // Fields
            WriteInt(Fields.Count);
            foreach (var field in Fields)
            {
                WriteInt(field.Value.Name.Length);
                WriteString(field.Value.Name);
                WriteInt((int)field.Value.Type);
                switch (field.Value.Type)
                {
                    case FastDBType.String:
                        WriteInt(((string)field.Value.DefaultValue).Length);
                        WriteString((string)field.Value.DefaultValue);
                        break;
                    case FastDBType.Integer:
                        WriteInt((int)field.Value.DefaultValue);
                        break;
                    case FastDBType.Float:
                        WriteFloat((float)field.Value.DefaultValue);
                        break;
                    case FastDBType.Bool:
                        WriteBool((bool)field.Value.DefaultValue);
                        break;
                    case FastDBType.DateTime:
                        WriteInt((((DateTime)field.Value.DefaultValue).ToString()).Length);
                        WriteString(((DateTime)field.Value.DefaultValue).ToString());
                        break;
                    default:
                        break;
                }
            }
            // Rows
            foreach (var row in Rows)
                foreach (var cell in row.Cells)
                {
                    switch (Fields[cell.Key].Type)
                    {
                        case FastDBType.String:
                            WriteString((string)cell.Value);
                            break;
                        case FastDBType.Integer:
                            WriteInt((int)cell.Value);
                            break;
                        case FastDBType.Float:
                            WriteFloat((float)cell.Value);
                            break;
                        case FastDBType.Bool:
                            WriteBool((bool)cell.Value);
                            break;
                        case FastDBType.DateTime:
                            WriteString(((DateTime)cell.Value).ToString());
                            break;
                        default:
                            break;
                    }
                }
        }

        internal override unsafe void Deserialize(int* ptr)
        {
            base.Deserialize(ptr);

            // Header
            int size = ReadInt();
            Name = ReadString(ReadInt());
            // Fields
            int nbFields = ReadInt();
            for(int i = 0; i < nbFields; i++)
            {
                string fieldName = ReadString(ReadInt());
                FastDBType type = (FastDBType)ReadInt();
                IComparable defaultValue = default(IComparable);
                switch (type)
                {
                    case FastDBType.String:
                        defaultValue = ReadString(ReadInt());
                        break;
                    case FastDBType.Integer:
                        defaultValue = ReadInt();
                        break;
                    case FastDBType.Float:
                        defaultValue = Readfloat();
                        break;
                    case FastDBType.Bool:
                        defaultValue = ReadBool();
                        break;
                    case FastDBType.DateTime:
                        defaultValue = DateTime.Parse(ReadString(ReadInt()));
                        break;
                    default:
                        defaultValue = default(IComparable);
                        break;
                }
                Fields.Add(fieldName, new Field(fieldName, type, defaultValue));
            }

        }
    }
}