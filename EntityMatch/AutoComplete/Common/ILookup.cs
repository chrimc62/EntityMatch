using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common
{
    public interface ITrieMatch
    {
        string Token { get; }
        int Distance { get; }
    }

    public interface ILookupIndex
    {
        int Lookup(string s, int maxEdit);
        IEnumerable<ITrieMatch> EditLookup(string s, int maxEdit, int maxMatches);        
    }

    public interface IAdd
    {
        void BeginUpdate();
        void Add(string s);
        void EndUpdate();
    }
}
