using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityMatch
{
    public class Span
    {
        public readonly int Start;
        public int End { get { return Start + Length; } }
        public readonly int Length;
        public readonly Entity Entity;
        public readonly double Score;

        public Span(int start, int length, Entity entity, double score)
        {
            Start = start;
            Length = length;
            Entity = entity;
            Score = score;
        }
    }

    public class Interpretation
    {
        private List<string> _tokens;

        public IReadOnlyCollection<Span> Spans;

        public Interpretation(IEnumerable<string> tokens, IEnumerable<Span> spans)
        {
            _tokens = tokens.ToList();
            Spans = spans.ToList();
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            foreach (var span in Spans)
            {
                builder.Append("[\"");
                bool first = true;
                for(var i = span.Start; i < span.End; ++i)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        builder.Append(' ');
                    }
                    builder.Append(_tokens[i]);
                }
                builder.Append($"\", {span.Score}, {span.Entity}]\n");
            }
            return builder.ToString();
        }
    }
}
