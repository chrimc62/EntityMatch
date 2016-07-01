using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common
{
    /// <summary>
    /// Maintains parent-child relationships in a trie
    /// </summary>
    [Serializable()]
    public class TrieChildState
    {
        int[] m_hiChildren;
        System.Byte[] m_hiChildChars;
        int[] m_hiOffsets;

        System.Byte[] m_loChildChars;
        //int[] m_loOffsets;

        int m_source;
        int m_sourceIdx;
        int m_UB;

        char[] m_converter;
        int m_nHiSources;
        const char SEP = '$';
        byte b_SEP;

        #region public methods
        public TrieChildState(int nSources, int nHiSources, int nTotalEdges, int nHiEdges)
        {
            int nLoSources = nSources - nHiSources;
            int nLoEdges = nTotalEdges - nHiEdges;
            m_hiChildren = new int[nHiEdges];
            m_hiChildChars = new byte[nHiEdges];
            m_hiOffsets = new int[nHiSources];

            int nChains = nHiEdges - nHiSources + 1;
            m_loChildChars = new byte[nLoEdges + nChains];
            //m_loOffsets = new int[nLoSources];

            m_converter = new char[1];
            m_nHiSources = nHiSources;
            b_SEP = GetAscii(SEP);
            m_sourceIdx = 0;
        }

        public void BeginHiSource(int source, int nTargets)
        {
            m_hiOffsets[source] = m_sourceIdx;
        }

        public void AddHiTarget(int source, char c, int target)
        {
            m_hiChildChars[m_sourceIdx] = GetAscii(c);
            m_hiChildren[m_sourceIdx] = (target - source);
            m_sourceIdx++;
        }

        public void BeginLo()
        {
            m_sourceIdx = 0;
        }

        public void AddLo(int loId, char[] chain)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(chain);
            int startIdx = loId - m_nHiSources;
            foreach (byte b in bytes)
            {
                m_loChildChars[startIdx] = b;
                startIdx++;
            }
            m_loChildChars[startIdx] = b_SEP;
        }

        public int FindTarget(int source, char label)
        {
            if (source < m_nHiSources)
            {
                return FindHiTarget(source, label);
            }
            return FindLoTarget(source, label);
        }

        public void BeginTarget(int source)
        {
            m_source = source;
            if (source < m_nHiSources)
            {
                m_sourceIdx = m_hiOffsets[source];
                m_UB = (source + 1 < m_hiOffsets.Length) ? m_hiOffsets[source + 1] : m_hiChildChars.Length;
            }
            else
            {
                if (m_loChildChars[m_source - m_nHiSources] == b_SEP)
                {
                    m_UB = m_source;
                }
                else
                {
                    m_UB = m_source + 1;
                }
            }
        }

        public int GetNextTarget()
        {
            if (m_source < m_nHiSources)
            {
                return GetNextHiTarget();
            }
            if (m_UB == m_source)
                return -1;
            m_source++;
            return m_source;
        }

        #endregion

        #region private methods

        private void BeginHiTarget(int source)
        {
            m_sourceIdx = m_hiOffsets[source];
            m_UB = (source + 1 < m_hiOffsets.Length) ? m_hiOffsets[source + 1] : m_hiChildChars.Length;
        }

        private int GetNextHiTarget()
        {
            if (m_sourceIdx >= m_UB)
                return -1;
            int oldSrcIdx = m_sourceIdx;
            m_sourceIdx++;
            return m_source + m_hiChildren[oldSrcIdx];
        }

        private int FindHiTarget(int source, char label)
        {
            byte blabel = GetAscii(label);
            int nChars = m_hiChildChars.Length;
            int start = m_hiOffsets[source];
            if (start >= nChars)
            {
                return -1;
            }

            int end = (source + 1 < m_hiOffsets.Length) ? m_hiOffsets[source + 1] - 1 : nChars - 1;
            int idx;
            while (start <= end)
            {
                idx = (int)((start + end) / 2);
                if (m_hiChildChars[idx] < blabel)
                {
                    start = idx + 1;
                }
                else if (m_hiChildChars[idx] > blabel)
                {
                    end = idx - 1;
                }
                else // childChars[idx] == label
                {
                    return source + m_hiChildren[idx];
                }
            }
            return -1;
        }

        private int FindLoTarget(int source, char label)
        {
            byte blabel = GetAscii(label);
            int idx = source - m_nHiSources;
            if (m_loChildChars[idx] == blabel)
            {
                return source + 1;
            }
            return -1;
        }

        private byte GetAscii(char c)
        {
            m_converter[0] = c;
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(m_converter);
            return bytes[0];
        }
        #endregion
    }
}
