using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using Common;

namespace EditTrie
{
    public class TrieMatch : ITrieMatch
    {
        internal TrieMatch(Trie trie, int entity, int distance)
        {
            _trie = trie;
            _entity = entity;
            Distance = distance;
        }

        private readonly Trie _trie;
        private readonly int _entity;
        public int Distance { get; private set; }
        public string Token { get { return _trie.LookupEntity(_entity); } }
    }

    /// <summary>
    /// Standard trie with two modifications: 1. Compression, and 2. Traversal is edit-tolerant.
    /// </summary>
    [Serializable]
    public class Trie : ILookupIndex, IAdd
    {
        #region Members
        const int infty = 10000;

        int m_numStates;
        int m_numIndexEntities;

        int m_topL;

        // State that exists only during build time
        List<List<int>>? m_outputFnUpdate;
        List<List<int>> m_descendantsUpdate;
        List<List<char>>? m_childTokensUpdate;
        IntPairHashTable? m_gotoFnUpdate;
        Dictionary<int, int>? m_oldId2NewId;
        Dictionary<int, string>? m_chains;
        List<string> m_EntitiesUpdate;
        Dictionary<string, bool> m_DupChecker;
        int m_nHiSources;
        int m_nHiEdges;

        // State that exists only during lookup time
        [Serializable()]
        struct OutputStruct
        {
            public int nodeid;
            public int entityid;
        }

        class OutputComparer : IComparer<OutputStruct>
        {
            #region IComparer<OutputStruct> Members

            public int Compare(OutputStruct x, OutputStruct y)
            {
                return (x.nodeid - y.nodeid);
            }

            #endregion
        }

        OutputStruct[]? m_outputFn;
        Dictionary<int, int[]> m_descendants;

        //short[] m_level;
        TrieChildState? m_childState;
        char[]? m_EntityChars;
        int[]? m_EntityOffsets;

        int m_CurrentLookupThreshold;
        int m_totalValidNodes;

        ActiveNodes m_AN1;
        ActiveNodes m_AN2;
        Queue<int> BFS_Q;

        string? m_prefix;

        List<int> m_stateIds;

        Dictionary<int, bool> m_visitActiveNodes;
        List<int> m_acEntityIds;
        #endregion

        #region Public Methods

        public Trie(int topl)
        {
            m_AN1 = new ActiveNodes();
            m_AN2 = new ActiveNodes();
            BFS_Q = new Queue<int>();
            m_EntitiesUpdate = new List<string>();
            m_stateIds = new List<int>();
            m_DupChecker = new Dictionary<string, bool>();
            m_visitActiveNodes = new Dictionary<int, bool>();
            m_acEntityIds = new List<int>();
            m_descendantsUpdate = new List<List<int>>();
            m_descendants = new Dictionary<int, int[]>();
            m_topL = topl;
        }

        #region update methods
        public void BeginUpdate()
        {
            m_numStates = 1;
            m_numIndexEntities = 0;
            m_gotoFnUpdate = new IntPairHashTable(1000000);

            m_outputFnUpdate = new List<List<int>>();
            m_childTokensUpdate = new List<List<char>>();

            m_childTokensUpdate.Add(new List<char>(0));
            m_outputFnUpdate.Add(new List<int>(0));
            m_descendantsUpdate.Add(new List<int>(0));
        }

        public void Add(string entity)
        {
            //entity = Normalize(entity);
            if (m_DupChecker.ContainsKey(entity))
                return;

            m_DupChecker.Add(entity, true);

            int curState = 0;

            string prefix = "";
            bool newState = true;
            for (int i = 0; i < entity.Length; i++)
            {
                curState = UGoto(curState, entity[i], out newState);
                prefix = prefix + entity[i];
                //m_outputFnUpdate[curState].Add(m_numIndexEntities);
                if (m_descendantsUpdate[curState].Count < m_topL)
                {
                    m_descendantsUpdate[curState].Add(m_numIndexEntities);
                }
            }
            m_outputFnUpdate![curState].Add(m_numIndexEntities++);
            //m_numIndexEntities++;
            m_EntitiesUpdate.Add(entity);
        }

        public void EndUpdate()
        {
            m_DupChecker.Clear();
            m_DupChecker = null!;
            MoveToLookupStructures();

            GC.Collect();
        }

        #endregion

        #region lookup methods

        public void ClearCounters()
        {
            m_totalValidNodes = 0;
        }

        public int Lookup(string s, int maxEdit)
        {
            return EditLookup(s, maxEdit).Count();
        }

