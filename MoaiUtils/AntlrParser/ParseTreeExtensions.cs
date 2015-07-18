using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using CppParser.FileStreams;

namespace CppParser {

	public static class ParseTreeExtensions {

		#region Code positions

		/// <summary>
		/// Returns the position of the token's first character.
		/// </summary>
		public static CodePosition StartPosition(this IToken token) {
			var inputStream = (FileStreamBase) token.InputStream;
			return new CodePosition(inputStream, token.StartIndex);
		}

		/// <summary>
		/// Returns the position of the token's last character.
		/// </summary>
		public static CodePosition EndPosition(this IToken token) {
			var inputStream = (FileStreamBase) token.InputStream;
			return new CodePosition(inputStream, token.StopIndex);
		}

		/// <summary>
		/// Returns the position beyond the token's last character.
		/// If the input stream ends with the token, returns the position of the token's last character.
		/// </summary>
		public static CodePosition NextPosition(this IToken token) {
			var inputStream = (FileStreamBase) token.InputStream;
			int charIndex = token.StopIndex;
			while ((charIndex + 1 < inputStream.Size) && (charIndex == token.StopIndex || inputStream[charIndex] == '\n')) {
				charIndex++;
			}

			return new CodePosition(inputStream, charIndex);
		}

		/// <summary>
		/// Returns the position of the context's first character.
		/// </summary>
		public static CodePosition StartPosition(this ParserRuleContext parserRuleContext) {
			return parserRuleContext.Start.StartPosition();
		}

		/// <summary>
		/// Returns the position of the context's last character.
		/// </summary>
		public static CodePosition EndPosition(this ParserRuleContext parserRuleContext) {
			return parserRuleContext.Stop.EndPosition();
		}

		/// <summary>
		/// Returns the position beyond the context's last character.
		/// If the input stream ends with the context, returns the position of the context,'s last character.
		/// </summary>
		public static CodePosition NextPosition(this ParserRuleContext parserRuleContext) {
			return parserRuleContext.Stop.NextPosition();
		}

		#endregion

		#region Text

		/// <summary>
		/// <see cref="RuleContext.GetText"/> returns only those parts of the string that are matched by the grammar,
		/// which is very annoying for output.
		/// </summary>
		public static string GetActualText(this ParserRuleContext parserRuleContext) {
			var inputStream = (FileStreamBase) parserRuleContext.Start.InputStream;
			int startIndex = parserRuleContext.Start.StartIndex;
			int stopIndex = parserRuleContext.Stop.StopIndex;
			return inputStream.GetText(new Interval(startIndex, stopIndex));
		}

		public static TokenValue<string> TextToTokenValue(this IToken token) {
			return TokenValue.Create(token.Text, token);
		}

		public static ContextValue<string, TContext> TextToContextValue<TContext>(this TContext context) where TContext : ParserRuleContext {
			return ContextValue.Create(context.GetActualText(), context);
		}

		#endregion

		#region Tree queries

		public static IEnumerable<IParseTree> GetDescendants(this IParseTree parseTree, bool includeSelf = false) {
			if (includeSelf) yield return parseTree;

			for (int childIndex = 0; childIndex < parseTree.ChildCount; childIndex++) {
				IParseTree child = parseTree.GetChild(childIndex);
				foreach (var descendant in child.GetDescendants(includeSelf: true)) {
					yield return descendant;
				}
			}
		}

		public static IEnumerable<T> GetDescendants<T>(this IParseTree parseTree, bool includeSelf = false) {
			return parseTree.GetDescendants(includeSelf).OfType<T>();
		}

		public static T GetDescendant<T>(this IParseTree parseTree, bool includeSelf = false) where T : class {
			T result = parseTree.GetDescendantOrNull<T>(includeSelf);
			if (result == null) {
				throw new ArgumentException($"Parse tree {parseTree} has no descendant of type {typeof(T).Name}.");
			}
			return result;
		}

		public static T GetDescendantOrNull<T>(this IParseTree parseTree, bool includeSelf = false) {
			return parseTree.GetDescendants<T>(includeSelf).SingleOrDefault();
		}

		#endregion

	}

}