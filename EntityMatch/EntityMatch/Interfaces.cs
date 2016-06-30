using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityMatch
{
    public class Alternative
    {
        public readonly string Word;
        public readonly double Weight;

        public Alternative(string word, double weight)
        {
            Word = word;
            Weight = weight;
        }
    }

    public interface ITokenizer
    {
        IEnumerable<string> Tokenize(string input);
    }

    public interface IAlternatives
    {
        IEnumerable<Alternative> Alternatives(string word);
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