        public IEnumerable<ITrieMatch> EditLookup(string s, int maxEdit, int maxMatches = int.MaxValue)
        {
            var matches = new List<TrieMatch>();
            if (s.Length > 0)
            {
                m_AN1.Clear();
                m_AN2.Clear();

                BeginAC(maxEdit);

                for (int i = 0; i < s.Length; i++)
                {
                    AppendChar(s[i]);
                }

                int curState;
                while ((curState = m_AN1.GetNext()) != -1)
                {
                    var entity = Output(curState);
                    if (entity != -1)
                    {
                        matches.Add(new TrieMatch(this, entity, m_AN1.Distance(curState)));
                    }
                }
                matches = (from match in matches
                           orderby match.Distance ascending
                           select match).Take(maxMatches).ToList();
                m_AN1.ClearDistances();
            }
            return (IEnumerable<ITrieMatch>) matches;
        }

        public string LookupEntity(int entity)
        {
            var builder = new StringBuilder();
            int begin = m_EntityOffsets![entity];
            int end = (entity < m_EntityOffsets.Length - 1) ? m_EntityOffsets[entity + 1] : m_EntityChars!.Length;
            for (int i = begin; i < end; i++)
            {
                builder.Append(m_EntityChars![i]);
            }
            return builder.ToString();
        }

        public void BeginAC(int k)
        {
            m_AN1.Clear();
            m_AN2.Clear();
            m_CurrentLookupThreshold = k;
            m_prefix = "";
            LevelVisit(m_CurrentLookupThreshold);
        }

        public void AppendChar(char c)
        {
            m_prefix += c;
            int prefixlen = m_prefix.Length;

            int currState;
            int idx = prefixlen - 1;
            while ((currState = m_AN1.GetNext()) != -1)
            {
                int dist = m_AN1.Distance(currState);
                if (dist < m_CurrentLookupThreshold)
                {
                    m_AN2.AddNode(currState, dist + 1);
                }

                int childState_inputChar = Goto(currState, c);
                if (childState_inputChar != -1 && childState_inputChar != 0)
                {
                    m_AN2.AddNode(childState_inputChar, dist);
                }

                int newdist = m_AN2.Distance(currState);
                int mindist = (dist < newdist) ? dist : newdist;
                if (mindist < m_CurrentLookupThreshold)
                {
                    m_childState!.BeginTarget(currState);

                    int childState;
                    while ((childState = m_childState!.GetNextTarget()) != -1)
                    {
                        if (childState != childState_inputChar)
                        {
                            m_AN2.AddNode(childState, mindist + 1);
                        }
                    }
                }
            }
            m_AN1.ClearDistances();
            Swap(ref m_AN1, ref m_AN2);
            m_totalValidNodes += m_AN1.Count;
#if DEBUG
            //Console.WriteLine("No. of active nodes after character {1}: {0}", AN1.Count, i + 1);
            //ActiveNodeAggregator.Add(i + 1, AN1.Count);
#endif

        }

        public void AppendChar(char c, List<string> retArray, int l)
        {
            AppendChar(c);
            FindEntityList(retArray, l);
        }

        #endregion

        #region other methods

        public int NStates
        {
            get { return m_numStates; }
        }

        public int NVisits
        {
            get { return m_totalValidNodes; }
        }

        #endregion

        #endregion

        #region Private Methods

        private int FindEntityList(List<string> retArray, int l)
        {
            retArray.Clear();
            m_acEntityIds.Clear();
            m_visitActiveNodes.Clear();
            m_stateIds.Clear();
            if (m_AN1.Count > 0)
                m_AN1.DistanceSort(m_stateIds, l, m_descendants);

            int nOutput = 0;
            foreach (int curState in m_stateIds)
            {
                VisitSubtree(curState, m_acEntityIds, l, m_visitActiveNodes);
                if (m_acEntityIds.Count >= l)
                    break;
            }

            foreach (int entityId in m_acEntityIds)
            {
                //retArray.Add(m_Entities[entityId]);
                string s = "";
                int begin = m_EntityOffsets![entityId];
                int end = (entityId < m_EntityOffsets.Length - 1) ? m_EntityOffsets[entityId + 1] : m_EntityChars!.Length;
                for (int i = begin; i < end; i++)
                {
                    s += m_EntityChars![i];
                }
                retArray.Add(s);
                nOutput++;
            }

            return nOutput;
        }

        private void Swap(ref ActiveNodes an1, ref ActiveNodes an2)
        {
            ActiveNodes tmp = an1;
            an1 = an2;
            an2 = tmp;
        }

        private int Goto(int state, char token)
        {
            int nextState = -1;
            nextState = m_childState!.FindTarget(state, token);
            return nextState;
        }

