static partial class Extensions
{
    public static IEnumerable<T> IntersectBinarySearch<T>(this T[] sequence1, T[] sequence2, IComparer<T> comparer = null)
    {
        // Sequence2 is longer
        if (sequence1.Length > sequence2.Length)
        {
            var temp = sequence1;
            sequence1 = sequence2;
            sequence2 = temp;
        }

        foreach (var match in sequence1)
        {
            if (Array.BinarySearch<T>(sequence2, match, comparer) == 0)
            {
                yield return match;
            }
        }
    }


    public static IEnumerable<int> IntersectSorted(this IEnumerable<int> sequence1, IEnumerable<int> sequence2)
    {
        using (var cursor1 = sequence1.GetEnumerator())
        using (var cursor2 = sequence2.GetEnumerator())
        {
            if (!cursor1.MoveNext() || !cursor2.MoveNext())
            {
                yield break;
            }
            var value1 = cursor1.Current;
            var value2 = cursor2.Current;

            while (true)
            {
                if (value1 < value2)
                {
                    if (!cursor1.MoveNext())
                    {
                        yield break;
                    }
                    value1 = cursor1.Current;
                }
                else if (value1 > value2)
                {
                    if (!cursor2.MoveNext())
                    {
                        yield break;
                    }
                    value2 = cursor2.Current;
                }
                else
                {
                    yield return value1;
                    if (!cursor1.MoveNext() || !cursor2.MoveNext())
                    {
                        yield break;
                    }
                    value1 = cursor1.Current;
                    value2 = cursor2.Current;
                }
            }
        }
    }


    public static IEnumerable<int> DifferenceSorted(this IEnumerable<int> sequence1, IEnumerable<int> sequence2)
    {
        using (var cursor1 = sequence1.GetEnumerator())
        using (var cursor2 = sequence2.GetEnumerator())
        {
            var continue1 = cursor1.MoveNext();
            var continue2 = cursor2.MoveNext();
            var value1 = continue1 ? cursor1.Current : default(int);
            var value2 = continue2 ? cursor2.Current : default(int);
            while (continue1 && continue2)
            {
                if (value1 < value2)
                {
                    yield return value1;
                    continue1 = cursor1.MoveNext();
                    if (continue1) value1 = cursor1.Current;
                }
                else if (value1 > value2)
                {
                    continue2 = cursor2.MoveNext();
                    if (continue2) value2 = cursor2.Current;
                }
                else
                {
                    continue1 = cursor1.MoveNext();
                    if (continue1) value1 = cursor1.Current;
                    continue2 = cursor2.MoveNext();
                    if (continue2) value2 = cursor2.Current;
                }
            }
            if (continue1)
            {
                do
                {
                    yield return cursor1.Current;
                } while (cursor1.MoveNext());
            }
        }
    }

    // Intersect sequence1 and 2 and put everyhing in 
    public static void SplitSorted(this IEnumerable<int> sequence1, IEnumerable<int> sequence2, ICollection<int> intersection, ICollection<int> difference)
    {
        using (var cursor1 = sequence1.GetEnumerator())
        using (var cursor2 = sequence2.GetEnumerator())
        {
            if (!cursor2.MoveNext())
            {
                while (cursor1.MoveNext())
                {
                    difference.Add(cursor1.Current);
                }
            }
            else if (cursor1.MoveNext())
            {
                var value1 = cursor1.Current;
                var value2 = cursor2.Current;

                while (true)
                {
                    if (value1 < value2)
                    {
                        difference.Add(value1);
                        if (!cursor1.MoveNext())
                        {
                            break;
                        }
                        value1 = cursor1.Current;
                    }
                    else if (value1 > value2)
                    {
                        if (!cursor2.MoveNext())
                        {
                            do
                            {
                                difference.Add(cursor1.Current);
                            } while (cursor1.MoveNext());
                            break;
                        }
                        value2 = cursor2.Current;
                    }
                    else
                    {
                        intersection.Add(value1);
                        if (!cursor1.MoveNext())
                        {
                            break;
                        }
                        else if (!cursor2.MoveNext())
                        {
                            do
                            {
                                difference.Add(cursor1.Current);
                            } while (cursor1.MoveNext());
                            break;
                        }
                        value1 = cursor1.Current;
                        value2 = cursor2.Current;
                    }
                }
            }
        }
    }
}

