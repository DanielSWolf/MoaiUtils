using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MoaiUtils.Tools;

namespace MoaiUtils.LuaIO {
    public class LuaTableWriter {
        public static void Write(LuaTable table, FileInfo fileInfo, LuaComment headComment = null) {
            using (var file = fileInfo.CreateText()) {
                Write(table, file, headComment);
            }
        }

        public static string ToString(LuaTable table, LuaComment headComment = null) {
            using (var stringWriter = new StringWriter()) {
                Write(table, stringWriter, headComment);
                return stringWriter.ToString();
            }
        }

        public static void Write(LuaTable table, TextWriter textWriter, LuaComment headComment = null) {
            if (table == null) throw new ArgumentNullException("table");
            if (textWriter == null) throw new ArgumentNullException("textWriter");

            using (var indentedTextWriter = new IndentedTextWriter(textWriter, "    ")) {
                if (headComment != null) {
                    Write(headComment, indentedTextWriter);
                }
                indentedTextWriter.Write("return ");
                Write(table, indentedTextWriter);
            }
        }

        private static void Write(bool value, IndentedTextWriter indentedTextWriter) {
            indentedTextWriter.Write(value ? "true" : "false");
        }

        private static void Write(double value, IndentedTextWriter indentedTextWriter) {
            indentedTextWriter.Write(value.ToString(CultureInfo.InvariantCulture));
        }

        private static void Write(string value, IndentedTextWriter indentedTextWriter) {
            // Determine whether to single or double quote the string - whichever is shorter
            int singleQuoteCount = value.Count(c => c == '\'');
            int doubleQuoteCount = value.Count(c => c == '"');
            bool useSingleQuotes = doubleQuoteCount > singleQuoteCount;

            // Escape string
            var escapeSequences = new Dictionary<char, string> {
                { '\n', @"\n" },
                { '\r', @"\r" },
                { '\t', @"\t" },
                { '\\', @"\\" },
                { useSingleQuotes ? '\'' : '"', useSingleQuotes ? @"\'" : @"\""" }
            };
            var stringBuilder = new StringBuilder(value.Length + 2);
            stringBuilder.Append(useSingleQuotes ? "'" : "\"");
            foreach (char c in value) {
                if (escapeSequences.ContainsKey(c)) {
                    stringBuilder.Append(escapeSequences[c]);
                } else if (c < 32) {
                    stringBuilder.AppendFormat(@"\x{0:X2}", (int) c);
                } else {
                    stringBuilder.Append(c);
                }
            }
            stringBuilder.Append(useSingleQuotes ? "'" : "\"");

            indentedTextWriter.Write(stringBuilder.ToString());
        }

        private static void Write(LuaTable table, IndentedTextWriter indentedTextWriter) {
            // Write empty tables in a single line
            if (table.Count == 0) {
                indentedTextWriter.Write("{}");
                return;
            }

            // Determine whether to use auto-indexing for numeric keys.
            // Use auto-indexing if all n keys of numeric type form the sequence 1, 2, 3 .. n
            bool useAutoIndexing = table
                .Where(pair => pair.Key is double)
                .Select((pair, index) => new { Key = (double) pair.Key, Index = index })
                .All(element => element.Key == element.Index + 1);

            // Serialize table
            indentedTextWriter.WriteLine("{");
            using (indentedTextWriter.IndentBlock()) {
                var pairs = table.ToList();
                for (int i = 0; i < pairs.Count; i++) {
                    dynamic key = pairs[i].Key;
                    dynamic value = pairs[i].Value;
                    if (value is LuaComment) {
                        // Comment. This is easy.
                        Write(value, indentedTextWriter);
                    } else {
                        // Actual entry, not a comment
                        if (useAutoIndexing && key is double) {
                            // Use auto-indexing: omit the key
                            Write(value, indentedTextWriter);
                        } else {
                            // Use key-value syntax
                            if (key is string && IsValidIdentifier(key)) {
                                // Use identifier syntax for key
                                indentedTextWriter.Write(key);
                            } else {
                                // Use bracket syntax for key
                                indentedTextWriter.Write("[");
                                Write(key, indentedTextWriter);
                                indentedTextWriter.Write("]");
                            }
                            indentedTextWriter.Write(" = ");
                            Write(value, indentedTextWriter);
                        }

                        // Figure out whether to add a comma
                        if (pairs.Skip(i + 1).Any(pair => !(pair.Value is LuaComment))) {
                            indentedTextWriter.WriteLine(",");
                        } else {
                            indentedTextWriter.WriteLine();
                        }
                    }
                }
            }
            indentedTextWriter.Write("}");
        }

        private static readonly HashSet<string> reservedWords = new HashSet<string> {
            "and", "break", "do", "else", "elseif", "end", "false", "for", "function", "if", "in",
            "local", "nil", "not", "or", "repeat", "return", "then", "true", "until", "while"
        };

        private static readonly Regex identifierRegex = new Regex("^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled);

        private static bool IsValidIdentifier(string s) {
            return (!reservedWords.Contains(s) && identifierRegex.IsMatch(s));
        }

        private static void Write(LuaFunction value, IndentedTextWriter indentedTextWriter) {
            if (value != LuaFunction.Empty) {
                throw new NotSupportedException("Only empty Lua functions are supported.");
            }
            indentedTextWriter.Write("function () end");
        }

        private static void Write(LuaComment comment, IndentedTextWriter indentedTextWriter) {
            if (comment.BlankLineBefore) {
                indentedTextWriter.WriteLine();
            }
            foreach (var line in comment.Text.SplitIntoLines()) {
                indentedTextWriter.WriteLine("-- {0}", line);
            }
            if (comment.BlankLineAfter) {
                indentedTextWriter.WriteLine();
            }
        }
    }
}