        private int UGoto(int state, char token, out bool newState)
        {
            int nextState = 0;
            newState = false;
            if (!m_gotoFnUpdate!.TryGetValue(state, token, out nextState))
            {
                nextState = m_numStates++;
                newState = true;
                m_outputFnUpdate!.Add(new List<int>(0));
                m_descendantsUpdate!.Add(new List<int>(0));
                m_childTokensUpdate!.Add(new List<char>(0));

                m_childTokensUpdate[state].Add(token);
                m_gotoFnUpdate.Add(state, token, nextState);
            }

            return nextState;
        }

        private void MoveToLookupStructures()
        {
            MarkChains();
            RenameNodeIds();

            int nChains = m_nHiEdges - m_nHiSources + 1;

            for (int i = 0; i < m_numStates; i++)
            {
                if (m_descendantsUpdate[i].Count >= m_topL)
                {
                    int newid = m_oldId2NewId![i];
                    int[] descendants = m_descendantsUpdate[i].ToArray();
                    m_descendants.Add(newid, descendants);
                }
            }
            m_descendantsUpdate.Clear();
            m_descendantsUpdate = null!;

            int nOutput = 0;
            for (int i = 0; i < m_numStates; i++)
            {
                if (m_outputFnUpdate![i].Count > 0)
                    nOutput++;
            }
            m_outputFn = new OutputStruct[nOutput];

            int totalChars = 0;
            foreach (string e in m_EntitiesUpdate)
            {
                totalChars += e.Length;
            }
            m_EntityChars = new char[totalChars];
            m_EntityOffsets = new int[m_EntitiesUpdate.Count];
            int currOffset = 0;
            int currEntityId = 0;
            foreach (string e in m_EntitiesUpdate)
            {
                m_EntityOffsets[currEntityId++] = currOffset;
                foreach (char c in e)
                {
                    m_EntityChars[currOffset++] = c;
                }
            }

            m_EntitiesUpdate.Clear();
            m_EntitiesUpdate = null!;

            int TotalEdges = 0;
            int idx = 0;
            for (int i = 0; i < m_numStates; i++)
            {
                if (m_outputFnUpdate![i].Count > 0)
                {
                    if (m_outputFnUpdate[i].Count > 1)
                    {
                        throw new Exception("Output size exceeded 1");
                    }
                    m_outputFn[idx].nodeid = m_oldId2NewId![i];
                    m_outputFn[idx].entityid = m_outputFnUpdate[i][0];
                    idx++;
                }
                m_outputFnUpdate[i].Clear();
                int nChildren = m_childTokensUpdate![i].Count;
                TotalEdges += nChildren;
            }

            OutputComparer comparer = new OutputComparer();
            Array.Sort(m_outputFn, comparer);

            m_outputFnUpdate!.Clear();
            m_outputFnUpdate = null;

            m_childState = new TrieChildState(m_numStates, m_nHiSources, TotalEdges, m_nHiEdges);

            Dictionary<int, int> newId2oldId = new Dictionary<int, int>();
            Dictionary<int, int>.Enumerator old2newEnum = m_oldId2NewId!.GetEnumerator();
            int maxNewId = 0;
            while (old2newEnum.MoveNext())
            {
                newId2oldId.Add(old2newEnum.Current.Value, old2newEnum.Current.Key);
                if (old2newEnum.Current.Value > maxNewId)
                    maxNewId = old2newEnum.Current.Value;
            }

            for (int i = 0; i < m_nHiSources; i++)
            {
                int oldid = newId2oldId[i];
                m_childState.BeginHiSource(i, m_childTokensUpdate![oldid].Count);
                m_childTokensUpdate[oldid].Sort();
                foreach (char c in m_childTokensUpdate[oldid])
                {
                    int tgt;
                    m_gotoFnUpdate!.TryGetValue(oldid, c, out tgt);
                    m_childState.AddHiTarget(i, c, m_oldId2NewId[tgt]);
                }
                m_childTokensUpdate[oldid].Clear();
            }

            m_childState.BeginLo();

            for (int i = m_nHiSources; i < maxNewId; i++)
            {
                int oldid = newId2oldId[i];
                if (m_chains!.ContainsKey(oldid))
                {
                    m_childState.AddLo(i, m_chains[oldid].ToCharArray());
                }
            }

            newId2oldId.Clear();
            newId2oldId = null!;

            m_childTokensUpdate!.Clear();
            m_childTokensUpdate = null;
            m_gotoFnUpdate!.Clear();
            m_gotoFnUpdate = null;
            m_oldId2NewId.Clear();
            m_oldId2NewId = null;
            m_chains!.Clear();
            m_chains = null;
        }

        private int Output(int nodeid)
        {
            int start = 0;
            int end = m_outputFn!.Length - 1;
            int idx;
            while (start <= end)
            {
                idx = (int)((start + end) / 2);
                if (m_outputFn[idx].nodeid < nodeid)
                {
                    start = idx + 1;
                }
                else if (m_outputFn[idx].nodeid > nodeid)
                {
                    end = idx - 1;
                }
                else // childChars[idx] == label
                {
                    return m_outputFn[idx].entityid;
                }
            }
            return -1;
        }

