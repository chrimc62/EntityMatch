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
            Console.WriteLine("analyze <histogram.bin> [optional args]");
            Console.WriteLine("By default print a high level analysis.");
            Console.WriteLine("-a : sort alphabetically.  (Default is by descending counts.)");
            Console.WriteLine("-m <max> : Max number of items in listing. (Default is 5000 LUIS limit.)");
            Console.WriteLine("-p : Generate a phrases.json file with a phrase list for each string column.");
            Console.WriteLine("-t <threshold> : Drop any value with less than threshold counts from listing.");
            Console.WriteLine("-l : List the results to the console.");
            System.Environment.Exit(-1);
        }

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Usage();
            }
            var path = args[0];
            int threshold = int.MinValue;
            int max = 5000;
            bool alphaSort = false;
            bool phraseList = false;
            bool list = false;
            for (var argi = 1; argi < args.Length; ++argi)
            {
                var arg = args[argi];
                if (arg.StartsWith("-t"))
                {
                    if (++argi < args.Length)
                    {
                        threshold = int.Parse(args[argi]);
                    }
                    else
                    {
                        Usage();
                    }
                }
                else if (arg.StartsWith("-m"))
                {
                    if (++argi < args.Length)
                    {
                        max = int.Parse(args[argi]);
                    }
                    else
                    {
                        Usage();
                    }
                }
                else if (arg.StartsWith("-a"))
                {
                    alphaSort = true;
                }
                else if (arg.StartsWith("-p"))
                {
                    phraseList = true;
                }
                else if (arg.StartsWith("-l"))
                {
                    list = true;
                }
            }
            Dictionary<string, Histogram<object>> histograms;
            using (var stream = new FileStream(path, FileMode.Open))
            {
                var serializer = new BinaryFormatter();
#pragma warning disable SYSLIB0011 // Type or member is obsolete
				histograms = (Dictionary<string, Histogram<object>>)serializer.Deserialize(stream);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
			}
            foreach (var histogram in histograms)
            {
                var values = histogram.Value.Values();
                Console.WriteLine($"{histogram.Key} has {values.Count()} unique values with a range of [{values.Min()}, {values.Max()}] and {histogram.Value.Counts().Sum()} counts.");
            }
            Func<Histogram<object>, IEnumerable<KeyValuePair<object, int>>> seq = (histogram) =>
            {
                var pairs = (from pair in histogram.Pairs() where pair.Value >= threshold orderby pair.Value descending select pair).Take(max);
                if (alphaSort)
                {
                    pairs = from pair in pairs orderby pair.Key ascending select pair;
                }
                return pairs;
            };
            if (phraseList)
            {
                Console.WriteLine($"Generating phrases.json with up to {max} phrases and at least {threshold} counts.");
                using (var stream = new StreamWriter($"phrases.json"))
                {
                    var listSeperator = "";
                    alphaSort = true;
                    foreach (var histogram in histograms)
                    {
                        var values = histogram.Value.Values();
                        var type = values.FirstOrDefault()!.GetType();
                        if (type == typeof(string))
                        {
                            stream.Write(listSeperator);
                            stream.Write($"{{\"name\":\"{histogram.Key}\", \"mode\":true, \"words\":\"");
                            listSeperator = ",\n";
                            var seperator = "";
                            var count = 0;
                            foreach (var pair in seq(histogram.Value))
                            {
                                var key = pair.Key.ToString()!.Replace(",", @"\,").Replace("\"", "");
                                if (!string.IsNullOrWhiteSpace(key))
                                {
                                    stream.Write(seperator);
                                    stream.Write(key);
                                    seperator = ",";
                                    ++count;
                                }
                            }
                            stream.Write(@""", ""activated"":true}");
                            Console.WriteLine($"Generated {histogram.Key} with {count} phrases");
                        }
                    }
                }
            }
            else if (list)
            {
                foreach (var histogram in histograms)
                {
                    Console.WriteLine($"{histogram.Key}");
                    foreach (var pair in seq(histogram.Value))
                    {
                        var key = pair.Key.ToString()!.Replace(",", "\\,");
                        Console.WriteLine($"{key}: {pair.Value}");
                    }
                }
            }
        }
    }
}
