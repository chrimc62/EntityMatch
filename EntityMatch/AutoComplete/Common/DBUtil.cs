using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Diagnostics;

namespace Common
{
    public class DBUtil
    {
        static Dictionary<string, bool> DuplicateDetector = new Dictionary<string, bool>();

        public static void BuildFromDatabase(string server, string db, string table, string column, IAdd t)
        {
            DuplicateDetector.Clear();
            t.BeginUpdate();
            SqlConnection connection = new SqlConnection(Util.GetConnectionString(server, db));
            connection.Open();
            string q = TrieQuery(db, table, column);
            SqlCommand sqlcmd = new SqlCommand(q, connection);
            sqlcmd.CommandTimeout = 0;

            int count = 0;
            using (SqlDataReader reader = sqlcmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    count++;
                    if (count == 10)
                    {
                        Console.Write("");
                        //break;
                    }
                    string s = reader.GetString(0);
                    if (DuplicateDetector.ContainsKey(s))
                    {
                        if (count % 10000 == 0)
                        {
                            Console.WriteLine("{1}: Inserted {0} records", count, DateTime.Now);
                        }
                        continue;
                    }
                    DuplicateDetector.Add(s, true);
                    t.Add(s);
                    if (count % 10000 == 0)
                    {
                        Console.WriteLine("{1}: Inserted {0} records", count, DateTime.Now);
                    }
                }
            }
            connection.Close();
            t.EndUpdate();
        }

        public static long LookupFromDatabase(string server, string db, string table, string column, int nLookups, ILookupIndex t, int k)
        {
            SqlConnection connection = new SqlConnection(Util.GetConnectionString(server, db));
            connection.Open();
            string q = TrieQuery(db, table, column, nLookups);
            SqlCommand sqlcmd = new SqlCommand(q, connection);
            sqlcmd.CommandTimeout = 0;

            string[] lookupStrings = new string[nLookups];
            int idx = 0;
            using (SqlDataReader reader = sqlcmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    lookupStrings[idx++] = reader.GetString(0);
                }
            }
            connection.Close();

            Stopwatch sw = new Stopwatch();
            sw.Reset();
            sw.Start();

            foreach (string s in lookupStrings)
            {
                int nresults = t.Lookup(s, k);
            }
            sw.Stop();

            return (sw.ElapsedMilliseconds);
        }

        private static string TrieQuery(string db, string table, string column)
        {
            return "select " + column + " from " + db + ".." + table;
        }

        private static string TrieQuery(string db, string table, string column, int NLookups)
        {
            return "select top " + NLookups + " " + column + " from " + db + ".." + table;
        }

    }

}


