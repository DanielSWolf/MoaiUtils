using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MoaiUtils.Tools {
    public static class StringExtensions {
        public static IEnumerable<string> SplitIntoLines(this string s) {
            using (var stringReader = new StringReader(s)) {
                while (true) {
                    string line = stringReader.ReadLine();
                    if (line != null) {
                        yield return line;
                    } else {
                        yield break;
                    }
                }
            }
        }

        public static string Join(this IEnumerable<string> elements, string separator) {
            return string.Join(separator, elements.ToArray());
        }

        public static string Enclose(this string s, string left, string right) {
            var result = new StringBuilder(left.Length + s.Length + right.Length);
            result.Append(left);
            result.Append(s);
            result.Append(right);
            return result.ToString();
        }

        public static string GetExcerpt(this string s, int maxLength = 20) {
            if (s.Length <= maxLength) return s;
            s = s.Substring(0, maxLength - 3);
            s = s.Split('\r', '\n')[0];
            return s + "...";
        }

    }
}