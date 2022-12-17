using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

using LengthType = System.Int32;  // Note: might as well make 32 bits over 16 bits; keeps StringExtent aligned on 64-bit boundary and prevents overflow issues and cast cost.

namespace Common
{
    [Serializable]
    internal static class StringExtentStatic
    {
        public static readonly StringExtent Empty = new StringExtent(new char[0], 0, 0);

        public static readonly StringExtentHeapAllocator HeapAllocator = new StringExtentHeapAllocator();
    }

    /// <summary>
    /// Interface representing basic operations on a string.  
    /// Structs implementing this interface can be efficiently inlined by string functions with the constaint where T : IString
    /// </summary>
    public interface IString
    {
        /// <summary>
        /// The length of the string
        /// </summary>
        int Length { get; }

        /// <summary>
        /// The character at the zero-based position.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        char this[int i] { get; }
    }

    [Serializable]
    internal struct StringWrapper : IString
    {
        private readonly string m_str;

        public StringWrapper(string s)
        {
            m_str = s;
        }

        public int Length { get { return m_str.Length; } }

        public char this[int i] { get { return m_str[i]; } }

        public static implicit operator string(StringWrapper s) { return s.m_str; }
        public static implicit operator StringWrapper(string s) { return new StringWrapper(s); }

        public static int Compare<S, T>(S s, int s_start, T t, int t_start, int len)
            where S : IString
            where T : IString
        {
            int si = s_start;
            int ti = t_start;

            for (int k = 0; k < len; k++, si++, ti++)
            {
                if (si < s.Length && ti < t.Length)
                {
                    if (s[si] == t[ti])
                    {
                        continue;
                    }

                    return s[si] < t[ti] ? -1 : 1;
                }

                if (si > s.Length)
                    return -1;

                if (ti > t.Length)
                    return 1;
            }

            return 0;
        }
    }

    // Compare to ArraySegment<char> (ArraySegments are readonly)
    /// <summary>
    /// Represents a span of a character array
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    [Serializable]
    public struct StringExtent : IEquatable<StringExtent>, IString
    {
        public static readonly StringExtentEqualityComparer EqualityComparer = StringExtentEqualityComparer.Instance;

        public const int SizeOf = 8 + 2 * sizeof(LengthType);
        public const int MaxLength = LengthType.MaxValue;

        private char[] m_str;
        private LengthType m_Start;
        private LengthType m_Length;

        public StringExtent(string s)
            : this(s.ToCharArray(), 0, s.Length)
        {
        }

        public StringExtent(char[] str)
            : this(str, 0, str.Length)
        {
        }

        public StringExtent(char[] str, int start, int length)
        {
            if (start > MaxLength || length > MaxLength)
            {
                throw new ArgumentOutOfRangeException("The start or length specified to StringExtent goes beyond the maximum value of " + MaxLength + "."); //@err
            }

            m_str = str;
            m_Start = (LengthType)start;
            m_Length = (LengthType)length;
        }

        public Int64 MemoryUsage
        {
            get { return Length * sizeof(char) + 2 * sizeof(LengthType) + sizeof(int); }
        }

        public char[] Str
        {
            [DebuggerStepThroughAttribute]
            get { return m_str; }
            set { m_str = value; }
        }

        public int Start
        {
            [DebuggerStepThroughAttribute]
            get { return m_Start; }
            set
            {
#if DEBUG
                if (value > MaxLength)
                {
                    throw new ArgumentOutOfRangeException("The start value specified to StringExtent goes beyond the maximum value of " + MaxLength + "."); //@err
                }
#endif
                m_Start = (LengthType)value;
            }
        }

        public int Length
        {
            [DebuggerStepThroughAttribute]
            get { return m_Length; }
            set
            {
#if DEBUG
                if (value > MaxLength)
                {
                    throw new ArgumentOutOfRangeException("The length specified to StringExtent goes beyond the maximum value of " + MaxLength + "."); //@err
                }
#endif
                m_Length = (LengthType)value;
            }
        }

        public char this[int index]
        {
            [DebuggerStepThroughAttribute]
            get { return Str[Start + index]; }
        }

        public override string ToString()
        {
            return (Str != null) ? new string(m_str, Start, Length) : string.Empty;
        }

        public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            if (!IsEmpty)
            {
                Array.ConstrainedCopy(m_str, Start, destination, destinationIndex, count);
            }
        }