        private void MarkChains()
        {
            m_chains = new Dictionary<int, string>();
            MarkChains(0, m_chains);
        }

        private string MarkChains(int state, Dictionary<int, string> marker)
        {
            string s = "";
            bool bchain = true;
            if (m_childTokensUpdate![state].Count > 1)
            {
                bchain = false;
            }
            foreach (char c in m_childTokensUpdate[state])
            {
                int tgt;
                m_gotoFnUpdate!.TryGetValue(state, c, out tgt);
                string chain = MarkChains(tgt, marker);
                if (chain == null)
                {
                    bchain = false;
                }
                else if (bchain)
                {
                    s = c + chain;
                }
            }
            if (bchain)
            {
                marker.Add(state, s);
                return s;
            }
            return null!;
        }

        private void RenameNodeIds()
        {
            m_oldId2NewId = new Dictionary<int, int>();
            m_nHiEdges = 0;
            m_nHiSources = DFSHiNumbers(0, m_oldId2NewId, 0) + 1;
            DFSLoNumbers(0, m_oldId2NewId, m_nHiSources);
        }

        private int DFSHiNumbers(int state, Dictionary<int, int> newIds, int Id)
        {
            newIds.Add(state, Id);
            m_nHiEdges += m_childTokensUpdate![state].Count;
            int maxId = Id;
            foreach (char c in m_childTokensUpdate![state])
            {
                int tgt;
                m_gotoFnUpdate!.TryGetValue(state, c, out tgt);
                if (!m_chains!.ContainsKey(tgt))
                {
                    maxId = DFSHiNumbers(tgt, newIds, maxId + 1);
                }
            }
            return maxId;
        }

        private int DFSLoNumbers(int state, Dictionary<int, int> newIds, int Id)
        {
            if (m_chains!.ContainsKey(state))
            {
                DFSNumbers(state, newIds, Id);
                int chainLen = m_chains[state].Length;
                return Id + chainLen + 1;
            }

            int maxId = Id;
            foreach (char c in m_childTokensUpdate![state])
            {
                int tgt;
                m_gotoFnUpdate!.TryGetValue(state, c, out tgt);
                maxId = DFSLoNumbers(tgt, newIds, maxId);
            }
            return maxId;
        }

        private void DFSNumbers(int state, Dictionary<int, int> newIds, int Id)
        {
            newIds.Add(state, Id);
            foreach (char c in m_childTokensUpdate![state])
            {
                int tgt;
                m_gotoFnUpdate!.TryGetValue(state, c, out tgt);
                DFSNumbers(tgt, newIds, Id + 1);
            }
        }

        private void VisitSubtree(int state, List<int> entityIdList, int l, Dictionary<int, bool> visitedActiveNodes)
        {
            if (visitedActiveNodes.ContainsKey(state))
                return;

            int entityId = Output(state);
            if (m_AN1.IsActiveNode(state))
            {
                visitedActiveNodes.Add(state, true);
            }
            if (entityId != -1)
            {
                entityIdList.Add(entityId);
                if (entityIdList.Count >= l)
                    return;
            }

            int[]? entityIds;
            if (m_descendants.TryGetValue(state, out entityIds))
            {
                foreach (int eid in entityIds)
                {
                    entityIdList.Add(eid);
                    if (entityIdList.Count >= l)
                        break;
                }
                return;
            }

            //throw new Exception("Something wrong...");

            m_childState!.BeginTarget(state);
            int childState;
            while ((childState = m_childState.GetNextTarget()) != -1)
            {
                VisitSubtree(childState, entityIdList, l, visitedActiveNodes);
                if (entityIdList.Count >= l)
                    return;
            }
        }

        private void LevelVisit(int k)
        {
            BFS_Q.Clear();
            BFS_Q.Enqueue(0);
            m_AN1.AddNode(0, 0);

            while (BFS_Q.Count > 0)
            {
                int nextState = BFS_Q.Dequeue();
                //int distance = m_level[nextState];
                int distance = m_AN1.Distance(nextState);
                Debug.Assert(distance <= k);
                m_AN1.AddNode(nextState, distance);
                if (distance == k)
                    continue;
                m_childState!.BeginTarget(nextState);
                int tmpState;
                while ((tmpState = m_childState.GetNextTarget()) != -1)
                {
                    BFS_Q.Enqueue(tmpState);
                    m_AN1.AddNode(tmpState, distance + 1);
                }
            }
        }

        private string Normalize(string s)
        {
            return s.Trim().ToLower();
        }

        #endregion

    }
}
