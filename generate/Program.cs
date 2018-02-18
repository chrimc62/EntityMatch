using EntityMatch.Utilities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace Search.Generate
{
    class Program
    {
        static void Usage()
        {
            System.Environment.Exit(-1);
        }

        static void Main(string[] args)
        {
            if (args.Count() < 2)
            {
                Usage();
            }
            string modelPath = args[0];
            string histogramPath = args[1];
            for(var i = 1; i < args.Count(); ++i)
            {
                var arg = args[i];
            }
            var model = JObject.Parse(File.ReadAllText(modelPath));
            Dictionary<string, Histogram<object>> histograms;
            using (var stream = new FileStream(histogramPath, FileMode.Open))
            {
                var serializer = new BinaryFormatter();
                histograms = (Dictionary<string, Histogram<object>>)serializer.Deserialize(stream);
            }
            foreach(var histogram in histograms)
            {
                // Numeric
                // String with small number of values
                // String with large number of values
                // Really need schema too to check on stuff from that
            }
        }
    }
}
