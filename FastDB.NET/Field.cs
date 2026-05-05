using System;

namespace FastDB.NET
{
    public sealed class Field
    {
        public string Name { get; internal set; }
        public FastDBType Type { get; }
        private object _defaultValue;
        public object DefaultValue
        {
            get { return _defaultValue; }
            set { _defaultValue = Table.NormalizeValue(Type, value, allowNull: true) ?? Table.GetDefaultValue(Type); }
        }
        public int FieldIndex { get; internal set; }

        public Field(string name, FastDBType type, object defaultValue, int fieldIndex)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type;
            FieldIndex = fieldIndex;
            DefaultValue = defaultValue;
        }

        public override string ToString()
        {
            return Name + " (" + Type + ") - [" + (DefaultValue ?? "null") + "]";
        }
    }
}
