using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace EntityMatch
{
    public class SimpleTokenizer : ITokenizer
    {

        public IEnumerable<Token> Tokenize(string input)
        {
            return Language.Normalize(input);
        }
    }

    public class Language
    {
        static Regex _tokenRegex = new Regex(@"(\w)+", RegexOptions.Compiled);
        public static IEnumerable<Token> WordBreak(string phrase)
        {
            var tokenInfos = _tokenRegex.Matches(phrase).Cast<Match>().Select(m => new Token(m.Value, m.Index, m.Length));
            return tokenInfos;
        }

        static Regex _parens = new Regex(@"\([^)]*\)", RegexOptions.Compiled);
        //static Regex _punc = new Regex(@"\p{P}\b|\b\p{P}", RegexOptions.Compiled);

        public static IEnumerable<Token> Normalize(string phrase)
        {
            // not removing punctuation: that will be taken care of by the regex in WordBreak
            //var nPhrase = _punc.Replace(_parens.Replace(phrase.Trim().ToLower(), ""), "");
            var nPhrase = _parens.Replace(phrase.Trim().ToLower(), "");

            return WordBreak(nPhrase);
        }
    }
}
