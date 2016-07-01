using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common;

namespace EditTrie
{
    [Serializable()]
    class ActiveNodes
    {
        Queue<int> m_Q;
        Dictionary<int, int> m_ht = new Dictionary<int, int>();
        const int infty = 10000;
        List<int> m_nodeList = new List<int>();
        Dictionary<int, int> m_dist2count = new Dictionary<int, int>();
        Dictionary<int, List<int>> m_dist2nodes = new Dictionary<int, List<int>>();
        int m_minDist;
        int m_maxDist;

        #region public methods
        public ActiveNodes()
        {
            m_Q = new Queue<int>();
        }

        public int Count
        {
            get { return m_ht.Count; }
        }

        public void Clear()
        {
            m_ht.Clear();
            m_Q.Clear();
            foreach (KeyValuePair<int, List<int>> kvp in m_dist2nodes)
                kvp.Value.Clear();
            m_minDist = infty;
            m_maxDist = 0;
        }

        public bool IsActiveNode(int node)
        {
            return m_ht.ContainsKey(node);
        }

        public int Distance(int node)
        {
            int dist;
            if (!m_ht.TryGetValue(node, out dist))
                return infty;
            return dist;
        }

        public void AddNode(int node, int distance)
        {
            int currDistance;
            if (m_ht.TryGetValue(node, out currDistance))
            {
                if (currDistance > distance)
                {
                    m_ht[node] = distance;
                }
                return;
            }
            m_ht.Add(node, distance);
            m_Q.Enqueue(node);
            if (!m_dist2nodes.ContainsKey(distance))
            {
                m_dist2nodes.Add(distance, new List<int>());
            }
            m_dist2nodes[distance].Add(node);
            if (distance < m_minDist)
                m_minDist = distance;
            if (distance > m_maxDist)
                m_maxDist = distance;
        }

        public int GetNext()
        {
            if (m_Q.Count == 0)
                return -1;
            return m_Q.Dequeue();
        }

        public void ClearDistances()
        {
            //m_Q.Clear();
            m_ht.Clear();
            foreach (KeyValuePair<int, List<int>> kvp in m_dist2nodes)
                kvp.Value.Clear();
            m_minDist = infty;
            m_maxDist = 0;
        }

        public void DistanceSort(List<int> outNodes, int l, Dictionary<int, int[]> descendants)
        {
            outNodes.Clear();
            m_dist2count.Clear();
            //int mindist = infty, maxdist = 0;
            //Dictionary<int, int>.Enumerator aEnum = m_ht.GetEnumerator();
            //while (aEnum.MoveNext())
            //{
            //    int nodeid = aEnum.Current.Key;
            //    int dist = aEnum.Current.Value;
            //    if (dist < mindist)
            //        mindist = dist;
            //    if (dist > maxdist)
            //        maxdist = dist;
            //    if (!m_dist2count.ContainsKey(dist))
            //        m_dist2count.Add(dist, 0);
            //    m_dist2count[dist]++;
            //    outNodes.Add(aEnum.Current.Key);
            //}

            //return;

            int cumCnt = 0;
            for (int d = m_minDist; d <= m_maxDist; d++)
            {
                List<int> nodeList;
                if (m_dist2nodes.TryGetValue(d, out nodeList))
                {
                    foreach (int nodeid in nodeList)
                    {
                        int[] desc;
                        if (descendants.TryGetValue(nodeid, out desc))
                            cumCnt += desc.Length;
                        outNodes.Add(nodeid);
                        if (cumCnt >= l)
                            return;
                    }
                }
            }

            //Dictionary<int, int>.Enumerator aEnum2 = m_ht.GetEnumerator();
            //while (aEnum2.MoveNext())
            //{
            //    int nodeid = aEnum2.Current.Key;
            //    int dist = aEnum.Current.Value;
            //    int idx = m_dist2count[dist];
            //    outNodes[idx] = nodeid;
            //    m_dist2count[dist]++;
            //}

            //m_dist2count.Clear();
            //SortByDistance(outNodes, 0, outNodes.Count - 1);
        }

        #endregion

        private void SortByDistance(List<int> nodes, int left, int right)
        {
            int l_hold, r_hold;
            int pivot;

            l_hold = left;
            r_hold = right;
            pivot = nodes[left];

            while (left < right)
            {
                while ((Distance(pivot) <= Distance(nodes[right])) && (left < right))
                    right--;
                if (left != right)
                {
                    nodes[left] = nodes[right];
                    left++;
                }
                while ((Distance(nodes[left]) <= Distance(pivot)) && (left < right))
                    left++;
                if (left != right)
                {
                    nodes[right] = nodes[left];
                    right--;
                }
            }
            nodes[left] = pivot;
            int tmp = left;
            left = l_hold;
            right = r_hold;
            if (left < tmp)
            {
                SortByDistance(nodes, left, tmp - 1);
            }
            if (right > tmp)
            {
                SortByDistance(nodes, tmp + 1, right);
            }
        }


    }


}
