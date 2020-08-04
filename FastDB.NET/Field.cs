using System;

namespace FastDB.NET
{
    public class Field
    {
        public string Name { get; set; }
        public FastDBType Type { get; set; }
        public IComparable DefaultValue { get; set; }

        public Field(string name, FastDBType type, IComparable defaultValue)
        {
            Name = name;
            Type = type;
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