using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityMatch
{
    public class Entity
    {
        public readonly string Type;
        public readonly string Phrase;
        public readonly string[] Tokens;
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
