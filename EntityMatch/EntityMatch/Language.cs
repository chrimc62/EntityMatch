using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace EntityMatch
{
    public class SimpleTokenizer : ITokenizer
    {
        public IEnumerable<string> Tokenize(string input)
        {
            return Language.Normalize(input);
        }
    }

    public class Language
    {
        public static IEnumerable<string> WordBreak(string phrase)
        {
            // For now just break on space
            return (from word in phrase.Split(' ') where word.Length > 0 select word);
        }

        static Regex _parens = new Regex(@"\([^)]*\)", RegexOptions.Compiled);
        static Regex _punc = new Regex(@"\p{P}\b|\b\p{P}", RegexOptions.Compiled);

        public static IEnumerable<string> Normalize(string phrase)
        {
            var nPhrase = _punc.Replace(_parens.Replace(phrase.Trim().ToLower(), ""), "");
            return WordBreak(nPhrase);
        }
    }
}
