using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityMatch
{
    public class Matcher : IMatcher
    {
        private ITokenizer _tokenizer;
        private IEntities _tokenToEntities;
        private IAlternatives _alternatives;
        private IEntityRecognizer _recognizer;

        public Matcher(
            ITokenizer tokenizer, 
            IEntities tokenToEntities,
            IAlternatives alternatives, 
            IEntityRecognizer recognizer)
        {
            _tokenizer = tokenizer;
            _tokenToEntities = tokenToEntities;
            _alternatives = alternatives;
            _recognizer = recognizer;
        }

        public void AddEntities(string type, params string[] phrases)
        {
            foreach (var phrase in phrases)
            {
                var entity = new Entity(type, phrase);
                _tokenToEntities.AddEntities(entity);
                _alternatives.Add(entity.Tokens);
            }
        }

        public void Compute()
        {
            _tokenToEntities.Compute();
        }

        public IEnumerable<Interpretation> Interpretations(string input, int spansPerPosition, double threshold)
        {
            var tokens = _tokenizer.Tokenize(input);
            var alternatives = (from token in tokens select _alternatives.Alternatives(token));
            var spans = _recognizer.Recognize(alternatives, spansPerPosition, threshold);
            var interpretation = new Interpretation(tokens, spans);
            yield return interpretation;
        }
    }
}
