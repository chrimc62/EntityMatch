using EntityMatch.Utilities;
using Newtonsoft.Json.Linq;
using System.Runtime.Serialization.Formatters.Binary;

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
#pragma warning disable SYSLIB0011 // Type or member is obsolete
				histograms = (Dictionary<string, Histogram<object>>)serializer.Deserialize(stream);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
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
