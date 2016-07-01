using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityMatch
{
    public class Alternative
    {
        public readonly string Token;
        public readonly double Weight;

        public Alternative(string token, double weight)
        {
            Token = token;
            Weight = weight;
        }
    }

    public interface ITokenizer
    {
        IEnumerable<string> Tokenize(string input);
    }

    public interface IAlternatives
    {
        void Add(params string[] tokens);

        IEnumerable<Alternative> Alternatives(string token);
    }

    public interface IEntities
    {
        void AddEntities(params Entity[] entities);

        IEnumerable<Entity> Entities { get; }

        Entity this[int id] { get; }

        IEnumerable<int> TokenEntities(string token);
    }

    public interface IEntityRecognizer
    {
        IEnumerable<Span> Recognize(IEnumerable<IEnumerable<Alternative>> words);
    }

    public interface IMatcher
    {
        void AddEntities(string type, params string[] phrases);
        IEnumerable<Interpretation> Interpretations(string input);
    }
}
