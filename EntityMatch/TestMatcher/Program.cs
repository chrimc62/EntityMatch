using Autofac;
using EntityMatch;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TestMatcher
{
    class Program
    {
        // Scoring
        // %word = # matched / total phrase
        // word match = sum of 1.0 or 0.9 on if original word matched phrase
        // word adjacenct = keep adding X to previous as long as adjacent.  If X is one, then max possible is 1/2*n(n + 1).
        // word rarity is a constant for a given span.  1/#phrases per word.
        // 
        static void ReadPhrases(string path, IMatcher matcher)
        {
            var timer = Stopwatch.StartNew();
            int count = 0;
            using (var stream = new StreamReader(path))
            {
                var splitter = new Regex("(?:^|,)(?:(?:\"((?:[^\"]|\"\")*)\")|([^,\"]*))", RegexOptions.Compiled);
                Func<string, string[]> split = (input) => (from Match match in splitter.Matches(input) select match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value).ToArray();
                var columns = split(stream.ReadLine());
                while (!stream.EndOfStream)
                {
                    var line = stream.ReadLine();
                    var values = split(line);
                    matcher.AddEntities("DVD", values[0]);
                    ++count;
                }
            }
            Console.WriteLine($"Reading {count} phrases from {path} took {timer.Elapsed.TotalSeconds}s");
        }

        static void TestLoop(IMatcher matcher)
        {
            string input;
            Console.Write("\nInput: ");
            while (!string.IsNullOrWhiteSpace(input = Console.ReadLine()))
            {
                var interpretations = matcher.Interpretations(input);
                foreach (var interpretation in interpretations)
                {
                    Console.WriteLine($"{interpretation}");
                }
                Console.Write("\nInput: ");
            }
        }

        private static IContainer Container { get; set; }

        static void Main(string[] args)
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<SimpleTokenizer>().As<ITokenizer>().SingleInstance();
            builder.RegisterType<EntitiesDictionary>().As<IEntities>().SingleInstance();
            builder.Register((c) => new SynonymAlternatives(new SpellingAlternatives(new BaseAlternatives())))
                .As<IAlternatives>()
                .As<SynonymAlternatives>()
                .SingleInstance();
            builder.RegisterType<Recognizer>().As<IEntityRecognizer>().SingleInstance();
            builder.RegisterType<Matcher>().As<IMatcher>(); ;
            Container = builder.Build();
            using (var scope = Container.BeginLifetimeScope())
            {
                var matcher = scope.Resolve<IMatcher>();
                var synonyms = scope.Resolve<SynonymAlternatives>();
                synonyms.AddAlternatives("mouse", new Alternative("mouse", 1.0), new Alternative("mice", 0.9));
                ReadPhrases(@"c:\tmp\DVD.txt", matcher);
                TestLoop(matcher);
            }
        }
    }
}