public static IEnumerable<T> IntersectSortedArray<T>(this T[] sequence1, T[] sequence2, IComparer<T> comparer = null)
{
    if (comparer == null)
    {
        comparer = Comparer<T>.Default;
    }
    var i1 = 0;
    var i2 = 0;
    while (i1 < sequence1.Length && i2 < sequence2.Length)
    {
        var value1 = sequence1[i1];
        var value2 = sequence2[i2];
        var comparison = comparer.Compare(value1, value2);
        if (comparison < 0)
        {
            ++i1;
        }
        else if (comparison > 0)
        {
            ++i2;
        }
        else
        {
            ++i1;
            ++i2;
            yield return value1;
        }
    }
}

public static IEnumerable<T> IntersectSortedEnumerable<T>(this IEnumerable<T> sequence1, IEnumerable<T> sequence2, IComparer<T> comparer = null)
{
    if (comparer == null)
    {
        comparer = Comparer<T>.Default;
    }
    using (var cursor1 = sequence1.GetEnumerator())
    using (var cursor2 = sequence2.GetEnumerator())
    {
        if (!cursor1.MoveNext() || !cursor2.MoveNext())
        {
            yield break;
        }
        var value1 = cursor1.Current;
        var value2 = cursor2.Current;

        while (true)
        {
            int comparison = comparer.Compare(value1, value2);
            if (comparison < 0)
            {
                if (!cursor1.MoveNext())
                {
                    yield break;
                }
                value1 = cursor1.Current;
            }
            else if (comparison > 0)
            {
                if (!cursor2.MoveNext())
                {
                    yield break;
                }
                value2 = cursor2.Current;
            }
            else
            {
                yield return value1;
                if (!cursor1.MoveNext() || !cursor2.MoveNext())
                {
                    yield break;
                }
                value1 = cursor1.Current;
                value2 = cursor2.Current;
            }
        }
    }
}

public static IEnumerable<int> IntersectSortedIntArray(this int[] sequence1, int[] sequence2)
{
    var i1 = 0;
    var i2 = 0;
    while (i1 < sequence1.Length && i2 < sequence2.Length)
    {
        var value1 = sequence1[i1];
        var value2 = sequence2[i2];
        if (value1 < value2)
        {
            ++i1;
        }
        else if (value1 > value2)
        {
            ++i2;
        }
        else
        {
            ++i1;
            ++i2;
            yield return value1;
        }
    }
}

public static IEnumerable<T> UnionSortedArray<T>(this T[] sequence1, T[] sequence2, IComparer<T> comparer = null)
{
    if (comparer == null)
    {
        comparer = Comparer<T>.Default;
    }
    var i1 = 0;
    var i2 = 0;
    while (i1 < sequence1.Length && i2 < sequence2.Length)
    {
        var value1 = sequence1[i1];
        var value2 = sequence2[i2];
        var comparison = comparer.Compare(value1, value2);
        if (comparison < 0)
        {
            ++i1;
            yield return value1;
        }
        else if (comparison > 0)
        {
            ++i2;
            yield return value2;
        }
        else
        {
            ++i1;
            ++i2;
            yield return value1;
        }
    }
}

public static IEnumerable<T> UnionSortedEnumerable<T>(this IEnumerable<T> sequence1, IEnumerable<T> sequence2, IComparer<T> comparer = null)
{
    if (comparer == null)
    {
        comparer = Comparer<T>.Default;
    }
    using (var cursor1 = sequence1.GetEnumerator())
    using (var cursor2 = sequence2.GetEnumerator())
    {
        var continue1 = cursor1.MoveNext();
        var continue2 = cursor2.MoveNext();
        var value1 = continue1 ? cursor1.Current : default(T);
        var value2 = continue2 ? cursor2.Current : default(T);
        while (continue1 && continue2)
        {
            var comparison = comparer.Compare(value1, value2);
            if (comparison < 0)
            {
                yield return value1;
                continue1 = cursor1.MoveNext();
                if (continue1) value1 = cursor1.Current;
            }
            else if (comparison > 0)
            {
                yield return value2;
                continue2 = cursor2.MoveNext();
                if (continue2) value2 = cursor2.Current;
            }
            else
            {
                yield return value1;
                continue1 = cursor1.MoveNext();
                if (continue1) value1 = cursor1.Current;
                continue2 = cursor2.MoveNext();
                if (continue2) value2 = cursor2.Current;
            }
        }
        if (!continue1)
        {
            while (continue2)
            {
                yield return cursor2.Current;
                continue2 = cursor2.MoveNext();
            }
        }
        else
        {
            while (continue1)
            {
                yield return cursor1.Current;
                continue1 = cursor1.MoveNext();
            }
        }
    }
}

