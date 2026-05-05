using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace FastDB.NET
{
    internal static class SerializableDatabase
    {
        private static readonly byte[] PlainMagic = Encoding.ASCII.GetBytes("FDB2");
        private static readonly byte[] EncryptedMagic = Encoding.ASCII.GetBytes("FDBE");
        private const int Version = 2;
        private const int MaxTables = 100000;
        private const int MaxFields = 10000;
        private const int MaxRows = 100000000;
        private const int MaxStringBytes = 64 * 1024 * 1024;
        private const int MaxByteArrayBytes = 512 * 1024 * 1024;
        private const int EncryptionIterations = 120000;
        private const int SaltSize = 16;
        private const int NonceSize = 12;
        private const int TagSize = 16;

        internal static void Serialize(FastDatabase db, string password)
        {
            Directory.CreateDirectory(db.FilePath);
            string targetPath = db.FullPath;
            string tempPath = targetPath + ".tmp";

            if (File.Exists(tempPath))
                File.Delete(tempPath);

            if (password == null)
                WritePlainFile(tempPath, db);
            else
                WriteEncryptedFile(tempPath, db, password);

            ReplaceAtomically(tempPath, targetPath);
        }

        internal static void Deserialize(FastDatabase db, string password)
        {
            using (FileStream stream = new FileStream(db.FullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024))
            {
                byte[] magic = ReadBytesExact(stream, 4);
                if (magic.SequenceEqual(PlainMagic))
                {
                    using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true))
                        ReadPayload(db, reader);
                    return;
                }

                if (magic.SequenceEqual(EncryptedMagic))
                {
                    if (password == null)
                        throw new UnauthorizedAccessException("This database is encrypted. Provide a password to Connect/Open.");

                    byte[] payload = ReadEncryptedPayload(stream, password);
                    using (MemoryStream payloadStream = new MemoryStream(payload, writable: false))
                    using (BinaryReader reader = new BinaryReader(payloadStream, Encoding.UTF8))
                        ReadPayload(db, reader);
                    return;
                }
            }

            throw new InvalidDataException("Unsupported FastDB file format. This file is probably a legacy v1 file.");
        }

        private static void WritePlainFile(string tempPath, FastDatabase db)
        {
            using (FileStream stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1024 * 1024, FileOptions.SequentialScan))
            {
                stream.Write(PlainMagic, 0, PlainMagic.Length);
                using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
                    WritePayload(db, writer);
                stream.Flush(flushToDisk: true);
            }
        }

        private static void WriteEncryptedFile(string tempPath, FastDatabase db, string password)
        {
            byte[] payload;
            using (MemoryStream payloadStream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(payloadStream, Encoding.UTF8, leaveOpen: true))
                    WritePayload(db, writer);
                payload = payloadStream.ToArray();
            }

            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
            byte[] ciphertext = new byte[payload.Length];
            byte[] tag = new byte[TagSize];
            byte[] key = DeriveKey(password, salt);

            using (AesGcm aes = new AesGcm(key, TagSize))
                aes.Encrypt(nonce, payload, ciphertext, tag);

            using (FileStream stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1024 * 1024))
            using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            {
                stream.Write(EncryptedMagic, 0, EncryptedMagic.Length);
                writer.Write(Version);
                WriteByteArray(writer, salt);
                WriteByteArray(writer, nonce);
                WriteByteArray(writer, tag);
                WriteByteArray(writer, ciphertext);
                stream.Flush(flushToDisk: true);
            }
        }

        private static byte[] ReadEncryptedPayload(Stream stream, string password)
        {
            using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true))
            {
                int version = reader.ReadInt32();
                if (version != Version)
                    throw new InvalidDataException("Unsupported encrypted FastDB version " + version + ".");

                byte[] salt = ReadByteArray(reader, SaltSize, SaltSize);
                byte[] nonce = ReadByteArray(reader, NonceSize, NonceSize);
                byte[] tag = ReadByteArray(reader, TagSize, TagSize);
                byte[] ciphertext = ReadByteArray(reader, 0, int.MaxValue);
                byte[] key = DeriveKey(password, salt);
                byte[] payload = new byte[ciphertext.Length];

                using (AesGcm aes = new AesGcm(key, TagSize))
                    aes.Decrypt(nonce, ciphertext, tag, payload);

                return payload;
            }
        }

        private static void WritePayload(FastDatabase db, BinaryWriter writer)
        {
            writer.Write(Version);
            IReadOnlyList<Table> tables = db.GetTablesInOrder().ToArray();
            writer.Write(tables.Count);

            foreach (Table table in tables)
            {
                WriteString(writer, table.Name);

                writer.Write(table.NbFields);
                foreach (Field field in table.FieldDefinitions)
                {
                    WriteString(writer, field.Name);
                    writer.Write((int)field.Type);
                    WriteTypedValue(writer, field.Type, field.DefaultValue);
                }

                TableIndexSnapshot[] indexes = table.GetIndexSnapshots().ToArray();
                writer.Write(indexes.Length);
                foreach (TableIndexSnapshot index in indexes)
                {
                    WriteString(writer, index.FieldName);
                    writer.Write(index.Unique);
                }

                writer.Write(table.NbRows);
                foreach (Row row in table.Rows)
                {
                    byte[] nulls = new byte[table.NbFields];
                    for (int i = 0; i < table.NbFields; i++)
                        nulls[i] = row.IsNull(i) ? (byte)1 : (byte)0;
                    writer.Write(nulls);

                    foreach (Field field in table.FieldDefinitions)
                    {
                        object value = row.Get(field.FieldIndex);
                        if (value != null)
                            WriteNonNullTypedValue(writer, field.Type, value);
                    }
                }
            }
        }

        private static void ReadPayload(FastDatabase db, BinaryReader reader)
        {
            int version = reader.ReadInt32();
            if (version != Version)
                throw new InvalidDataException("Unsupported FastDB version " + version + ".");

            int tableCount = ReadCount(reader, "table", MaxTables);
            for (int t = 0; t < tableCount; t++)
            {
                string tableName = ReadString(reader);
                Table table = new Table(tableName);

                int fieldCount = ReadCount(reader, "field", MaxFields);
                for (int f = 0; f < fieldCount; f++)
                {
                    string fieldName = ReadString(reader);
                    FastDBType type = (FastDBType)reader.ReadInt32();
                    object defaultValue = ReadTypedValue(reader, type);
                    table.AddLoadedField(fieldName, type, defaultValue);
                }

                int indexCount = ReadCount(reader, "index", MaxFields);
                for (int i = 0; i < indexCount; i++)
                {
                    string fieldName = ReadString(reader);
                    bool unique = reader.ReadBoolean();
                    table.AddLoadedIndex(fieldName, unique);
                }

                int rowCount = ReadCount(reader, "row", MaxRows);
                for (int r = 0; r < rowCount; r++)
                {
                    byte[] nulls = ReadBytesExact(reader.BaseStream, fieldCount);
                    object[] cells = new object[fieldCount];
                    for (int f = 0; f < fieldCount; f++)
                    {
                        if (nulls[f] == 0)
                            cells[f] = ReadNonNullTypedValue(reader, table.FieldDefinitions[f].Type);
                    }
                    table.AddLoadedRow(cells);
                }

                db.AddLoadedTable(table);
            }
        }

        private static void WriteTypedValue(BinaryWriter writer, FastDBType type, object value)
        {
            writer.Write(value == null);
            if (value != null)
                WriteNonNullTypedValue(writer, type, value);
        }

        private static object ReadTypedValue(BinaryReader reader, FastDBType type)
        {
            bool isNull = reader.ReadBoolean();
            return isNull ? null : ReadNonNullTypedValue(reader, type);
        }

        private static void WriteNonNullTypedValue(BinaryWriter writer, FastDBType type, object value)
        {
            switch (type)
            {
                case FastDBType.String:
                    WriteString(writer, (string)value);
                    break;
                case FastDBType.Integer:
                    writer.Write((int)value);
                    break;
                case FastDBType.UnsignedInteger:
                    writer.Write((uint)value);
                    break;
                case FastDBType.Float:
                    writer.Write((float)value);
                    break;
                case FastDBType.Double:
                    writer.Write((double)value);
                    break;
                case FastDBType.Bool:
                    writer.Write((bool)value);
                    break;
                case FastDBType.Date:
                case FastDBType.DateTime:
                    writer.Write(((DateTime)value).Ticks);
                    break;
                case FastDBType.ByteArray:
                    WriteByteArray(writer, (byte[])value);
                    break;
                default:
                    throw new InvalidDataException("Unsupported field type " + type + ".");
            }
        }

        private static object ReadNonNullTypedValue(BinaryReader reader, FastDBType type)
        {
            switch (type)
            {
                case FastDBType.String:
                    return ReadString(reader);
                case FastDBType.Integer:
                    return reader.ReadInt32();
                case FastDBType.UnsignedInteger:
                    return reader.ReadUInt32();
                case FastDBType.Float:
                    return reader.ReadSingle();
                case FastDBType.Double:
                    return reader.ReadDouble();
                case FastDBType.Bool:
                    return reader.ReadBoolean();
                case FastDBType.Date:
                case FastDBType.DateTime:
                    return new DateTime(reader.ReadInt64());
                case FastDBType.ByteArray:
                    return ReadByteArray(reader, 0, MaxByteArrayBytes);
                default:
                    throw new InvalidDataException("Unsupported field type " + type + ".");
            }
        }

        private static void WriteString(BinaryWriter writer, string value)
        {
            value = value ?? string.Empty;
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }

        private static string ReadString(BinaryReader reader)
        {
            int length = ReadLength(reader, MaxStringBytes);
            byte[] bytes = ReadBytesExact(reader.BaseStream, length);
            return Encoding.UTF8.GetString(bytes);
        }

        private static void WriteByteArray(BinaryWriter writer, byte[] value)
        {
            value = value ?? Array.Empty<byte>();
            writer.Write(value.Length);
            writer.Write(value);
        }

        private static byte[] ReadByteArray(BinaryReader reader, int expectedLength, int maxLength)
        {
            int length = ReadLength(reader, maxLength);
            if (expectedLength > 0 && length != expectedLength)
                throw new InvalidDataException("Unexpected byte array length.");
            return ReadBytesExact(reader.BaseStream, length);
        }

        private static int ReadCount(BinaryReader reader, string name, int max)
        {
            int value = reader.ReadInt32();
            if (value < 0 || value > max)
                throw new InvalidDataException("Invalid " + name + " count " + value + ".");
            return value;
        }

        private static int ReadLength(BinaryReader reader, int maxLength)
        {
            int length = reader.ReadInt32();
            if (length < 0 || length > maxLength)
                throw new InvalidDataException("Invalid payload length " + length + ".");
            return length;
        }

        private static byte[] ReadBytesExact(Stream stream, int count)
        {
            byte[] buffer = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                int read = stream.Read(buffer, offset, count - offset);
                if (read == 0)
                    throw new EndOfStreamException();
                offset += read;
            }
            return buffer;
        }

        private static byte[] DeriveKey(string password, byte[] salt)
        {
            using (Rfc2898DeriveBytes deriveBytes = new Rfc2898DeriveBytes(password, salt, EncryptionIterations, HashAlgorithmName.SHA256))
                return deriveBytes.GetBytes(32);
        }

        private static void ReplaceAtomically(string tempPath, string targetPath)
        {
            if (File.Exists(targetPath))
                File.Replace(tempPath, targetPath, null);
            else
                File.Move(tempPath, targetPath);
        }
    }
}
