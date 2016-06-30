using System.Collections.Generic;

namespace EntityMatch
{
    public class BaseAlternatives : IAlternatives
    {
        public IEnumerable<Alternative> Alternatives(string token)
        {
            yield return new Alternative(token, 1.0);
        }
    }

    public class SynonymAlternatives : IAlternatives
    {
        private Dictionary<string, Alternative[]> _synonyms = new Dictionary<string, Alternative[]>();
        private IAlternatives _alternatives;

        public SynonymAlternatives(IAlternatives alternatives)
        {
            _alternatives = alternatives;
        }

        public void AddAlternatives(string word, params Alternative[] alternatives)
        {
            _synonyms[word] = alternatives;
        }

        public IEnumerable<Alternative> Alternatives(string token)
        {
            foreach (var alternative in _alternatives.Alternatives(token))
            {
                Alternative[] synonyms;
                if (!_synonyms.TryGetValue(alternative.Word, out synonyms))
                {
                    yield return alternative;
                }
                else
                {
                    foreach (var synonym in synonyms)
                    {
                        yield return new Alternative(synonym.Word, synonym.Weight * alternative.Weight);
                    }
                }
            }
        }
    }
}