static void Main(string[] args)
{
    {
        var timer = Stopwatch.StartNew();
        var last = words.First().Value.ToArray();
        double count = 0.0;
        foreach (var entry in words)
        {
            var values = entry.Value.ToArray();
            var intersection = last.IntersectSortedArray(values);
            count += intersection.Count();
            last = values;
        }
        Console.WriteLine($"Average array intersection {count / words.Count()} in {timer.Elapsed.TotalMilliseconds / words.Count()}ms");
    }

    {
        var timer = Stopwatch.StartNew();
        var last = words.First().Value.ToArray();
        double count = 0.0;
        foreach (var entry in words)
        {
            var values = entry.Value.ToArray();
            var intersection = last.IntersectSortedIntArray(values);
            count += intersection.Count();
            last = values;
        }
        Console.WriteLine($"Average int array intersection {count / words.Count()} in {timer.Elapsed.TotalMilliseconds / words.Count()}ms");
    }

    {
        var timer = Stopwatch.StartNew();
        var count = 0.0;
        var last = words.First().Value;
        foreach (var entry in words)
        {
            var intersection = last.IntersectSortedEnumerable(entry.Value);
            count += intersection.Count();
            last = entry.Value;
        }
        Console.WriteLine($"Average enumerable intersection {count / words.Count()} in {timer.Elapsed.TotalMilliseconds / words.Count()}ms");
    }
    {
        var timer = Stopwatch.StartNew();
        var count = 0.0;
        var first = words.First().Value;
        var second = first;
        var third = first;
        foreach (var entry in words)
        {
            var union1 = first.Union(second);
            var union2 = third.Union(entry.Value);
            var intersection = union1.IntersectSortedEnumerable(union2);
            count += intersection.Count();
            first = second;
            second = third;
            third = entry.Value;
        }
        Console.WriteLine($"Average enumerable union intersection {count / words.Count()} in {timer.Elapsed.TotalMilliseconds / words.Count()}ms");
    }

    {
        var timer = Stopwatch.StartNew();
        var count = 0.0;
        var first = words.First().Value;
        var second = first;
        var third = first;
        foreach (var entry in words)
        {
            var union1 = first.Union(second);
            var union2 = third.Union(entry.Value);
            var intersection = union1.ToArray().IntersectSortedArray(union2.ToArray());
            count += intersection.Count();
            first = second;
            second = third;
            third = entry.Value;
        }
        Console.WriteLine($"Average array union intersection {count / words.Count()} in {timer.Elapsed.TotalMilliseconds / words.Count()}ms");
    }

    {
        var timer = Stopwatch.StartNew();
        var entries = words.Values.ToArray();
        var rand = new Random();
        double count = 0.0;
        for (var i = 0; i < 100; ++i)
        {
            IEnumerable<int> union = new int[0];
            for (var s = 0; s < 20; ++s)
            {
                union = union.UnionSortedEnumerable(entries[rand.Next(entries.Length)]);
            }
            count += union.Count();
        }
        Console.WriteLine($"Union all enumerable 100 * 20 {count / 2000} took {timer.Elapsed.TotalMilliseconds / 2000}ms");
    }

}

            {
                var timer = Stopwatch.StartNew();
var count = 0.0;
var last = wordToPhrases.First().Value;
                foreach (var entry in wordToPhrases)
                {
                    var intersection = last.IntersectSorted(entry.Value);
count += intersection.Count();
                    last = entry.Value;
                }
                Console.WriteLine($"Average int enumerable intersection {count / wordToPhrases.Count()} in {timer.Elapsed.TotalMilliseconds / wordToPhrases.Count()}ms");
            }

            {
                var timer = Stopwatch.StartNew();
var entries = wordToPhrases.Values.ToArray();
var rand = new Random();
double count = 0.0;
                for (var i = 0; i< 100; ++i)
                {
                    IEnumerable<int> union = new int[0];
                    for (var s = 0; s< 20; ++s)
                    {
                        union = union.UnionSorted(entries[rand.Next(entries.Length)]);
                    }
                    count += union.Count();
                }
                Console.WriteLine($"Union all int enumerable 100 * 20 {count / 2000} took {timer.Elapsed.TotalMilliseconds / 2000}ms");
            }

            {
                var timer = Stopwatch.StartNew();
var count = 0.0;
var last = wordToPhrases.First().Value;
                foreach (var entry in wordToPhrases)
                {
                    var difference = last.DifferenceSorted(entry.Value);
count += difference.Count();
                    last = entry.Value;
                }
                Console.WriteLine($"Average int enumerable difference {count / wordToPhrases.Count()} in {timer.Elapsed.TotalMilliseconds / wordToPhrases.Count()}ms");
            }

            int loops = 10;
