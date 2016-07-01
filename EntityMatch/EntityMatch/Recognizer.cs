// To show top 10 for each span: #define DEBUGRECOGNIZE
// To show every score: #define DEBUGSCORE
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
                var byTypebyStart = (from span in longestPerToken[i]
                                     group span by _entities[span.Entity].Type into type
                                     select new
                                     {
                                         Type = type.Key,
                                         Start = (from span2 in type group span2 by span2.Start into byStart select byStart)
                                     });
                foreach (var type in byTypebyStart)
                {
                    foreach (var start in type.Start)
                    {
                        var spans = (from span in start select ToSpan(span, i, tokens));
                        var max = 0.0;
                        Span maxSpan = null;
                        foreach (var span in spans)
                        {
                            if (span.Score > max)
                            {
                                max = span.Score;
                                maxSpan = span;
                            }
                        }
#if DEBUGRECOGNIZE
                        Debug.WriteLine($"{type.Type} {start.Key}");
                        foreach(var span in (from s in spans orderby s.Score descending select s).Take(10))
                        {
                            Debug.WriteLine($"{span.Score:f3} {span.Entity.Phrase}");
                        }
#endif
                        yield return maxSpan;
                    }
                }
            }
        }

        private Span ToSpan(SimpleSpan span, int end, IEnumerable<IEnumerable<Alternative>> tokens)
        {
            var length = end - span.Start + 1;
            var entity = _entities[span.Entity];
            var score = Score(tokens.Skip(span.Start).Take(length), entity.Tokens);
            return new EntityMatch.Span(span.Start, length, entity, score);
        }

        private struct Position
        {
            public Position(int phrasePos, double weight)
            {
                PhrasePos = phrasePos;
                Weight = weight;
            }
            public int PhrasePos { get; private set; }
            public double Weight { get; private set; }
        }

#if DEBUGSCORE
        private List<Tuple<Position[], double>> _choiceScores = new List<Tuple<Position[], double>>();
#endif

        private double Score(IEnumerable<IEnumerable<Alternative>> tokens, string[] entity)
        {
#if DEBUGSCORE
            _choiceScores.Clear();
#endif
            // Collect position+weight per token position
            var positions = new List<List<Position>>();
            foreach (var inputAlternatives in tokens)
            {
                var perPosition = new List<Position>();
                foreach (var inputAlternative in inputAlternatives)
                {
                    for (var i = 0; i < entity.Length; ++i)
                    {
                        if (entity[i] == inputAlternative.Token)
                        {
                            perPosition.Add(new Position(i, inputAlternative.Weight));
                        }
                    }
                }
                positions.Add(perPosition);
            }
            var score = Score(positions, new Stack<Position>(), entity);
#if DEBUGSCORE
            Debug.Write("***");
            foreach (var token in entity)
            {
                Debug.Write($" {token}");
            }
            Debug.WriteLine("");
            foreach (var choiceScore in (from choice in _choiceScores orderby choice.Item2 descending select choice).Take(20)) 
            {
                Debug.Write($"{choiceScore.Item2:F3} ");
                foreach(var choice in choiceScore.Item1)
                {
                    Debug.Write($"{entity[choice.PhrasePos]}_{choice.PhrasePos}({choice.Weight}) ");
                }
                Debug.WriteLine("");
            }
#endif
            return score;
        }

        private double Score(List<List<Position>> positions, Stack<Position> choices, string[] entity)
        {
            double score = 0.0;
            int current = choices.Count();
            int spanLength = positions.Count();
            if (positions.Count() == current)
            {
                score = Score(choices, spanLength, entity);
            }
            else
            {
                foreach (var position in positions[current])
                {
                    double childScore;
                    if (!choices.Any((c) => c.PhrasePos == position.PhrasePos))
                    {
                        choices.Push(position);
                        childScore = Score(positions, choices, entity);
                        choices.Pop();
                    }
                    else
                    {
                        childScore = Score(choices, spanLength, entity);
                    }
                    if (childScore > score)
                    {
                        score = childScore;
                    }
                }
            }
            return score;
        }

        // Scoring
        // %word = # matched / total phrase
        // word match = sum of 1.0 or 0.9 on if original word matched phrase
        // word adjacenct = keep adding X to previous as long as adjacent.  If X is one, then max possible is 1/2*n(n + 1).
        // word rarity is a constant for a given span.  1/#phrases per word.
        private double Score(Stack<Position> choices, int spanLength, string[] entity)
        {
            double wordScore = choices.Sum((c) => c.Weight) / spanLength;
            int adjacentCount = 0;
            int start = choices.First().PhrasePos;
            int lastPosition = start - 1;
            foreach (var position in choices)
            {
                if (position.PhrasePos == lastPosition + 1)
                {
                    // Sequence order and entity order match
                    adjacentCount += position.PhrasePos - start + 1;
                }
                else
                {
                    // Restart adjacency
                    start = position.PhrasePos;
                }
                lastPosition = position.PhrasePos;
            }
            double adjacencyScore = adjacentCount / (0.5d * (spanLength * (spanLength + 1)));
            var percentMatched = (double)choices.Count() / entity.Length;
            var score = percentMatched * wordScore * adjacencyScore;
#if DEBUGSCORE
            _choiceScores.Add(Tuple.Create(choices.ToArray(), score));
#endif
            return score;
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
