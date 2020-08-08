using System;

namespace FastDB.NET.CRUD
{
    public struct Condition
    {
        public int FieldIndex;
        public Func<IComparable, bool> Function;
    }
}