System.GC.Collect();
            {
                var timer = Stopwatch.StartNew();
var count = 0.0;
                for (int i = 0; i<loops; ++i)
                {
                    var last = wordToPhrases.First().Value;
                    foreach (var entry in wordToPhrases)
                    {
                        var intersection = new List<int>();
var difference = new List<int>();
last.SplitSorted(entry.Value, intersection, difference);
                        count += difference.Count();
                        last = entry.Value;
                    }
                }
                System.GC.Collect();
                Console.WriteLine($"Average int enumerable split 1 pass {count / (wordToPhrases.Count() * loops)} in {timer.Elapsed.TotalMilliseconds / wordToPhrases.Count()}ms");
            }

            {
                var timer = Stopwatch.StartNew();
var count = 0.0;
                for (int i = 0; i<loops; ++i)
                {
                    var last = wordToPhrases.First().Value;
                    foreach (var entry in wordToPhrases)
                    {
                        var intersection = last.IntersectSorted(entry.Value);
var difference = last.DifferenceSorted(intersection);
count += difference.Count();
                        last = entry.Value;
                    }
                }
                System.GC.Collect();
                Console.WriteLine($"Average int enumerable split 2 pass {count / (wordToPhrases.Count() * loops)} in {timer.Elapsed.TotalMilliseconds / wordToPhrases.Count()}ms");
            }

            // Histogram(phrases);
            // TestAhoCarasick(phrases);
        static void TestAhoCarasick(string[] phrases)
{
    var timer = Stopwatch.StartNew();
    var searcher = new StringSearch();
    searcher.Keywords = phrases;
    Console.WriteLine($"Building lookup took {timer.Elapsed.TotalSeconds}s");

    string text;
    var rand = new Random();
    for (int i = 0; i < 10; ++i)
    {
        text = "the " + phrases[rand.Next(phrases.Length)];
        timer.Restart();
        var results = searcher.FindAll(text);
        Console.WriteLine($"Searching {text} found {results.Count()} results in {timer.Elapsed.TotalSeconds}s");
        foreach (var result in results)
        {
            Console.WriteLine($"  {result.Keyword}");
        }
    }

    while ((text = Console.ReadLine()) != "q")
    {
        timer.Restart();
        var results = searcher.FindAll(text);
        Console.WriteLine($"Searching {text} found {results.Count()} results in {timer.Elapsed.TotalSeconds}s");
        foreach (var result in results)
        {
            Console.WriteLine($"  {result.Keyword}");
        }
    }
}

static void Histogram(string[] phrases)
{
    var histogram = new Dictionary<string, int>();
    foreach (var phrase in phrases)
    {
        var words = phrase.Split(' ');
        foreach (var word in words.Distinct())
        {
            int count;
            if (histogram.TryGetValue(word, out count))
            {
                histogram[word] = ++count;
            }
            else
            {
                histogram[word] = 1;
            }
        }
    }
    int max = 0;
    double sum = 0;
    foreach (var entry in histogram)
    {
        sum += entry.Value;
        if (entry.Value > max)
        {
            max = entry.Value;
        }
    }
    Console.WriteLine($"Max {max}, Avg {sum / histogram.Count()}");
    foreach (var entry in (from record in histogram orderby record.Value descending select record).Take(20))
    {
        Console.WriteLine($"{entry.Key}: {entry.Value}");
    }
}



