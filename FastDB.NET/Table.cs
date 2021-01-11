using FastDB.NET.CRUD;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FastDB.NET
{
    public unsafe class Table : SerializableBlock
    {
        public string Name { get; set; }
        public Dictionary<string, Field> Fields { get; set; }
        public List<Row> Rows;
        private byte[] nulls;
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
        }

        /// <summary>
        /// Add a field to this table
        /// </summary>
        /// <param name="name">name of the field to add</param>
        /// <param name="type">type of the data it will contains</param>
        /// <param name="defaultValue">the value to set by default</param>
        /// <returns>this self instance</returns>
        public Table AddField(string name, FastDBType type, IComparable defaultValue = default(IComparable))
        {
            // check if field already exist
            if (Fields.ContainsKey(name))
                throw new FieldAlreadyExistExceptions();

            // Determinate strongly typed default value
            if (defaultValue == default(IComparable))
            {
                switch (type)
                {
                    default:
                    case FastDBType.String:
                        defaultValue = "";
                        break;
                    case FastDBType.Integer:
                        defaultValue = default(int);
                        break;
                    case FastDBType.UnsignedInteger:
                        defaultValue = default(uint);
                        break;
                    case FastDBType.Float:
                        defaultValue = default(float);
                        break;
                    case FastDBType.Double:
                        defaultValue = default(double);
                        break;
                    case FastDBType.ByteArray:
                        defaultValue = null;
                        break;
                    case FastDBType.Bool:
                        defaultValue = default(bool);
                        break;
                    case FastDBType.Date:
                    case FastDBType.DateTime:
                        defaultValue = default(DateTime);
                        break;
                }
            }

            // insert the field
            Fields.Add(name, new Field(name, type, defaultValue, NbFields));
            // set default values for already existing rows
            for (int i = 0; i < NbRows; i++)
                Rows[i] = new Row(this).SetCells(Rows[i].AddField(defaultValue).GetCells());
            return this;
        }

        /// <summary>
        /// Remove a field to this table
        /// </summary>
        /// <param name="name">name of the field to remove</param>
        /// <returns>this self instance</returns>
        public Table RemoveField(string name)
        {
            // check if field exist
            if (!Fields.ContainsKey(name))
                throw new FieldDontExistExceptions();
            // Remove from fields list
            int index = Fields[name].FieldIndex;
            Fields.Remove(name);
            // remove from rows
            for (int i = 0; i < NbRows; i++)
                Rows[i] = new Row(this).SetCells(Rows[i].RemoveField(index).GetCells());
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
            // prepare row
            Row row = new Row(this);
            row.InitializeCells(NbFields);
            foreach (var pair in Fields)
                row.Set(pair.Value.FieldIndex, Values[pair.Value.FieldIndex] == null ? pair.Value.DefaultValue : Values[pair.Value.FieldIndex]);
            // insert row
            AddRow(row);
            return true;
        }

        /// <summary>
        /// Insert some data to this table. don't check for null or default values. use it if you are sure of your datas. faster but unsafe
        /// </summary>
        /// <param name="Values">data values to insert</param>
        /// <returns>true if success, false if table don't exist OR values mismatch the fields</returns>
        public bool UnsafeInsert(params object[] Values)
        {
            AddRow(new Row(this).SetCells(Values));
            return true;
        }

        /// <summary>
        /// Insert some data into this table
        /// </summary>
        /// <param name="Values">data values to insert</param>
        /// <returns>true if success, false if table don't exist OR values mismatch the fields</returns>
        public bool Insert(Dictionary<string, object> Values)
        {
            // check data length
            if (Values.Count != NbFields)
                return false;
            // prepare row
            Row row = new Row(this);
            row.InitializeCells(NbFields);
            foreach (var pair in Fields)
                row.Set(pair.Value.FieldIndex, Values[pair.Key] == null ? pair.Value.DefaultValue : Values[pair.Key]);
            // insert row
            AddRow(row);
            return true;
        }

        /// <summary>
        /// Create a Selector to select some values in this table
        /// </summary>
        /// <param name="FieldName">Fields to select</param>
        public Select GetSelector(params string[] FieldName)
        {
            return new Select(this, FieldName);
        }

        /// <summary>
        /// Create a Selector to select some values in this table
        /// </summary>
        /// <param name="FieldName">Fields to select</param>
        public Select GetSelectorAllFields()
        {
            return new Select(this, Fields.Keys.ToArray());
        }

        public bool isNull(int RowIndex, int fieldIndex)
        {
            return nulls[RowIndex * NbFields + fieldIndex] == 1;
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
                    case FastDBType.Double:
                        size += 8;
                        break;
                    case FastDBType.Bool:
                        size += 4;
                        break;
                    case FastDBType.Date:
                    case FastDBType.DateTime:
                        size += 8;
                        break;
                    default:
                        break;
                }
            }

            size += 4; // nbRows
            // nulls
            size += Rows.Count * NbFields;

            // Rows values
            int i = 0;
            nulls = new byte[Rows.Count * NbFields];
            foreach (var row in Rows)
            {
                foreach (var field in Fields.Values)
                {
                    if (row.isNull(field.FieldIndex))
                    {
                        nulls[i] = 1;
                    }
                    else
                    {
                        nulls[i] = 0;
                        switch (field.Type)
                        {
                            case FastDBType.String:
                                size += 4 + row.Get<string>(field.FieldIndex).Length * 2;
                                break;
                            case FastDBType.UnsignedInteger:
                            case FastDBType.Integer:
                            case FastDBType.Float:
                                size += 4;
                                break;
                            case FastDBType.Double:
                                size += 8;
                                break;
                            case FastDBType.Bool:
                                size += 4;
                                break;
                            case FastDBType.Date:
                            case FastDBType.DateTime:
                                size += 8;
                                break;
                            case FastDBType.ByteArray:
                                size += 4; // Size
                                size += row.GetByteArray(field.FieldIndex).Length;
                                break;
                            default:
                                break;
                        }
                    }
                    i++;
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
                    case FastDBType.UnsignedInteger:
                        WriteUInt((uint)field.Value.DefaultValue);
                        break;
                    case FastDBType.Float:
                        WriteFloat((float)field.Value.DefaultValue);
                        break;
                    case FastDBType.Double:
                        WriteDouble((double)field.Value.DefaultValue);
                        break;
                    case FastDBType.ByteArray:
                        break;
                    case FastDBType.Bool:
                        WriteBool((bool)field.Value.DefaultValue);
                        break;
                    case FastDBType.Date:
                    case FastDBType.DateTime:
                        WriteDateTime((DateTime)field.Value.DefaultValue);
                        break;
                    default:
                        break;
                }
            }
            // NbRows
            WriteInt(NbRows);
            // Write null cells
            WriteByteArray(nulls);
            // Rows
            int i = 0;
            foreach (var row in Rows)
                foreach (var field in Fields.Values)
                {
                    if (!row.isNull(field.FieldIndex))
                        switch (field.Type)
                        {
                            case FastDBType.String:
                                WriteInt(row.Get<string>(field.FieldIndex).Length);
                                WriteString(row.Get<string>(field.FieldIndex));
                                break;
                            case FastDBType.Integer:
                                WriteInt(row.Get<int>(field.FieldIndex));
                                break;
                            case FastDBType.UnsignedInteger:
                                WriteUInt(row.Get<uint>(field.FieldIndex));
                                break;
                            case FastDBType.Float:
                                WriteFloat(row.Get<float>(field.FieldIndex));
                                break;
                            case FastDBType.Double:
                                WriteDouble(row.Get<double>(field.FieldIndex));
                                break;
                            case FastDBType.Bool:
                                WriteBool(row.Get<bool>(field.FieldIndex));
                                break;
                            case FastDBType.ByteArray:
                                WriteInt(row.GetByteArray(field.FieldIndex).Length);
                                WriteByteArray(row.GetByteArray(field.FieldIndex));
                                break;
                            case FastDBType.Date:
                            case FastDBType.DateTime:
                                WriteDateTime(row.Get<DateTime>(field.FieldIndex));
                                break;
                            default:
                                break;
                        }
                    i++;
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
                    case FastDBType.UnsignedInteger:
                        defaultValue = ReadUInt();
                        break;
                    case FastDBType.Float:
                        defaultValue = Readfloat();
                        break;
                    case FastDBType.Double:
                        defaultValue = ReadDouble();
                        break;
                    case FastDBType.Bool:
                        defaultValue = ReadBool();
                        break;
                    case FastDBType.Date:
                    case FastDBType.DateTime:
                        defaultValue = ReadDateTime();
                        break;
                    default:
                        defaultValue = null;
                        break;
                }
                Fields.Add(fieldName, new Field(fieldName, type, defaultValue, index));
            }

            // Rows
            int nbRows = ReadInt();

            // read nulls
            nulls = ReadByteArray(nbRows * nbFields);

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
                Row row = new Row(this);
                row.InitializeCells(nbFields);
                for (j = 0; j < nbFields; j++)
                {
                    if (!isNull(i, j))
                        switch (fields[j].Type)
                        {
                            case FastDBType.String:
                                row.Set(j, ReadString(ReadInt()));
                                break;
                            case FastDBType.Integer:
                                row.Set(j, ReadInt());
                                break;
                            case FastDBType.UnsignedInteger:
                                row.Set(j, ReadUInt());
                                break;
                            case FastDBType.Float:
                                row.Set(j, Readfloat());
                                break;
                            case FastDBType.Double:
                                row.Set(j, ReadDouble());
                                break;
                            case FastDBType.Bool:
                                row.Set(j, ReadBool());
                                break;
                            case FastDBType.ByteArray:
                                row.Set(j, ReadByteArray(ReadInt()));
                                break;
                            case FastDBType.Date:
                            case FastDBType.DateTime:
                                row.Set(j, ReadDateTime());
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