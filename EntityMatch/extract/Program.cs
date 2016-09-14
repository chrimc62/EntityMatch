using EntityMatch.Utilities;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace extract
{
    class Program
    {
        static int Apply(SearchIndexClient client, string valueField, string idField, string text, SearchParameters sp, Action<int, SearchResult> function,
            int max = int.MaxValue,
            int page = 1000)
        {
            var originalFilter = sp.Filter;
            var originalOrder = sp.OrderBy;
            var originalTop = sp.Top;
            var originalSkip = sp.Skip;
            var total = 0;
            object lastValue = null;
            object lastID = null;
            sp.OrderBy = new string[] { valueField };
            sp.Top = page;
            var results = client.Documents.Search(text, sp).Results;
            while (total < max && results.Any())
            {
                bool skipping = lastValue != null;
                bool newValue = false;
                int row = 0;
                int firstRowWithValue = 0;
                foreach (var result in results)
                {
                    var id = result.Document[idField];
                    if (skipping)
                    {
                        // Skip until we find the last processed id
                        skipping = !id.Equals(lastID);
                    }
                    else
                    {
                        var value = result.Document[valueField];
                        function(total, result);
                        lastID = id;
                        if (!value.Equals(lastValue))
                        {
                            firstRowWithValue = row;
                            lastValue = value;
                            newValue = true;
                        }
                        if (++total == max)
                        {
                            break;
                        }
                    }
                    ++row;
                }
                if (skipping)
                {
                    throw new Exception($"Could not find id {lastID} in {lastValue}");
                }
                if (row == 1)
                {
                    // Last row in the table
                    break;
                }
                var toSkip = row - firstRowWithValue - 1;
                if (newValue)
                {
                    sp.Skip = toSkip;
                }
                else
                {
                    sp.Skip += toSkip;
                }
                sp.Filter = (originalFilter == null ? "" : $"({originalFilter}) and ") + $"{valueField} ge {lastValue}";
                results = client.Documents.Search(text, sp).Results;
            }
            sp.Filter = originalFilter;
            sp.OrderBy = originalOrder;
            sp.Top = originalTop;
            sp.Skip = originalSkip;
            return total;
        }

        static int Apply(SearchIndexClient client, string order, string text, SearchParameters sp, Action<int, SearchResult> function,
            int max = int.MaxValue,
            int page = 1000)
        {
            // Use order to set orderby and keep value to skip through--what if someting has > 1000 values?
            sp.Skip = 0;
            sp.Top = page;
            var search = client.Documents.Search(text, sp);
            var results = search.Results;
            while (sp.Skip < max && results.Any())
            {
                foreach (var result in results)
                {
                    function(sp.Skip.Value, result);
                    ++sp.Skip;
                    if (sp.Skip == max)
                    {
                        break;
                    }
                }
                results = client.Documents.Search(text, sp).Results;
            }
            return sp.Skip.Value;
        }

        static void Process(int count,
            SearchResult result,
            IEnumerable<string> fields, Dictionary<string, Histogram<object>> histograms)
        {
            var doc = result.Document;
            foreach (var field in fields)
            {
                var value = doc[field];
                if (value != null)
                {
                    Histogram<object> histogram;
                    if (!histograms.TryGetValue(field, out histogram))
                    {
                        histogram = histograms[field] = new Histogram<object>();
                    }
                    histogram.Add(value);
                }
            }
            if ((count % 100) == 0)
            {
                Console.Write($"\n{count}: ");
            }
            else
            {
                Console.Write(".");
            }
        }

        static void Main(string[] args)
        {
            var searchServiceName = ConfigurationManager.AppSettings["SearchServiceName"];
            var queryApiKey = ConfigurationManager.AppSettings["SearchCredentials"];
            var indexClient = new SearchIndexClient(searchServiceName, "listings", new SearchCredentials(queryApiKey));
            // Cannot handle location yet
            var facets = new string[] { "beds", "baths", "sqft", "daysOnMarket", "status", "source", "street", "type", "city", "district", "region", "zipcode", "countryCode", "price" };
            var values = new string[] { "description" };
            var path = ".";
            var histograms = new Dictionary<string, Histogram<object>>();
            var valueCount = new Dictionary<string, List<string>>();
            var sp = new SearchParameters();
            var timer = Stopwatch.StartNew();
            var results = Apply(indexClient, "price", "listingId", null, sp,
                (count, result) =>
                {
                    Process(count, result, facets, histograms);
                }
                // , 1000
                );
            /*
            var results = Apply(indexClient, "zipcode", null, sp,
                (count, result) =>
                {
                    Process(count, result, facets, facetCount, values, valueCount);
                }
                // , 20
                );
                */
            Console.WriteLine($"\nFound {results} in {timer.Elapsed.TotalSeconds}s");
            using (var stream = new FileStream(Path.Combine(path, "-histograms.bin"), FileMode.Create))
            {
                var serializer = new BinaryFormatter();
                serializer.Serialize(stream, histograms);
            }
        }
    }
}
