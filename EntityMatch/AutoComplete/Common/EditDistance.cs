using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common;

namespace Common
{

    public class PairwiseEditDist
    {
        // We will do edit distance only for strings of len <= 128
        private const int MAX_LEN = 128;

        // Two-dim array for edit distance computation
        private int[,] dist;

        public const int INFTY = 1000;

        public PairwiseEditDist()
        {
            dist = new int[MAX_LEN + 1, MAX_LEN + 1];

            // These entries are unchanged over any no of computations
            for (int i = 0; i <= MAX_LEN; i++)
                dist[0, i] = dist[i, 0] = i;
        }

        // Check if edit distance (s1, s2) <= k
        public int check(string s1, string s2, int k)
        {
            int n1 = s1.Length;
            int n2 = s2.Length;

            int l, r, flag, rdecr;

            // See assertion in for loop
            l = 1;
            r = k;

            for (int i = 1; i <= n2; i++)
            {
                // Assert: Only entries C[i-1,l] ... C[i-1,r] are <= k

                r++;
                if (r <= n1)
                {
                    dist[i - 1, r] = k + 1;
                }
                else
                {
                    r = n1;
                }
                if (l > 1)
                {
                    dist[i, l - 1] = k + 1;
                }

                flag = 1;
                rdecr = 0;
                for (int j = l; j <= r; j++)
                {

                    if (s2[i - 1] == s1[j - 1])
                    {
                        dist[i, j] = dist[i - 1, j - 1];
                    }
                    else
                    {
                        dist[i, j] = (dist[i - 1, j - 1] < dist[i - 1, j]) ?
                            dist[i - 1, j - 1] : dist[i - 1, j];
                        dist[i, j] = (dist[i, j] < dist[i, j - 1]) ?
                            dist[i, j] : dist[i, j - 1];
                        dist[i, j]++;
                    }

                    rdecr++;

                    if (dist[i, j] <= k)
                    {
                        rdecr = 0;
                        flag = 0;
                    }

                    l += flag;
                }

                r -= rdecr;

                if (l > r)
                {
                    //return false;
                    return INFTY;
                }
            }

            //return (r == n1);
            if (r != n1)
            {
                return INFTY;
            }
            return dist[n2, n1];
        }

        // Check if edit distance (s1, s2) == k
        public bool checkExact(string s1, string s2, int k)
        {
            int n1 = s1.Length;
            int n2 = s2.Length;

            int l, r, flag, rdecr;

            // See assertion in for loop
            l = 1;
            r = k;

            for (int i = 1; i <= n2; i++)
            {
                // Assert: Only entries C[i-1,l] ... C[i-1,r] are <= k

                r++;
                if (r <= n1)
                {
                    dist[i - 1, r] = k + 1;
                }
                else
                {
                    r = n1;
                }
                if (l > 1)
                {
                    dist[i, l - 1] = k + 1;
                }

                flag = 1;
                rdecr = 0;
                for (int j = l; j <= r; j++)
                {

                    if (s2[i - 1] == s1[j - 1])
                    {
                        dist[i, j] = dist[i - 1, j - 1];
                    }
                    else
                    {
                        dist[i, j] = (dist[i - 1, j - 1] < dist[i - 1, j]) ?
                            dist[i - 1, j - 1] : dist[i - 1, j];
                        dist[i, j] = (dist[i, j] < dist[i, j - 1]) ?
                            dist[i, j] : dist[i, j - 1];
                        dist[i, j]++;
                    }

                    rdecr++;

                    if (dist[i, j] <= k)
                    {
                        rdecr = 0;
                        flag = 0;
                    }

                    l += flag;
                }

                r -= rdecr;

                if (l > r)
                    return false;
            }

            return (r == n1 && dist[n2, n1] == k);
        }

        // Check if edit distance (s1, s2) <= k
        public void prefixcheck(string s1, string s2, int k, List<int> positions, List<int> distance)
        {
            int n1 = s1.Length;
            int n2 = s2.Length;

            int l, r, flag, rdecr;

            // See assertion in for loop
            l = 1;
            r = k;

            for (int i = 1; i <= n2; i++)
            {
                // Assert: Only entries C[i-1,l] ... C[i-1,r] are <= k

                r++;
                if (r <= n1)
                {
                    dist[i - 1, r] = k + 1;
                }
                else
                {
                    r = n1;
                }
                if (l > 1)
                {
                    dist[i, l - 1] = k + 1;
                }

                flag = 1;
                rdecr = 0;
                for (int j = l; j <= r; j++)
                {

                    if (s2[i - 1] == s1[j - 1])
                    {
                        dist[i, j] = dist[i - 1, j - 1];
                    }
                    else
                    {
                        dist[i, j] = (dist[i - 1, j - 1] < dist[i - 1, j]) ?
                            dist[i - 1, j - 1] : dist[i - 1, j];
                        dist[i, j] = (dist[i, j] < dist[i, j - 1]) ?
                            dist[i, j] : dist[i, j - 1];
                        dist[i, j]++;
                    }

                    rdecr++;

                    if (dist[i, j] <= k)
                    {
                        rdecr = 0;
                        flag = 0;
                        if (i == n2)
                        {
                            positions.Add(j);
                            distance.Add(dist[i, j]);
                        }
                    }

                    l += flag;
                }

                r -= rdecr;

                if (l > r)
                {
                    //return false;
                    //return INFTY;
                    return;
                }
            }

            //return (r == n1);
            //if (r != n1)
            //{
            //    return INFTY;
            //}
            //return dist[n2, n1];
        }


    }
}