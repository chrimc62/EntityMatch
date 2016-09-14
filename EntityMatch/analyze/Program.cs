using EntityMatch.Utilities;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace analyze
{
    class Program
    {
        public static void Usage()
        {
            Console.WriteLine("analyze <histogram.bin>");
            System.Environment.Exit(-1);
        }

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Usage();
            }
            var path = args[0];
            for(var argi = 1; argi < args.Length; ++argi)
            {
                var arg = args[argi];
            }
            Dictionary<string, Histogram<object>> histograms;
            using (var stream = new FileStream(path, FileMode.Open))
            {
                var serializer = new BinaryFormatter();
                histograms = (Dictionary<string, Histogram<object>>)serializer.Deserialize(stream);
            }
            foreach (var histogram in histograms)
            {
                Console.WriteLine($"{histogram.Key} has {histogram.Value.DistinctValues()} unique values and {histogram.Value.Counts()} counts.");
            }
            foreach (var histogram in histograms)
            {
                Console.WriteLine($"{histogram.Key} has {histogram.Value.DistinctValues()} unique values");
                histogram.Value.Apply((key, count) =>
                {
                    Console.WriteLine($"{key}: {count}");
                });
            }
        }
    }
}
