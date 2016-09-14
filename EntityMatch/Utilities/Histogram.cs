using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityMatch.Utilities
{
    [Serializable]
    public class Histogram<T>
    {
        private SortedDictionary<T, int> _counts = new SortedDictionary<T, int>();

        public int Add(T value)
        {
            int count;
            if (_counts.TryGetValue(value, out count))
            {
                ++_counts[value];
            }
            else
            {
                count = 1;
                _counts.Add(value, 1);
            }
            return count;
        }

        public int Count(T value)
        {
            int count;
            if (!_counts.TryGetValue(value, out count))
            {
                count = 1;
            }
            return count;
        }

        public IEnumerable<T> Values()
        {
            foreach (var value in _counts.Keys)
            {
                yield return value;
            }
        }

        public int DistinctValues()
        {
            return _counts.Count();
        }

        public int Counts()
        {
            return _counts.Values.Sum();
        }

        public void Apply(Action<T, int> function)
        {
            foreach (var entry in _counts)
            {
                function(entry.Key, entry.Value);
            }
        }

        public void Save(Stream stream)
        {
            // string, int32, int64, float, double, boolean, datetime, collection, geography point
            var serializer = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            serializer.Serialize(stream, _counts);
        }

        public void Load(Stream stream)
        {
            var serializer = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            _counts = (SortedDictionary<T, int>)serializer.Deserialize(stream);
        }
    }
}
