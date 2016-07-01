using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Configuration;
using System.Collections;

namespace Common
{
    public static class Util
    {
        public static string GetConnectionString(string server, string db)
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
            builder.DataSource = server;
            builder.InitialCatalog = db;
            builder.IntegratedSecurity = true;
            return builder.ConnectionString;
        }

        public static string QuoteName(string name)
        {
            return "[" + name.Replace("]", "]]") + "]";
        }

        public static string ToQualifiedTableName(string serverPart, string dbPart, string schemaPart, string tablePart)
        {
            if (string.IsNullOrEmpty(tablePart))
                throw new ArgumentNullException("tablePart");
            StringBuilder buf = new StringBuilder();
            if (!string.IsNullOrEmpty(serverPart))
            {
                buf.Append(QuoteName(serverPart));
            }
            if (buf.Length > 0)
            {
                buf.Append('.');
            }
            if (!string.IsNullOrEmpty(dbPart))
            {
                buf.Append(QuoteName(dbPart));
            }
            if (buf.Length > 0)
            {
                buf.Append('.');
            }
            if (!string.IsNullOrEmpty(schemaPart))
            {
                buf.Append(QuoteName(schemaPart));
            }
            if (buf.Length > 0)
            {
                buf.Append('.');
            }
            buf.Append(QuoteName(tablePart));
            return buf.ToString();
        }


        public static string ToQualifiedTableName(string dbPart, string schemaPart, string tablePart)
        {
            if (string.IsNullOrEmpty(tablePart))
                throw new ArgumentNullException("tablePart");
            StringBuilder buf = new StringBuilder();
            if (!string.IsNullOrEmpty(dbPart))
            {
                buf.Append(QuoteName(dbPart));
            }
            if (buf.Length > 0)
            {
                buf.Append('.');
            }
            if (!string.IsNullOrEmpty(schemaPart))
            {
                buf.Append(QuoteName(schemaPart));
            }
            if (buf.Length > 0)
            {
                buf.Append('.');
            }
            buf.Append(QuoteName(tablePart));
            return buf.ToString();
        }

        public static string QuoteSqlLiteral(string literal)
        {
            if (literal == null)
                throw new ArgumentNullException("literal");
            return "'" + literal.Replace("'", "''") + "'";
        }

        // this does not check whether there is invalid character in pathToFolder 
        // or fileName such as '\\'
        public static string GetFilePath(string pathToFolder, string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException("fileName");
            if (pathToFolder == null)
                return fileName;
            if (pathToFolder.EndsWith("\\", StringComparison.Ordinal))
            {
                return pathToFolder + fileName;
            }
            else
            {
                return pathToFolder + '\\' + fileName;
            }
        }

        public static string ConcatSubString(this string[] tokens, int start, int length)
        {
            if (start < 0 || start >= tokens.Length)
                throw new ArgumentOutOfRangeException("start");
            if (length <= 0 || (start + length > tokens.Length))
                throw new ArgumentOutOfRangeException("length");
            if (length == 1)
                return tokens[start];
            StringBuilder buf = new StringBuilder();
            buf.Append(tokens[start]);
            for (int i = 1; i < length; i++)
            {
                string tok = tokens[start + i];
                if (tok.Length > 1 || IsDigitOrLetter(tok[0]))
                {
                    buf.Append(' ');
                }
                buf.Append(tokens[start + i]);
            }
            return buf.ToString();
        }

        public static T[] SubArray<T>(this T[] array, int start, int length)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (start < 0 || start >= array.Length)
                throw new ArgumentOutOfRangeException("start");
            if (length <= 0 || (start + length) > array.Length)
                throw new ArgumentOutOfRangeException("length");
            T[] result = new T[length];
            Array.Copy(array, start, result, 0, length);
            return result;
        }


        public static bool IsDigitAscii(this char ch)
        {
            return (ch >= '0' && ch <= '9');
        }

        public static bool IsLetterAscii(this char ch)
        {
            return (ch >= 'A' && ch <= 'Z' || ch >= 'a' && ch <= 'z');
        }

        public static bool IsDigitOrLetter(this char ch)
        {
            return (ch >= '0' && ch <= '9' ||
                    ch >= 'A' && ch <= 'Z' ||
                    ch >= 'a' && ch <= 'z');
        }

        // try to parse a numeric value assuming it looks like a number followed by a unit
        public static bool TryParseNumericAttrValue(string value, out string numberPart, out string unitPart)
        {
            numberPart = null;
            unitPart = null;
            if (value == null)
                return false;
            int start = 0;
            bool seenDot = false;
            for (; start < value.Length; start++)
            {
                char ch = value[start];
                if (ch.IsDigitAscii())
                    break;
                if (ch == '.' && start < value.Length - 1 && value[start + 1].IsDigitAscii())
                {
                    seenDot = true;
                    break;
                }
            }
            if (start >= value.Length)
                return false;
            int end = start + 1;
            for (; end < value.Length; end++)
            {
                char ch = value[end];
                if (ch.IsDigitAscii())
                    continue;
                if (ch != '.' || seenDot)
                    break;
                seenDot = true;
            }
            if (start == 0 && end == value.Length)
            {
                numberPart = value;
            }
            else
            {
                numberPart = value.Substring(start, end - start);
                if (end < value.Length)
                {
                    unitPart = value.Substring(end, value.Length - end).Trim();
                }
            }
            return true;
        }

        // the input is the output of TryParseNumericAttrValue() or ScanNumber()
        // so only checking '0' and '.' is enough
        public static bool IsZeroNumber(this string number)
        {
            if (string.IsNullOrEmpty(number))
                throw new ArgumentNullException("number");
            for (int i = 0; i < number.Length; i++)
            {
                if (number[i] != '0' && number[i] != '.')
                    return false;
            }
            return true;
        }

        public static int ScanNumber(this string token, int start)
        {
            Debug.Assert(!string.IsNullOrEmpty(token));
            Debug.Assert(start >= 0 && start < token.Length);
            Debug.Assert(token[start].IsDigitAscii());
            bool seenDot = false;
            for (int i = start + 1; i < token.Length; i++)
            {
                char ch = token[i];
                if (ch.IsDigitAscii())
                    continue;
                if (ch != '.' || seenDot)
                    return i;
                seenDot = true;
            }
            return token.Length;
        }

        public static bool HasDigit(this string token, int start)
        {
            Debug.Assert(!string.IsNullOrEmpty(token));
            Debug.Assert(start >= 0 && start < token.Length);
            for (int i = start; i < token.Length; i++)
            {
                char ch = token[i];
                if (ch.IsDigitAscii())
                    return true;
            }
            return false;
        }

        public static void Swap<T>(ref T a, ref T b)
        {
            T tmp = a;
            a = b;
            b = tmp;
        }

        public static bool AllLetters(this string token)
        {
            if (token == null)
                throw new ArgumentNullException(token);
            for (int i = 0; i < token.Length; i++)
            {
                if (!token[i].IsLetterAscii())
                    return false;
            }
            return true;
        }
    }

}
