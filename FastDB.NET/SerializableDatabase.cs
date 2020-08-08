using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime;
using System.Text;

namespace FastDB.NET
{
    internal unsafe static class SerializableDatabase
    {
        internal static int* bufferPtr;

        internal unsafe static void Serialize(FastDatabase db)
        {
            // Getting full size
            int fullSize = db.GetSize();
            foreach (var table in db.Tables)
                fullSize += table.Value.GetSize();
            // prepare ptr buffer
            byte[] buffer = new byte[fullSize];
            System.Runtime.InteropServices.GCHandle rawDataHandle = System.Runtime.InteropServices.GCHandle.Alloc(buffer, System.Runtime.InteropServices.GCHandleType.Pinned);
            bufferPtr = (int*)rawDataHandle.AddrOfPinnedObject().ToPointer();
            // Serialize
            db.Serialize();
            foreach (var table in db.Tables)
                table.Value.Serialize();
            // Write buffer
            File.WriteAllBytes(Path.Combine(db.FilePath, db.DatabaseName + ".FastDB"), buffer);
            // Free memory
            buffer = new byte[0];
            rawDataHandle.Free();
        }

        internal unsafe static void Deserialize(FastDatabase db)
        {
            // Read buffer
            byte[] buffer = File.ReadAllBytes(Path.Combine(db.FilePath, db.DatabaseName + ".FastDB"));
            // prepare ptr buffer
            System.Runtime.InteropServices.GCHandle rawDataHandle = System.Runtime.InteropServices.GCHandle.Alloc(buffer, System.Runtime.InteropServices.GCHandleType.Pinned);
            bufferPtr = (int*)rawDataHandle.AddrOfPinnedObject().ToPointer();
            // Deserialize
            db.Deserialize();
            int nbTables = db.ReadInt();
            for (int i = 0; i < nbTables; i++)
            {
                Table table = new Table("");
                table.Deserialize();
                db.Tables.Add(table.Name, table);
            }
            // Free memory
            rawDataHandle.Free();
            buffer = null;
        }
    }

    public unsafe abstract class SerializableBlock
    {
        internal abstract int GetSize();

        internal abstract void Serialize();

        internal abstract void Deserialize();

        internal unsafe void WriteString(string Value)
        {
            string v = Value;
            fixed (char* p = v)
            {
                char* ptr = (char*)SerializableDatabase.bufferPtr;
                for (int i = 0; i < Value.Length; i++)
                {
                    *(ptr) = *(p + i);
                    ptr++;
                }
                SerializableDatabase.bufferPtr = (int*)ptr;
            }
        }

        internal unsafe void WriteInt(int value)
        {
            *(SerializableDatabase.bufferPtr) = value;
            SerializableDatabase.bufferPtr++;
        }

        internal unsafe void WriteFloat(float value)
        {
            (*(float*)(SerializableDatabase.bufferPtr)) = value;
            SerializableDatabase.bufferPtr++;
        }

        internal unsafe void WriteBool(bool value)
        {
            (*(bool*)(SerializableDatabase.bufferPtr)) = value;
            bool* p = (bool*)SerializableDatabase.bufferPtr;
            p++;
            SerializableDatabase.bufferPtr = (int*)p;
        }

        internal unsafe int ReadInt()
        {
            SerializableDatabase.bufferPtr++;
            return *(SerializableDatabase.bufferPtr - 1);
        }

        internal unsafe bool ReadBool()
        {
            bool* p = (bool*)SerializableDatabase.bufferPtr;
            p++;
            SerializableDatabase.bufferPtr = (int*)p;
            return *(p - 1);
        }

        internal unsafe float Readfloat()
        {
            SerializableDatabase.bufferPtr++;
            return (*(float*)(SerializableDatabase.bufferPtr - 1));
        }

        internal unsafe string ReadString(int size)
        {
            char[] charArray = new char[size];
            char* p = (char*)SerializableDatabase.bufferPtr;
            for (int i = 0; i < size; i++)
            {
                charArray[i] = *(p);
                p++;
            }
            SerializableDatabase.bufferPtr = (int*)p;
            string retVal;
            fixed (char* charPointer = charArray)
            {
                retVal = new string(charPointer);
            }
            return retVal;
        }
    }
}