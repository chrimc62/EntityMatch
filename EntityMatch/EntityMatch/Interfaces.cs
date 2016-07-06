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

        public override string ToString()
        {
            return $"{Token}({Weight})";
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

    public struct EntityPosition
    {
        public EntityPosition(int entity, int position)
        {
            Entity = entity;
            Position = position;
        }
        public readonly int Entity;
        public readonly int Position;

        public override string ToString()
        {
            return $"{Entity}({Position})";
        }
    }

    public interface IEntities
    {
        void AddEntities(params Entity[] entities);

        void Compute();

        IEnumerable<Entity> Entities { get; }

        Entity this[int id] { get; }

        IEnumerable<EntityPosition> TokenEntities(string token);

        double TokenWeight(string token);
    }

    public interface IEntityRecognizer
    {
        IEnumerable<Span> Recognize(IEnumerable<IEnumerable<Alternative>> words);
    }

    public interface IMatcher
    {
        void AddEntities(string type, params string[] phrases);
        void Compute();
        IEnumerable<Interpretation> Interpretations(string input);
    }
}
