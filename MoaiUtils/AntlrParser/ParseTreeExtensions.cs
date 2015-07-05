using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace CppParser {

	public static class ParseTreeExtensions {

		public static CodePosition GetCodePosition(this IToken token) {
			var inputStream = (AntlrInputStream) token.InputStream;
			return new CodePosition(inputStream, token.StartIndex);
		}

		public static CodePosition GetCodePosition(this ParserRuleContext parserRuleContext) {
			return parserRuleContext.Start.GetCodePosition();
		}

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

		public static T GetDescendant<T>(this IParseTree parseTree, bool includeSelf = false) {
			return parseTree.GetDescendants<T>(includeSelf).Single();
		}

		public static T GetDescendantOrNull<T>(this IParseTree parseTree, bool includeSelf = false) {
			return parseTree.GetDescendants<T>(includeSelf).SingleOrDefault();
		}

	}

}