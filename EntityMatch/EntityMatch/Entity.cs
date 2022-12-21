using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityMatch
{
    /// <summary>
    /// everything you need to know about an entity: 
    /// - its type, e.g. "DVD"
    /// - the full phrase it covers (a string)
    /// - the tokens it covers (an array of Tokens)
    /// - its weight
    /// </summary>
    public class Entity
    {
        public readonly string Type;
        public readonly string Phrase;
        public readonly Token[] Tokens;
        public double TotalTokenWeight;

        public Entity(string type, string phrase)
        {
            Type = type;
            Phrase = phrase;
            Tokens = Language.Normalize(phrase).ToArray();
        }

        public override string ToString()
        {
            return $"{Type}({Phrase})";
        }
    }
}
