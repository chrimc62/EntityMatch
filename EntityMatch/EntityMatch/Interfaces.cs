using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityMatch
{
    public struct Token
    {
        public Token(string tokenString, int tokenStart, int tokenLength)
        {
            TokenString = tokenString;
            TokenStart = tokenStart;
            TokenLength = tokenLength;
        }
        public readonly string TokenString;
        public readonly int TokenStart;
        public readonly int TokenLength;

        public override string ToString()
        {
            return $"{TokenString}({TokenStart}-{TokenStart + TokenLength - 1})";
        }
    }

    /// <summary>
    /// An alternative is a token with a weight
    /// </summary>
    public class Alternative
    {
        public readonly Token Token;
        public readonly double Weight;

        public Alternative(Token token, double weight)
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
        IEnumerable<Token> Tokenize(string input);
    }

    /// <summary>
    /// Used to stores alternative tokens
    /// </summary>
    public interface IAlternatives
    {
        void Add(params Token[] tokens);

        IEnumerable<Alternative> Alternatives(Token token);
    }

    /// <summary>
    /// An entityPosition contains:
    /// - entity: an id used to retrieve the Entity object from a list
    /// - position: start token position
    /// </summary>
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
        IEnumerable<Span> Recognize(IEnumerable<IEnumerable<Alternative>> words, int spansPerPosition, double threshold);
    }

    public interface IMatcher
    {
        void AddEntities(string type, params string[] phrases);
        void Compute();
        IEnumerable<Interpretation> Interpretations(string input, int spansPerPosition, double threshold);
    }
}
