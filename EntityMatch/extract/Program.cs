using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using System;
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
            IEnumerable<string> facets, Dictionary<string, Dictionary<string, int>> faceted, 
            IEnumerable<string> values, Dictionary<string, List<string>> unique)
        {
            var doc = result.Document;
            foreach(var field in facets)
            {
                var value = (string) doc[field];
                if (value != null)
                {
                    Dictionary<string, int> counts;
                    if (!faceted.TryGetValue(field, out counts))
                    {
                        counts = faceted[field] = new Dictionary<string, int>();
                    }
                    if (!counts.ContainsKey(value))
                    {
                        counts[value] = 1;
                    }
                    else
                    {
                        ++counts[value];
                    }
                }
            }
            foreach(var field in values)
            {
                var value = (string)doc[field];
                if (value != null)
                {
                    if (!unique.ContainsKey(field))
                    {
                        unique[field] = new List<string>();
                    }
                    unique[field].Add(value);
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
            var facets = new string[] { "status", "street", "type", "city", "district", "region", "zipcode", "countryCode" };
            var values = new string[] { "description" };
            var path = ".";
            var facetCount = new Dictionary<string, Dictionary<string, int>>();
            var valueCount = new Dictionary<string, List<string>>();
            var sp = new SearchParameters();
            var timer = Stopwatch.StartNew();
            var results = Apply(indexClient, "zipcode", null, sp,
                (count, result) =>
                {
                    Process(count, result, facets, facetCount, values, valueCount);
                }
                // , 20
                );
            Console.WriteLine($"\nFound {results} in {timer.Elapsed.TotalSeconds}s");
            foreach(var field in facetCount)
            {
                using (var stream = new StreamWriter(Path.Combine(path, field.Key + "-facet.txt")))
                {
                    foreach(var value in field.Value.Keys)
                    {
                        stream.WriteLine(value);
                    }
                }
            }
            foreach(var field in valueCount)
            {
                using (var stream = new StreamWriter(Path.Combine(path, field.Key + ".txt")))
                {
                    foreach (var value in field.Value)
                    {
                        stream.WriteLine(value);
                    }
                }
            }
        }
    }
}
