// To show top for each span
// #define DEBUGRECOGNIZE
using Microsoft.VisualStudio.Profiler;
using System;
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
            DataCollection.CommentMarkProfile(1, "Recognize");
            var tokenEntities = TokenEntities(tokens);
            var longestPerToken = LongestMatches(tokens, tokenEntities, 0.25);
            for (var i = 0; i < longestPerToken.Count(); ++i)
            {
                var byTypebyStart = (from span in longestPerToken[i]
                                     group span by span.Entity.Type into type
                                     select new
                                     {
                                         Type = type.Key,
                                         Start = (from span2 in type group span2 by span2.Start into byStart select byStart)
                                     });
                foreach (var type in byTypebyStart)
                {
                    foreach (var start in type.Start)
                    {
                        var max = 0.0;
                        Span maxSpan = null;
                        foreach (var span in start)
                        {
                            if (span.Score > max)
                            {
                                max = span.Score;
                                maxSpan = span;
                            }
                        }
#if DEBUGRECOGNIZE
                        Debug.WriteLine($"{type.Type} {start.Key}-{i} {start.Count()}");
                        foreach (var span in (from s in start orderby s.Score descending select s))
                        {
                            Debug.WriteLine($"{span.Score:f3} {span.Entity.Phrase}");
                        }
#endif
                        yield return maxSpan;
                    }
                }
            }
        }

        // Scoring
        // %word = # matched / total phrase
        // word match = sum of 1.0 or 0.9 on if original word matched phrase
        // word adjacenct = keep adding X to previous as long as adjacent.  If X is one, then max possible is 1/2*n(n + 1).
        // word rarity is a constant for a given span.  1/#phrases per word.
        private void AddSpan(SimpleSpan span, int end, IEnumerable<IEnumerable<Alternative>> tokens, double threshold, ICollection<Span> spans)
        {
            var length = end - span.Start + 1;
            var entity = _entities[span.Entity];
            var score = span.Weight / entity.TotalTokenWeight;
            if (score >= threshold)
            {
                spans.Add(new EntityMatch.Span(span.Start, length, entity, score));
            }
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

        private struct SimpleSpan
        {
            // Start position in input
            public int Start;

            // Start position in entity
            public int EntityStart;

            // Offset for entity
            public int Entity;

            // Sum of word weights
            public float Weight;
        }

        private IEnumerable<IEnumerable<WeightedEntityPosition>> TokenEntities(IEnumerable<IEnumerable<Alternative>> tokens)
        {
            var totalEntities = _entities.Entities.Count();
            foreach (var alternatives in tokens)
            {
                var alternativeCount = alternatives.Count();
                if (alternativeCount > 1)
                {
                    IEnumerable<WeightedEntityPosition> tokenEntities = new List<WeightedEntityPosition>();
                    foreach (var alternative in alternatives)
                    {
                        var altEntities = _entities.TokenEntities(alternative.Token);
                        if (altEntities.Any())
                        {
                            // Weight of expansion times IDF for word
                            tokenEntities = UnionSorted(tokenEntities, altEntities, alternative.Weight * _entities.TokenWeight(alternative.Token));
                        }
                    }
                    yield return tokenEntities;
                }
                else if (alternativeCount == 1)
                {
                    var alternative = alternatives.First();
                    var weight = alternative.Weight * _entities.TokenWeight(alternative.Token);
                    yield return (from entity in _entities.TokenEntities(alternative.Token)
                                  select new WeightedEntityPosition { Entity = entity.Entity, Position = entity.Position, Weight = (float)weight });
                }
            }
        }

        private struct WeightedEntityPosition
        {
            public int Entity;
            public int Position;
            public float Weight;
        }

        private IEnumerable<WeightedEntityPosition> UnionSorted(IEnumerable<WeightedEntityPosition> sequence1, IEnumerable<EntityPosition> sequence2, double weight)
        {
            using (var cursor1 = sequence1.GetEnumerator())
            using (var cursor2 = sequence2.GetEnumerator())
            {
                var continue1 = cursor1.MoveNext();
                var continue2 = cursor2.MoveNext();
                var value1 = continue1 ? cursor1.Current : default(WeightedEntityPosition);
                var value2 = continue2 ? cursor2.Current : default(EntityPosition);
                while (continue1 && continue2)
                {
                    if (value1.Entity < value2.Entity)
                    {
                        yield return value1;
                        continue1 = cursor1.MoveNext();
                        if (continue1) value1 = cursor1.Current;
                    }
                    else if (value1.Entity > value2.Entity)
                    {
                        yield return new WeightedEntityPosition { Entity = value2.Entity, Position = value2.Position, Weight = (float)weight };
                        continue2 = cursor2.MoveNext();
                        if (continue2) value2 = cursor2.Current;
                    }
                    else
                    {
                        do
                        {
                            if (value1.Position < value2.Position)
                            {
                                yield return value1;
                                continue1 = cursor1.MoveNext();
                                if (continue1) value1 = cursor1.Current;
                            }
                            else
                            {
                                Debug.Assert(value1.Position > value2.Position);
                                yield return new WeightedEntityPosition { Entity = value2.Entity, Position = value2.Position, Weight = weight > value1.Weight ? (float)weight : value1.Weight };
                                continue2 = cursor2.MoveNext();
                                if (continue2) value2 = cursor2.Current;
                            }
                        } while (continue1 && continue2 && value1.Entity == value2.Entity);
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
                        var current = cursor2.Current;
                        yield return new WeightedEntityPosition { Entity = current.Entity, Position = current.Position, Weight = (float)weight };
                    } while (cursor2.MoveNext());
                }
            }
        }

        // At each word position return the longest matches for each entity
        private IReadOnlyCollection<Span>[] LongestMatches(IEnumerable<IEnumerable<Alternative>> tokens, IEnumerable<IEnumerable<WeightedEntityPosition>> tokenEntities, double threshold)
        {
            var tokenEntitiesList = tokenEntities.ToList();
            var done = new List<Span>[tokenEntitiesList.Count()];
            IEnumerable<SimpleSpan> currentSpans = new List<SimpleSpan>();
            for (var i = 0; i < tokenEntitiesList.Count(); ++i)
            {
                done[i] = new List<Span>();
                var token = tokenEntitiesList[i];
                if (token.Any())
                {
                    currentSpans = ExtendSpans(tokens, currentSpans, token, i, 0.25, i > 0 ? done[i - 1] : null);
                }
                else
                {
                    if (i > 0)
                    {
                        foreach (var span in currentSpans)
                        {
                            AddSpan(span, i - 1, tokens, threshold, done[i - 1]);
                        }
                        currentSpans = new List<SimpleSpan>();
                    }
                }
            }
            foreach (var span in currentSpans)
            {
                AddSpan(span, tokenEntitiesList.Count() - 1, tokens, threshold, done[tokenEntitiesList.Count() - 1]);
            }
            return done;
        }

        private IEnumerable<SimpleSpan> ExtendSpans(IEnumerable<IEnumerable<Alternative>> tokens, IEnumerable<SimpleSpan> spans, IEnumerable<WeightedEntityPosition> entities, int start, double threshold, ICollection<Span> done)
        {
            var extensions = new List<SimpleSpan>();
            using (var spanCursor = spans.GetEnumerator())
            using (var entityCursor = entities.GetEnumerator())
            {
                var spanContinue = spanCursor.MoveNext();
                var entityContinue = entityCursor.MoveNext();
                while (spanContinue && entityContinue)
                {
                    var span = spanCursor.Current;
                    var entity = entityCursor.Current;
                    if (span.Entity < entity.Entity)
                    {
                        // Span is finished
                        AddSpan(span, start - 1, tokens, threshold, done);
                        spanContinue = spanCursor.MoveNext();
                    }
                    else if (span.Entity > entity.Entity)
                    {
                        // New span
                        extensions.Add(new SimpleSpan { Start = start, EntityStart = entity.Position, Entity = entity.Entity, Weight = entity.Weight });
                        entityContinue = entityCursor.MoveNext();
                    }
                    else
                    {
                        var nextPosition = (start - span.Start) + span.EntityStart;
                        if (nextPosition == entity.Position)
                        {
                            // Extend span because words are adjacent
                            extensions.Add(new SimpleSpan
                            {
                                Start = span.Start,
                                EntityStart = span.EntityStart,
                                Entity = span.Entity,
                                Weight = span.Weight + entity.Weight
                            });
                            spanContinue = spanCursor.MoveNext();
                            entityContinue = entityCursor.MoveNext();
                        }
                        else if (nextPosition > entity.Position)
                        {
                            // Move to next position
                            entityContinue = entityCursor.MoveNext();
                        }
                        else
                        {
                            // Span is finished
                            AddSpan(span, start - 1, tokens, threshold, done);
                            spanContinue = spanCursor.MoveNext();
                        }
                    }
                }
                if (spanContinue)
                {
                    // Remaining are done
                    do
                    {
                        AddSpan(spanCursor.Current, start - 1, tokens, threshold, done);
                    } while (spanCursor.MoveNext());
                }
                else if (entityContinue)
                {
                    // Remaining are new
                    do
                    {
                        extensions.Add(new SimpleSpan { Start = start, EntityStart = entityCursor.Current.Position, Entity = entityCursor.Current.Entity, Weight = entityCursor.Current.Weight });
                    } while (entityCursor.MoveNext());
                }
            }
            return extensions;
        }
    }
}