        public StringExtent AllocClone(IStringExtentAllocator extentAllocator)
        {
            StringExtent clone = extentAllocator.New(Length);

            if (!IsEmpty)
            {
                Array.ConstrainedCopy(m_str, Start, clone.m_str, clone.Start, Length);
            }

            return clone;
        }

        public StringExtent AllocClone()
        {
            StringExtent clone = new StringExtent(new char[Length], 0, Length);

            if (!IsEmpty)
            {
                Array.ConstrainedCopy(m_str, Start, clone.m_str, clone.Start, Length);
            }

            return clone;
        }

        public StringExtent SubExtent(int startIndex, int length)
        {
            if (Start + startIndex + length > m_str.Length)
            {
                throw new ArgumentOutOfRangeException("The length specified to SubExtent goes beyond the length of the underlying string."); //@err
            }

            return new StringExtent(Str, Start + startIndex, length);
        }

        public override int GetHashCode()
        {
            return Utilities.GetHashCode(Str, Start, Length);
        }

		public override bool Equals(object? obj) => obj is StringExtent && Equals((StringExtent)obj);

		public bool Equals(StringExtent strB)
        {
            if (Length != strB.Length) { return false; }

            if (Str != strB.Str || Start != strB.Start)
            {
                int end = Start + Length;
                int i = Start;
                int j = strB.Start;

                char[] stra = m_str;
                char[] strb = strB.Str;

                while (i < end)
                {
                    if (stra[i++] != strb[j++]) { return false; }
                }
            }

            return true;
        }

        public bool Equals(string strB)
        {
            return 0 == CompareTo(strB);
        }

        public int CompareTo(StringExtent strB)
        {
            int cmp = 0;

            char[] stra = m_str;
            char[] strb = strB.Str;

            if (strb != stra ||
                strB.Start != Start ||
                strB.Length != Length)
            {
                int i = Start;
                int iEnd = i + Length;
                int j = strB.Start;
                int jEnd = j + strB.Length;

                while (i < iEnd && j < jEnd)
                {
                    if (stra[i] != strb[j])
                    {
                        cmp = stra[i] < strb[j] ? -1 : 1;
                        break;
                    }

                    i++; j++;
                }

                if (0 == cmp)
                {
                    if (i < iEnd) cmp = -1;
                    if (j < jEnd) cmp = 1;
                }
            }

            return cmp;
        }

        public int CompareTo(string strB)
        {
            int cmp = 0;

            char[] strA = m_str;

            int i = Start;
            int iEnd = i + Length;
            int j = 0;
            int jEnd = strB.Length;

            while (i < iEnd && j < jEnd)
            {
                if (strA[i] != strB[j])
                {
                    cmp = strA[i] < strB[j] ? -1 : 1;
                    break;
                }

                i++; j++;
            }

            if (0 == cmp)
            {
                if (i < iEnd) cmp = -1;
                if (j < jEnd) cmp = 1;
            }

            return cmp;
        }

        public bool IsEmpty
        {
            get { return m_str == null || Length == 0; }
        }

        public void Reset()
        {
            m_str = null!;
            Start = Length = 0;
        }

