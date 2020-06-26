using System;
using System.Collections.Generic;
using EditTrie;
#if Dataintegration
using Microsoft.DataIntegration.Common;
using Microsoft.DataIntegration.FuzzyMatching;
#endif

namespace EntityMatch
{
    public class BaseAlternatives : IAlternatives
    {
        public void Add(params Token[] tokens)
        {
        }

        public IEnumerable<Alternative> Alternatives(Token token)
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

        public void Add(params Token[] tokens)
        {
            _alternatives.Add(tokens);
        }

        public void AddAlternatives(string token, params Alternative[] alternatives)
        {
            _synonyms[token] = alternatives;
        }

        public IEnumerable<Alternative> Alternatives(Token token)
        {
            foreach (var alternative in _alternatives.Alternatives(token))
            {
                Alternative[] synonyms;
                if (!_synonyms.TryGetValue(alternative.Token.TokenString, out synonyms))
                {
                    yield return alternative;
                }
                else
                {
                    foreach (var synonym in synonyms)
                    {
                        yield return new Alternative(synonym.Token, synonym.Weight * alternative.Weight);
                    }
                }
            }
        }
    }

    public class SpellingAlternatives : IAlternatives
    {
        private IAlternatives _alternatives;
        private Trie _trie = new Trie(5000);
        private bool _open = false;

        public SpellingAlternatives(IAlternatives alternatives)
        {
            _alternatives = alternatives;
        }

        public void Add(params Token[] tokens)
        {
            if (!_open)
            {
                _trie.BeginUpdate();
                _open = true;
            }
            foreach (var token in tokens)
            {
                _alternatives.Add(token);
                _trie.Add(token.TokenString);
            }
        }

        public IEnumerable<Alternative> Alternatives(Token token)
        {
            if (_open)
            {
                _trie.EndUpdate();
                _open = false;
            }
            var alternatives = _alternatives.Alternatives(token);
            foreach (var alternative in alternatives)
            {
                var matches = _trie.EditLookup(alternative.Token.TokenString, 1);
                foreach (var match in matches)
                {
                    if (match.Distance == 0)
                    {
                        yield return new Alternative(
                            new Token(match.Token, alternative.Token.TokenStart, alternative.Token.TokenLength), 
                            1.0);
                        yield break;
                    }
                    else
                    {
                        yield return new Alternative(
                            new Token(match.Token, alternative.Token.TokenStart, alternative.Token.TokenLength),
                            1.0 / (1.0 + match.Distance));
                    }
                }
            }
        }
    }

#if DataIntegration
    // This almost works, but I can't figure out how what the TransformationMatch records mean...
    // To use, you need to add in Microsoft.Dataintegration.common and FuzzyMatching dlls as references.  
    public class FuzzyAlternatives : IAlternatives
    {
        private IAlternatives _alternatives;
        private EditTransformationProvider _provider = new EditTransformationProvider();

        public FuzzyAlternatives(IAlternatives alternatives)
        {
            _alternatives = alternatives;
        }

        public void Add(params string[] tokens)
        {
            // _provider.Initialize();
            var dm = new DomainManager();
            dm.CreateDomain("test");
            _provider.Initialize(dm, "test");
            var session = _provider.CreateSession();
            var itp = dm.TokenIdProvider;
            var word = itp.GetOrCreateTokenId(new StringExtent("abc"), 0);
            _provider.Add(null, "abc");
            _provider.Add(null, "ac");
            _provider.Add(null, "abcd");
            _provider.Add(null, "qrst");
            var matches = new ArraySegment<TransformationMatch>();
            _provider.Match(session, itp, new TokenSequence(word), out matches);
            /*
            foreach (var token in tokens)
            {
                _alternatives.Add(token);
                _trie.Add(token);
            }
            */
        }

        public IEnumerable<Alternative> Alternatives(string token)
        {
            /*
            if (_open)
            {
                _trie.EndUpdate();
                _open = false;
            }
            var alternatives = _alternatives.Alternatives(token);
            foreach (var alternative in alternatives)
            {
                var n = _trie.EditLookup(alternative.Token, 1);
                /*
                _trie.BeginAC(2);
                foreach(var ch in alternative.Token)
                {
                    var matches = new List<string>();
                    _trie.AppendChar(ch, matches, 10);
                }
                yield return alternative;
            }
                */
            yield return new Alternative(token, 1.0);
        }
    }
#endif
}