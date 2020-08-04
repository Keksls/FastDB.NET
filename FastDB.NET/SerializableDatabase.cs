using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;

namespace FastDB.NET
{
    internal static class SerializableDatabase
    {
        internal unsafe static void Serialize(FastDatabase db)
        {
            // Getting full size
            int fullSize = 4;
            foreach (var table in db.Tables)
                fullSize += table.Value.GetSize();
            // prepare ptr buffer
            byte[] buffer = new byte[fullSize];
            System.Runtime.InteropServices.GCHandle rawDataHandle = System.Runtime.InteropServices.GCHandle.Alloc(buffer, System.Runtime.InteropServices.GCHandleType.Pinned);
            int* bufferPtr = (int*)rawDataHandle.AddrOfPinnedObject().ToPointer();
            // Serialize
            db.Serialize(bufferPtr);
            db.WriteInt(db.Tables.Count);
            foreach (var table in db.Tables)
                table.Value.Serialize(bufferPtr);
            // Write buffer
            File.WriteAllBytes(Path.Combine(db.FilePath, db.DatabaseName + ".FastDB"), buffer);
            // Free memory
            rawDataHandle.Free();
        }

        internal unsafe static void Deserialize(FastDatabase db)
        {
            // Read buffer
            byte[] buffer = File.ReadAllBytes(Path.Combine(db.FilePath, db.DatabaseName + ".FastDB"));
            // prepare ptr buffer
            System.Runtime.InteropServices.GCHandle rawDataHandle = System.Runtime.InteropServices.GCHandle.Alloc(buffer, System.Runtime.InteropServices.GCHandleType.Pinned);
            int* bufferPtr = (int*)rawDataHandle.AddrOfPinnedObject().ToPointer();
            // Deserialize
            db.Deserialize(bufferPtr);
            int nbTables = db.ReadInt();
            for (int i = 0; i < nbTables; i++)
            {
                Table table = new Table("");
                table.Deserialize(bufferPtr);
            }
            // Free memory
            rawDataHandle.Free();
        }
    }

    public unsafe abstract class SerializableBlock
    {
        private int* bufferPtr;

        public abstract int GetSize();

        internal virtual void Serialize(int* ptr)
        {
            bufferPtr = ptr;
        }

        internal virtual void Deserialize(int* ptr)
        {
            bufferPtr = ptr;
        }

        internal unsafe void WriteString(string Value)
        {
            string v = Value;
            fixed (char* p = v)
            {
                char* ptr = (char*)bufferPtr;
                for (int i = 0; i < Value.Length; i++)
                {
                    *(ptr) = *(p + i);
                    ptr++;
                }
                bufferPtr = (int*)ptr;
            }
        }

        internal unsafe void WriteInt(int value)
        {
            *(bufferPtr) = value;
            bufferPtr++;
        }

        internal unsafe void WriteFloat(float value)
        {
            (*(float*)(bufferPtr)) = value;
            bufferPtr++;
        }

        internal unsafe void WriteBool(bool value)
        {
            (*(bool*)(bufferPtr)) = value;
            bool* p = (bool*)bufferPtr;
            p++;
            bufferPtr = (int*)p;
        }

        internal unsafe int ReadInt()
        {
            bufferPtr++;
            return *(bufferPtr - 1);
        }

        internal unsafe bool ReadBool()
        {
            bool* p = (bool*)bufferPtr;
            p++;
            bufferPtr = (int*)p;
            return *(p - 1);
        }

        internal unsafe float Readfloat()
        {
            bufferPtr++;
            return (*(float*)(bufferPtr - 1));
        }

        internal unsafe string ReadString(int size)
        {
            char[] charArray = new char[size];
            char* p = (char*)bufferPtr;
            for(int i = 0; i < size; i++)
            {
                charArray[i] = *(p);
                p++;
            }
            bufferPtr = (int*)p;
            string retVal;
            fixed (char* charPointer = charArray)
            {
                retVal = new string(charPointer);
            }
            return retVal;
        }
    }
}