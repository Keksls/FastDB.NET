using System;

namespace FastDB.NET
{
    public struct Field
    {
        public string Name { get; set; }
        public FastDBType Type { get; set; }
        public object DefaultValue { get; set; }
        public int FieldIndex { get; set; }

        public Field(string name, FastDBType type, object defaultValue, int fieldIndex)
        {
            Name = name;
            Type = type;
            FieldIndex = fieldIndex;
            DefaultValue = defaultValue;
            if (DefaultValue == null)
                switch (type)
                {
                    case FastDBType.String:
                        DefaultValue = "";
                        break;
                    case FastDBType.Integer:
                        DefaultValue = 0;
                        break;
                    case FastDBType.Float:
                        DefaultValue = 0f;
                        break;
                    case FastDBType.Bool:
                        DefaultValue = false;
                        break;
                    case FastDBType.DateTime:
                        DefaultValue = DateTime.Now;
                        break;
                    default:
                        break;
                }
        }
    }
}