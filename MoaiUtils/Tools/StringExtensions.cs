using System.Collections.Generic;
using System.IO;

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
    }
}