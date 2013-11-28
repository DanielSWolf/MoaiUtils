using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Tools {
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
    }
}