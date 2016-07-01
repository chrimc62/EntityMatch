using System;
using System.Collections.Generic;
using System.Text;

namespace Common
{
    public class IntHashTable 
    {
        /// <summary>
        /// Each entry has a key and a timestamp. A key is valid if timestamp is equal to the current timestamp. The hashbucket is empty
        /// if the timestamp is less than the current timestamp. Timestamps are useful to fast Clear() operations.
        /// </summary>
        [Serializable]
        struct HashEntry
        {
            public int key;
            public int value;
            public int ts;
        }

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

        private Dictionary<int, int> m_fallbackDictionary;

        public IntHashTable(int suggestedCapacity)
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
                throw new InvalidOperationException();
            }

            m_hashBuckets = new HashEntry[m_hashBucketMask + 1];
            m_curTimestamp = 1;
            m_fallbackDictionary = new Dictionary<int, int>();
        }

        private static int GetHashCode(int key)
        {
            return Utilities.GetHashCode(key);
        }

        public void Add(int key, int value)
        {
            int hash = GetHashCode(key);
            int bucketId = hash & m_hashBucketMask;

            if (m_hashBuckets[bucketId].ts < m_curTimestamp)
            {
                // No collision with an existing key
                m_hashBuckets[bucketId].key = key;
                m_hashBuckets[bucketId].value = value;
                m_hashBuckets[bucketId].ts = m_curTimestamp;
            }
            else if (m_hashBuckets[bucketId].key == key)
            {
                throw new InvalidOperationException();
            }
            else
            {
                // Collides with an existing key - use fallback hashtable
                m_fallbackDictionary.Add(key, value);
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
        }

        public bool ContainsKey(int key)
        {
            int hash = GetHashCode(key);
            int bucketId = hash & m_hashBucketMask;

            if (m_hashBuckets[bucketId].ts < m_curTimestamp)
            {
                return false;
            }

            if (m_hashBuckets[bucketId].key == key)
            {
                return true;
            }

            return m_fallbackDictionary.ContainsKey(key);
        }

        public bool TryGetValue(int key, out int value)
        {
            int hash = GetHashCode(key);
            int bucketId = hash & m_hashBucketMask;

            if (m_hashBuckets[bucketId].ts < m_curTimestamp)
            {
                value = 0;
                return false;
            }

            if (m_hashBuckets[bucketId].key == key)
            {
                value = m_hashBuckets[bucketId].value;
                return true;
            }

            return m_fallbackDictionary.TryGetValue(key, out value);
        }

        public int this[int key]
        {
            get
            {
                int hash = GetHashCode(key);
                int bucketId = hash & m_hashBucketMask;

                if (m_hashBuckets[bucketId].ts < m_curTimestamp)
                {
                    throw new InvalidOperationException();
                }

                if (m_hashBuckets[bucketId].key == key)
                {
                    return m_hashBuckets[bucketId].value;
                }

                return m_fallbackDictionary[key];
            }
            set
            {
                int hash = GetHashCode(key);
                int bucketId = hash & m_hashBucketMask;

                if (m_hashBuckets[bucketId].ts < m_curTimestamp)
                {
                    m_hashBuckets[bucketId].key = key;
                    m_hashBuckets[bucketId].value = value;
                    m_hashBuckets[bucketId].ts = m_curTimestamp;
                }
                else if (m_hashBuckets[bucketId].key == key)
                {
                    m_hashBuckets[bucketId].value = value;
                }
                else
                {
                    m_fallbackDictionary[key] = value;
                }
            }
        }

        public Int64 MemoryUsage
        {
            get { return m_hashBuckets.Length * sizeof(int) * 2 + m_fallbackDictionary.Count * sizeof(int) * 2; }
        }
    }
}
