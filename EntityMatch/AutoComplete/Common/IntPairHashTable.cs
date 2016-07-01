using System;
using System.Collections.Generic;
using System.Text;

namespace Common
{
    [Serializable]
    public class IntPairHashTable
    {
        /// <summary>
        /// Each entry has a key and a timestamp. A key is valid if timestamp is equal to the current timestamp. The hashbucket is empty
        /// if the timestamp is less than the current timestamp. Timestamps are useful to fast Clear() operations.
        /// </summary>
        [Serializable]
        struct HashEntry
        {
            public int key1;
            public int key2;
            public int value;
            public int ts;
        }

        [Serializable]
        struct IntPair
        {
            public int i1;
            public int i2;

            public IntPair(int i1, int i2)
            {
                this.i1 = i1;
                this.i2 = i2;
            }
        }

        IntPair tmp = new IntPair();

        public int Count { get; private set; }

        /// <summary>
        /// m_hashBuckets.Length = GrowthFactor * suggested capacity for the set
        /// </summary>
        private const float GrowthFactor = 2.0f;

        /// <summary>
        /// The hash buckets storing the elements of the set. We ensure that the bucket sizes are powers of two to ensure that the operation
        /// of finding a bucket can be implemented as a fast bit operation.
        /// </summary>
        private HashEntry[] m_hashBuckets;

        private readonly int m_hashBucketMask;

        private int m_curTimestamp;

        private Dictionary<IntPair, int> m_fallbackDictionary;

        public IntPairHashTable(int suggestedCapacity)
        {
            int minHashTableCapacity = (int)(suggestedCapacity * GrowthFactor);

            for (m_hashBucketMask = 1; m_hashBucketMask < minHashTableCapacity && m_hashBucketMask > 0; )
            {
                m_hashBucketMask = (m_hashBucketMask << 1) | 1;
            }

            // Overflow checks
            if (m_hashBucketMask <= 0 || m_hashBucketMask + 1 <= 0)
            {
                // TODO: not able to add a localized error message
                throw new Exception();
            }

            m_hashBuckets = new HashEntry[m_hashBucketMask + 1];
            m_curTimestamp = 1;
            m_fallbackDictionary = new Dictionary<IntPair, int>();
            Count = 0;
        }

        private static int GetHashCode(int i1, int i2)
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

        public void Add(int key1, int key2, int value)
        {            
            int hash = GetHashCode(key1, key2);
            int bucketId = hash & m_hashBucketMask;

            if (m_hashBuckets[bucketId].ts < m_curTimestamp)
            {
                // No collision with an existing key
                m_hashBuckets[bucketId].key1 = key1;
                m_hashBuckets[bucketId].key2 = key2;
                m_hashBuckets[bucketId].value = value;
                m_hashBuckets[bucketId].ts = m_curTimestamp;
            }
            else if (m_hashBuckets[bucketId].key1 == key1 && m_hashBuckets[bucketId].key2 == key2)
            {
                throw new Exception("Multiple insertions");
            }
            else
            {
                // Collides with an existing key - use fallback hashtable
                m_fallbackDictionary.Add(new IntPair(key1, key2), value);
            }
            Count++;
        }

        public void Update(int key1, int key2, int value)
        {
            int hash = GetHashCode(key1, key2);
            int bucketId = hash & m_hashBucketMask;

            if (m_hashBuckets[bucketId].ts < m_curTimestamp)
            {
                // No collision with an existing key
                //m_hashBuckets[bucketId].key1 = key1;
                //m_hashBuckets[bucketId].key2 = key2;
                //m_hashBuckets[bucketId].value = value;
                //m_hashBuckets[bucketId].ts = m_curTimestamp;
            }
            else if (m_hashBuckets[bucketId].key1 == key1 && m_hashBuckets[bucketId].key2 == key2)
            {
                m_hashBuckets[bucketId].value = value;
                m_hashBuckets[bucketId].ts = m_curTimestamp;
            }
            else
            {
                // Collides with an existing key - use fallback hashtable
                tmp.i1 = key1;
                tmp.i2 = key2;
                m_fallbackDictionary[tmp] = value;
                //m_fallbackDictionary.Add(new IntPair(key1, key2), value);
            }
        }


        public void Clear()
        {
            m_curTimestamp++;

            // handle overflow
            if (m_curTimestamp == int.MaxValue)
            {
                for (int i = 0; i < m_hashBuckets.Length; i++)
                {
                    m_hashBuckets[i].ts = 0;
                }
                m_curTimestamp = 1;
            }

            m_fallbackDictionary.Clear();
            Count = 0;
        }

        public bool ContainsKey(int key1, int key2)
        {
            int hash = GetHashCode(key1, key2);
            int bucketId = hash & m_hashBucketMask;

            if (m_hashBuckets[bucketId].ts < m_curTimestamp)
            {
                return false;
            }

            if (m_hashBuckets[bucketId].key1 == key1 && m_hashBuckets[bucketId].key2 == key2)
            {
                return true;
            }

            return m_fallbackDictionary.ContainsKey(new IntPair(key1, key2));
        }

        public bool TryGetValue(int key1, int key2, out int value)
        {
            int hash = GetHashCode(key1, key2);
            int bucketId = hash & m_hashBucketMask;

            if (m_hashBuckets[bucketId].ts < m_curTimestamp)
            {
                value = 0;
                return false;
            }

            if (m_hashBuckets[bucketId].key1 == key1 && m_hashBuckets[bucketId].key2 == key2)
            {
                value = m_hashBuckets[bucketId].value;
                return true;
            }

            return m_fallbackDictionary.TryGetValue(new IntPair(key1, key2), out value);
        }
    }
}
