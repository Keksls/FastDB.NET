using System;
using System.Collections.Generic;
using System.Text;

namespace FastDB.NET
{
    public class Row
    {
        public Dictionary<string, IComparable> Cells;

        public Row()
        {
            Cells = new Dictionary<string, IComparable>();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var pair in Cells)
                sb.Append(pair.Value.ToString()).Append(" ");
            return sb.ToString();
        }
    }
}