        public void Trim(char[] trimCharacters)
        {
            int m = Length;
            int l = trimCharacters.Length;
            int j = 0;

            for (int i = Start + Length - 1; i >= Start && j < l; i--)
            {
                while (j < l)
                {
                    char c = m_str[i];

                    if (c == trimCharacters[j])
                    {
                        m--;
                        j = 0;
                        break;
                    }

                    j++;
                }
            }

            Length = m;
        }
    }

    /// <summary>
    /// Compares the equality of two string extents
    /// </summary>
    [Serializable]
    public class StringExtentEqualityComparer : IEqualityComparer<StringExtent>
    {
        public static readonly StringExtentEqualityComparer Instance = new StringExtentEqualityComparer();

        public bool Equals(StringExtent x, StringExtent y)
        {
            return x.Equals(y);
        }

        public int GetHashCode(StringExtent x)
        {
            return x.GetHashCode();
        }
    }

    /// <summary>
    /// Allocates new string extents
    /// </summary>
    public interface IStringExtentAllocator
    {
        StringExtent New(int length);
    }

    //[Serializable]
    //public class FixedStringExtentAllocator : IStringExtentAllocator
    //{
    //    char[] m_buffer;

    //    public FixedStringExtentAllocator(int fixedSize)
    //    {
    //        m_buffer = new char[fixedSize];
    //    }

    //    public StringExtent New(int length)
    //    {
    //        if (length > m_buffer.Length)
    //        {
    //            throw new ArgumentOutOfRangeException("Requested StringExtent length is greater than the fixed buffer size.");
    //        }

    //        return new StringExtent(m_buffer, 0, length);
    //    }
    //}

    [Serializable]
    internal class StringExtentHeapAllocator : IStringExtentAllocator
    {
        public StringExtent New(string source)
        {
            StringExtent stringExtent = new StringExtent(new char[source.Length], 0, source.Length);
            source.CopyTo(0, stringExtent.Str, stringExtent.Start, source.Length);
            return stringExtent;
        }

        public StringExtent New(int length)
        {
            return new StringExtent(new char[length], 0, length);
        }
    }

    [Serializable]
    internal class StringExtentBuffer : IStringExtentAllocator
    {
        char[]? buffer;

        public StringExtent New(string source)
        {
            Resize(source.Length);

            StringExtent stringExtent = new StringExtent(buffer!, 0, source.Length);
            source.CopyTo(0, stringExtent.Str, stringExtent.Start, source.Length);

            return stringExtent;
        }

        //public StringExtent New(string source, int bufferLength)
        //{
        //    Resize(bufferLength);

        //    int stringLength = Math.Min(source.Length, bufferLength);

        //    StringExtent stringExtent = new StringExtent(buffer, 0, stringLength);
        //    source.CopyTo(0, stringExtent.Str, stringExtent.Start, stringLength);

        //    return stringExtent;
        //}

        public StringExtent New(int length)
        {
            Resize(length);

            return new StringExtent(buffer!, 0, length);
        }

        public void Resize(int length)
        {
            if (null == buffer || buffer.Length < length)
            {
                buffer = new char[length];
            }
        }

        public static implicit operator char[](StringExtentBuffer seb) { return seb.buffer!; }
    }

    [Serializable]
    internal class StringExtentZoneAllocator : IStringExtentAllocator
    {
        int m_blockSize = 256 * 1024; // REVIEW : MEMORY : PERF
        internal Block m_block;
        Block? m_freeBlocks;
        int m_startPos = 0;
        Int64 m_memoryUsage = 5 * sizeof(int);

        public StringExtentZoneAllocator()
        {
            m_block = NewBlock();
        }

        public StringExtentZoneAllocator(int blockSize)
        {
            Debug.Assert(blockSize <= StringExtent.MaxLength);
            m_blockSize = blockSize;
            m_block = NewBlock();
        }

        public Int64 MemoryUsage { get { return m_memoryUsage; } }

        [Serializable]
        internal sealed class Block
        {
            public Block Next;
            public char[] Buffer;

            public Block(char[] buffer, Block next)
            {
                Buffer = buffer;
                Next = next;
            }
        }

        public StringExtent New(StringExtent source)
        {
            StringExtent stringExtent = New(source.Length);
            source.CopyTo(0, stringExtent.Str, stringExtent.Start, source.Length);
            return stringExtent;
        }

        public StringExtent New(string source)
        {
            StringExtent stringExtent = New(source.Length);
            source.CopyTo(0, stringExtent.Str, stringExtent.Start, source.Length);
            return stringExtent;
        }

        public StringExtent New(string source, int begin, int length)
        {
            StringExtent stringExtent = New(length);
            source.CopyTo(begin, stringExtent.Str, stringExtent.Start, length);
            return stringExtent;
        }

        public StringExtent New(int length)
        {
            if (length > m_blockSize)
            {
                throw new ArgumentOutOfRangeException("Requested StringExtent length is greater than the allocator block size.");
            }

            if (m_startPos + length > m_blockSize)
            {
                m_startPos = 0;
                m_block = NewBlock();
            }

            m_startPos += length;
            return new StringExtent(m_block.Buffer, m_startPos - length, length);
        }

        protected Block NewBlock()
        {
            if (m_freeBlocks != null)
            {
                Block b = m_freeBlocks;
                m_freeBlocks = b.Next;
                b.Next = m_block;
                return b;
            }

            m_memoryUsage += m_blockSize * sizeof(char) + 2 * sizeof(int);

            return new Block(new char[m_blockSize], m_block);
        }

        public void Reset()
        {
            m_startPos = 0;

            while (m_block.Next != null)
            {
                Block b = m_block.Next;
                m_block.Next = m_freeBlocks!;
                m_freeBlocks = m_block;
                m_block = b;
            }
        }
    }
}
