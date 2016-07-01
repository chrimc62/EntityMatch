using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityMatch
{
    public class Recognizer : IEntityRecognizer
    {
        private IEntities _entities;

        public Recognizer(IEntities entities)
        {
            _entities = entities;
        }

        public IEnumerable<Span> Recognize(IEnumerable<IEnumerable<Alternative>> tokens)
        {
            var tokenEntities = TokenEntities(tokens);
            var longestPerToken = LongestMatches(tokenEntities);
            for (var i = 0; i < longestPerToken.Count(); ++i)
            {
                var byTypebyLength = (from span in longestPerToken[i]
                                      group span by _entities[span.Entity].Type
                                    into type
                                      select new
                                      {
                                          Type = type.Key,
                                          values = (from span2 in type group span2 by span2.Start into byStart select byStart)
                                      });
                foreach (var type in byTypebyLength)
                {
                    foreach (var start in type.values)
                    {
                        var span = start.First();
                        yield return new Span(span.Start, i - span.Start + 1, _entities[span.Entity], 1.0);
                    }
                }
            }
        }

        private struct SimpleSpan
        {
            public int Start;
            public int Entity;
        }

        private IEnumerable<IEnumerable<int>> TokenEntities(IEnumerable<IEnumerable<Alternative>> tokens)
        {
            foreach (var alternatives in tokens)
            {
                IEnumerable<int> tokenEntities = new List<int>();
                foreach (var alternative in alternatives)
                {
                    var altEntities = _entities.TokenEntities(alternative.Token);
                    if (altEntities.Any())
                    {
                        tokenEntities = UnionSorted(tokenEntities, altEntities);
                    }
                }
                yield return tokenEntities;
            }
        }

        private IEnumerable<int> UnionSorted(IEnumerable<int> sequence1, IEnumerable<int> sequence2)
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
                if (continue1)
                {
                    do
                    {
                        yield return cursor1.Current;
                    } while (cursor1.MoveNext());
                }
                else if (continue2)
                {
                    do
                    {
                        yield return cursor2.Current;
                    } while (cursor2.MoveNext());
                }
            }
        }

        // At each word position return the longest matches for each entity
        private IReadOnlyCollection<SimpleSpan>[] LongestMatches(IEnumerable<IEnumerable<int>> tokenEntities)
        {
            var tokens = tokenEntities.ToList();
            var done = new List<SimpleSpan>[tokens.Count()];
            IEnumerable<SimpleSpan> currentSpans = new List<SimpleSpan>();
            for (var i = 0; i < tokens.Count(); ++i)
            {
                done[i] = new List<SimpleSpan>();
                var token = tokens[i];
                if (token.Any())
                {
                    currentSpans = ExtendSpans(currentSpans, token, i, i > 0 ? done[i - 1] : null);
                }
                else
                {
                    if (i > 0)
                    {
                        done[i - 1].AddRange(currentSpans);
                    }
                    currentSpans = new List<SimpleSpan>();
                }
            }
            done[tokens.Count() - 1].AddRange(currentSpans);
            return done;
        }

        private IEnumerable<SimpleSpan> ExtendSpans(IEnumerable<SimpleSpan> spans, IEnumerable<int> phrases, int start, ICollection<SimpleSpan> done)
        {
            var extensions = new List<SimpleSpan>();
            using (var spanCursor = spans.GetEnumerator())
            using (var entityCursor = phrases.GetEnumerator())
            {
                var spanContinue = spanCursor.MoveNext();
                var entityContinue = entityCursor.MoveNext();
                while (spanContinue && entityContinue)
                {
                    var span = spanCursor.Current.Entity;
                    var entity = entityCursor.Current;
                    if (span < entity)
                    {
                        // Span is finished
                        done.Add(spanCursor.Current);
                        spanContinue = spanCursor.MoveNext();
                    }
                    else if (span > entity)
                    {
                        // New span
                        extensions.Add(new SimpleSpan { Start = start, Entity = entity });
                        entityContinue = entityCursor.MoveNext();
                    }
                    else
                    {
                        // Extend span
                        extensions.Add(spanCursor.Current);
                        spanContinue = spanCursor.MoveNext();
                        entityContinue = entityCursor.MoveNext();
                    }
                }
                if (spanContinue)
                {
                    // Remaining are done
                    do
                    {
                        done.Add(spanCursor.Current);
                    } while (spanCursor.MoveNext());
                }
                else if (entityContinue)
                {
                    // Remaining are new
                    do
                    {
                        extensions.Add(new SimpleSpan { Start = start, Entity = entityCursor.Current });
                    } while (entityCursor.MoveNext());
                }
            }
            return extensions;
        }
    }
}
