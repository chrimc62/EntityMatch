using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace Common
{
    /// <summary>
    /// This interface allows classes to serialize large data structures directly to/from the stream thus avoiding
    /// the memory and performance overheads of .NET serialization.
    /// If a class implements this, normal ISerializable (de)serialization will occur first, then the formatter or container class
    /// will call (De)Serialize().
    /// </summary>
    internal interface IRawSerializable : ISerializable
    {
        /// <summary>
        /// Set to true by the serializer/owner of this object if the (De)Serialize methods will be called after normal .NET (de)serialization has occurred.
        /// If set to false, the object implementing this interface should perform normal (possibly more expensive) (de)serialization in GetObjectData and the
        /// deserialization constructor.
        /// </summary>
        bool EnableRawSerialization { get; set; }

        /// <summary>
        /// Used to ensure that objects are deserialized in the correct order.  Objects implementing this interface must persist this ID with ISerialiable.
        /// </summary>
        int RawSerializationID { get; set; }

        /// <summary>
        /// Serialize data directly to the stream
        /// </summary>
        void Serialize(Stream stream);

        /// <summary>
        /// Deserialize data directly from the stream
        /// </summary>
        void Deserialize(Stream stream);
    }

    [Serializable]
    internal static class Utilities
    {
        public static int GetHashCode(char[] buffer, int start, int len) // PERF REVIEW : uint operations tend to be slower than int.  consider changing.
        {
            if (len <= 0 || buffer == null) return 0;

            int rem = len & 1;
            len >>= 1;

            int idx = start;
            uint hash = (uint)len;
            uint tmp;

            for (; len > 0; len--)  // PERF REVIEW : is this the fastest way to iterate over the chars?
            {
                hash += (uint)buffer[idx++];
                tmp = (((uint)buffer[idx++]) << 11) ^ hash;
                hash = (hash << 16) ^ tmp;
                hash += hash >> 11;
            }

            if (rem == 1)
            {
                hash += buffer[idx];
                hash ^= hash << 10;
                hash += hash >> 1;
            }

            // Force "avalanching" of final 127 bits
            hash ^= hash << 3;
            hash += hash >> 5;
            hash ^= hash << 4;
            hash += hash >> 17;
            hash ^= hash << 25;
            hash += hash >> 6;

            return (int)hash;
        }

        public static int GetHashCode(int i1, int i2)
        {
            int key = i1;

            key += ~(key << 15);
            key ^= (key >> 10);
            key += (key << 3);
            key ^= (key >> 6);
            key += ~(key << 11);
            key ^= (key >> 16);

            key += i2;

            key += ~(key << 15);
            key ^= (key >> 10);
            key += (key << 3);
            key ^= (key >> 6);
            key += ~(key << 11);
            key ^= (key >> 16);

            return key;
        }

        public static int GetHashCode(int key)
        {
            key += ~(key << 15);
            key ^= (key >> 10);
            key += (key << 3);
            key ^= (key >> 6);
            key += ~(key << 11);
            key ^= (key >> 16);

            return key;
        }

        public static string GenerateUniqueSuffix()
        {
            DateTime dt = DateTime.Now;
            Guid g = Guid.NewGuid();

            string guidString = g.ToString().Replace('-', '_');

            string s = string.Format("_{0:0000}{1:00}{2:00}_{3:00}{4:00}{5:00}_{6}", dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, guidString);
            return s;
        }

        public static string GenerateUniqueName(string prefix, int maxLength)
        {
            string suffix = Utilities.GenerateUniqueSuffix();
            int prefixLen = Math.Min(prefix.Length, maxLength - suffix.Length);
            return prefix.Substring(0, prefixLen) + suffix;
        }

        // Perform a deep copy
        public static DataTable CloneDataTable(DataTable table)
        {
            DataTable clone = (DataTable)table.Clone();

            using (IDataReader rdr = table.CreateDataReader())
            {
                while (rdr.Read())
                {
                    object[] values = new object[rdr.FieldCount];
                    rdr.GetValues(values);
                    clone.Rows.Add(values);
                }
            }

            return clone;
        }

        /// <summary>
        /// Prints out the column header and rows of the data table.
        /// </summary>
        /// <param name="dt">A data table.</param>
        public static void PrintDataTableToConsole(DataTable dt)
        {
            using (DataTableReader reader = new DataTableReader(dt))
            {
                PrintColumnNamesToConsole(reader.GetSchemaTable());
                PrintDataReaderToConsole(reader);
            }
        }

        /// <summary>
        /// Prints out the names of each schema column in the schema table.
        /// </summary>
        /// <param name="schemaTable">A schema table of the form returned by IDataReader.GetSchemaTable().</param>
        public static void PrintColumnNamesToConsole(DataTable schemaTable)
        {
            string s = string.Empty;

            int c = 1;

            foreach (DataRow r in schemaTable.Rows)
            {
                s += string.Format("<{0}> {1} ", c++, r[SchemaTableColumn.ColumnName].ToString());
            }

            Console.WriteLine(s);
        }

        /// <summary>
        /// Prints out the data values in each row of the data reader.
        /// </summary>
        /// <param name="rdr">A data reader.</param>
        public static void PrintDataReaderToConsole(IDataReader rdr)
        {
            string s = string.Empty;

            int i = 0;
            while (rdr.Read())
            {
                Console.Write(string.Format("{0}: ", i++));
                PrintDataRecordToConsole(rdr as IDataRecord);
            }
        }

        /// <summary>
        /// Prints out the data values of a record.
        /// </summary>
        /// <param name="record">A data record.</param>
        public static void PrintDataRecordToConsole(IDataRecord record)
        {
            string s = string.Empty;

            for (int c = 0; c < record.FieldCount; c++)
            {
                string s2 = "(null)";

                if (!record.IsDBNull(c))
                {
                    s2 = record.GetValue(c).ToString()!;
                }

                s += string.Format("<{0}> {1} ", c, s2);
            }

            Console.WriteLine(s);
        }

        // U is underlying value type
        public static bool EnumParse<T, U>(string enumString, out T enumValue)
        {
            Debug.Assert(typeof(U) == Enum.GetUnderlyingType(typeof(T)));

            enumValue = default(T)!;

            string[] names = Enum.GetNames(typeof(T));
            Array values = Enum.GetValues(typeof(T));

            int index = Array.FindIndex<string>(names, delegate(string s) { return s.CompareTo(enumString) == 0; });

            if (index < 0)
            {
                int i = 0;
                foreach (U? v in values)
                {
                    if (v!.ToString()!.CompareTo(enumString) == 0)
                    {
                        index = i;
                        break;
                    }
                    i++;
                }
            }

            if (index > 0)
            {
                enumValue = (T)values.GetValue(index)!;
                return true;
            }

            return false;
        }

        public static void WriteInt(byte[] b, int i)
        {
            b[0] = (byte)(i >> 24);
            b[1] = (byte)(i >> 16);
            b[2] = (byte)(i >> 8);
            b[3] = (byte)(i >> 0);
        }

        public static void WriteInt(byte[] b, ref int idx, int i)
        {
            b[idx++] = (byte)(i >> 24);
            b[idx++] = (byte)(i >> 16);
            b[idx++] = (byte)(i >> 8);
            b[idx++] = (byte)(i >> 0);
        }

        public static int ReadInt(byte[] b)
        {
            return (int)(b[0] << 24) | (int)(b[1] << 16) | (int)(b[2] << 8) | (int)b[3];
        }

        public static int ReadInt(byte[] b, ref int idx)
        {
            return (int)(b[idx++] << 24) | (int)(b[idx++] << 16) | (int)(b[idx++] << 8) | (int)b[idx++];
        }

        sealed class CharArrayEqualityComparer : IEqualityComparer<char[]>
        {
            public static readonly CharArrayEqualityComparer Instance = new CharArrayEqualityComparer();

            public int Compare(char[] x, char[] y)
            {
                int l = Math.Min(x.Length, y.Length);

                for (int i = 0; i < l; i++)
                {
                    if (x[i] != y[i])
                    {
                        return x[i] < y[i] ? -1 : 1;
                    }
                }

                return x.Length == y.Length ? 0 : x.Length < y.Length ? -1 : 1;
            }

            public bool Equals(char[]? x, char[]? y)
            {
                return 0 == Compare(x!, y!);
            }

            public int GetHashCode(char[] x)
            {
                return Utilities.GetHashCode(x, 0, x.Length);
            }
        }

    }
}
