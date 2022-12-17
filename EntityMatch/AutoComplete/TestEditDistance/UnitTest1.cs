using EditTrie;

namespace TestEditDistance
{
	[TestClass]
	public class UnitTest1
	{
		[TestMethod]
		public void TestTrie()
		{
			var trie = new Trie(10);
			trie.BeginUpdate();
			foreach (var phrase in new string[] { "match", "matcher", "watch", "watcher", "hatch"})
			{
				trie.Add(phrase);
			}
			trie.EndUpdate();
			Assert.AreEqual(1, trie.Lookup("matcher", 0));
			Assert.AreEqual(5, trie.Lookup("atch", 3));
			var matches = trie.EditLookup("matcher", 1).ToArray();
			Assert.AreEqual(2, matches.Length);
			Assert.AreEqual(0, matches[0].Distance);
			Assert.AreEqual(1, matches[1].Distance);
			Assert.AreEqual("matcher", matches[0].Token);
			Assert.AreEqual("watcher", matches[1].Token);
		}
	}
}