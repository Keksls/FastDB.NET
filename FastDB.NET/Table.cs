using FastDB.NET.CRUD;
using System;
using System.Collections.Generic;
using System.Text;

namespace FastDB.NET
{
    public unsafe class Table : SerializableBlock
    {
        public string Name { get; set; }
        public Dictionary<string, Field> Fields { get; set; }
        public List<Row> Rows;
        public int NbFields { get { return Fields.Count; } }
        public int NbRows { get { return Rows.Count; } }

        public Table(string name)
        {
            Name = name;
            Fields = new Dictionary<string, Field>();
            Rows = new List<Row>();
        }

        private void AddRow(Row row)
        {
            Rows.Add(row);
            //Array.Resize<Row>(ref Rows, NbRows + 1);
            //Rows[NbRows - 1] = row;
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
            Fields.Add(name, new Field(name, type, defaultValue, NbFields));
            // set default values for already existing rows
            foreach (Row row in Rows)
                row.AddField(defaultValue);
            return this;
        }

        /// <summary>
        /// Insert some data to this table
        /// </summary>
        /// <param name="Values">data values to insert</param>
        /// <returns>true if success, false if table don't exist OR values mismatch the fields</returns>
        public bool Insert(params object[] Values)
        {
            // check data length
            if (Values.Length != NbFields)
                return false;
            // insert row
            AddRow(new Row().SetCells(Values)); ;
            return true;
        }

        /// <summary>
        /// Insert some data to this table
        /// </summary>
        /// <param name="Values">data values to insert</param>
        /// <returns>true if success, false if table don't exist OR values mismatch the fields</returns>
        public bool Insert(Dictionary<string, object> Values)
        {
            // check data length
            if (Values.Count != NbFields)
                return false;
            // prepare row
            Row row = new Row();
            foreach (var pair in Fields)
                row.Set(pair.Value.FieldIndex, Values[pair.Key]);
            // insert row
            AddRow(row);
            return true;
        }

        /// <summary>
        /// Create a Selector to select some values in this table
        /// </summary>
        /// <param name="FieldName">Fields to select</param>
        public Select Select(params string[] FieldName)
        {
            return new Select(this, FieldName);
        }

        #region Serialization
        internal override int GetSize()
        {
            int size = 4 + Name.Length * 2; // tableName

            // Fields
            size += 4;
            foreach (var field in Fields)
            {
                size += 4 + field.Value.Name.Length * 2; // name
                size += 4; // type
                size += 4; // index
                // default value
                switch (field.Value.Type)
                {
                    case FastDBType.String:
                        size += 4 + ((string)field.Value.DefaultValue).Length * 2;
                        break;
                    case FastDBType.Integer:
                    case FastDBType.Float:
                        size += 4;
                        break;
                    case FastDBType.Bool:
                        size += 1;
                        break;
                    case FastDBType.DateTime:
                        size += 4 + ((DateTime)field.Value.DefaultValue).ToString().Length * 2;
                        break;
                    default:
                        break;
                }
            }

            size += 4; // nbRows

            // Rows values
            foreach (var row in Rows)
            {
                foreach (var field in Fields)
                {
                    switch (field.Value.Type)
                    {
                        case FastDBType.String:
                            size += 4 + row.Get<string>(field.Value.FieldIndex).Length * 2;
                            break;
                        case FastDBType.Integer:
                        case FastDBType.Float:
                            size += 4;
                            break;
                        case FastDBType.Bool:
                            size += 1;
                            break;
                        case FastDBType.DateTime:
                            size += 4 + (row.Get<DateTime>(field.Value.FieldIndex)).ToString().Length * 2;
                            break;
                        default:
                            break;
                    }
                }
            }

            return size;
        }

        internal override void Serialize()
        {
            // Header
            WriteInt(Name.Length);
            WriteString(Name);
            // Fields
            WriteInt(Fields.Count);
            foreach (var field in Fields)
            {
                WriteInt(field.Value.Name.Length);
                WriteString(field.Value.Name);
                WriteInt((int)field.Value.Type);
                WriteInt(field.Value.FieldIndex);
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
            // NbRows
            WriteInt(NbRows);
            // Rows
            foreach (var row in Rows)
                foreach (var field in Fields)
                {
                    switch (field.Value.Type)
                    {
                        case FastDBType.String:
                            WriteInt(row.Get<string>(field.Value.FieldIndex).Length);
                            WriteString(row.Get<string>(field.Value.FieldIndex));
                            break;
                        case FastDBType.Integer:
                            WriteInt(row.Get<int>(field.Value.FieldIndex));
                            break;
                        case FastDBType.Float:
                            WriteFloat(row.Get<float>(field.Value.FieldIndex));
                            break;
                        case FastDBType.Bool:
                            WriteBool(row.Get<bool>(field.Value.FieldIndex));
                            break;
                        case FastDBType.DateTime:
                            WriteInt((row.Get<DateTime>(field.Value.FieldIndex)).ToString().Length);
                            WriteString((row.Get<DateTime>(field.Value.FieldIndex)).ToString());
                            break;
                        default:
                            break;
                    }
                }
        }

        internal override unsafe void Deserialize()
        {
            // Header
            Name = ReadString(ReadInt());

            // Fields
            int nbFields = ReadInt();
            int i = 0;
            for (i = 0; i < nbFields; i++)
            {
                string fieldName = ReadString(ReadInt());
                FastDBType type = (FastDBType)ReadInt();
                int index = ReadInt();
                object defaultValue = null;
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
                        defaultValue = null;
                        break;
                }
                Fields.Add(fieldName, new Field(fieldName, type, defaultValue, index));
            }

            // Rows
            int nbRows = ReadInt();
            Field[] fields = new Field[nbFields];
            i = 0;
            foreach (var field in Fields)
            {
                fields[i] = field.Value;
                i++;
            }

            int j = 0;
            for (i = 0; i < nbRows; i++)
            {
                Row row = new Row();
                row.InitializeCells(nbFields);
                for (j = 0; j < nbFields; j++)
                {
                    switch (fields[j].Type)
                    {
                        case FastDBType.String:
                            row.Set(j, ReadString(ReadInt()));
                            break;
                        case FastDBType.Integer:
                            row.Set(j, ReadInt());
                            break;
                        case FastDBType.Float:
                            row.Set(j, Readfloat());
                            break;
                        case FastDBType.Bool:
                            row.Set(j, ReadBool());
                            break;
                        case FastDBType.DateTime:
                            row.Set(j, DateTime.Parse(ReadString(ReadInt())));
                            break;
                        default:
                            break;
                    }
                }
                AddRow(row);
            }
        }
        #endregion

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(Name);
            foreach (var field in Fields)
                sb.AppendLine(field.Key + " (" + field.Value.Type.ToString() + ") : " + field.Value.DefaultValue.ToString());
            return sb.ToString();
        }
    }
}