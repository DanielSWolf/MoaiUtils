using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Dfa;
using Antlr4.Runtime.Tree;
using CppParser.CppSymbols;
using MoaiUtils.Tools;

namespace CppParser {

	internal class Program {

		private class LexerDebugErrorListener : IAntlrErrorListener<int> {
			private readonly Action addError;

			public LexerDebugErrorListener(Action addError) {
				this.addError = addError;
			}

			public void SyntaxError(IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e) {
				addError();
			}
		}

		private class DebugErrorListener : BaseErrorListener {
			private readonly Action addError;

			public DebugErrorListener(Action addError) {
				this.addError = addError;
			}

			public override void SyntaxError(IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e) {
				addError();
			}

			public override void ReportContextSensitivity(Parser recognizer, DFA dfa, int startIndex, int stopIndex, int prediction, SimulatorState acceptState) { }
		}

		private static void Main(string[] args) {
			string[] cppExtensions = { ".h", ".cpp", ".m", ".mm" };
			var sourceDir = new DirectoryInfo(@"X:\dev\projects\moai-dev\src");
			var files = sourceDir.EnumerateFiles("*.*", SearchOption.AllDirectories)
				.Where(file => cppExtensions.Contains(file.Extension.ToLowerInvariant()))
				.ToList();

			int errorCount = 0;
			IntProgress progress = new IntProgress { MaxValue = files.Count + 1 };
			Console.Write("Parsing files... ");
			using (new ProgressBar(progress)) {
				{
					// TODO: Use better-suited stream
					ICharStream charStream = new CppFileStream(sourceDir.GetFileInfo(@"lua-headers\moai.lua"));
					LuaLexer lexer = new LuaLexer(charStream);
					lexer.RemoveErrorListeners();
					lexer.AddErrorListener(new LexerDebugErrorListener(() => errorCount++));
					CommonTokenStream tokenStream = new CommonTokenStream(lexer);
					LuaParser parser = new LuaParser(tokenStream);
					parser.RemoveErrorListeners();
					parser.AddErrorListener(new DebugErrorListener(() => errorCount++));

					IParseTree parseTree = parser.chunk();

					progress.Value++;
				}

				List<CppParser.FileContext> fileContexts = new List<CppParser.FileContext>();
				foreach (FileInfo file in files) {
					CppParser.FileContext fileContext = ParseCppFile(file, () => errorCount++);
					fileContexts.Add(fileContext);

					progress.Value++;
				}

				Dictionary<string, ICppType> cppTypes = GetCppTypes(fileContexts);
			}

			Console.WriteLine("Done with {0} errors.", errorCount);
			Console.ReadLine();
		}

		private static CppParser.FileContext ParseCppFile(FileInfo file, Action addError) {
			ICharStream charStream = new CppFileStream(file);
			CppLexer lexer = new CppLexer(charStream);
			lexer.RemoveErrorListeners();
			lexer.AddErrorListener(new LexerDebugErrorListener(addError));
			CommonTokenStream tokenStream = new CommonTokenStream(lexer);
			CppParser parser = new CppParser(tokenStream);
			parser.RemoveErrorListeners();
			parser.AddErrorListener(new DebugErrorListener(addError));

			return parser.file();
		}

		private static Dictionary<string, ICppType> GetCppTypes(IReadOnlyList<CppParser.FileContext> fileContexts) {

			// Collect type drafts (potentially multiple entries per name)
			var typeDrafts = new MultiValueDictionary<string, ICppTypeDraft>();
			foreach (var fileContext in fileContexts) {
				// Handle typedefs
				var typedefs = fileContext.GetDescendants<CppParser.TypedefContext>();
				foreach (var typedef in typedefs) {
					CppTypedefDraft typedefDraft = typedef.type().TypedefDraft;
					// Skip redundant typedefs, such as "typedef struct lua_State lua_State;"
					if (typedefDraft.Name == typedefDraft.TargetName) continue;

					typeDrafts.Add(typedefDraft.Name, typedefDraft);
				}

				// Handle class definitions
				var classDefinitions = fileContext.GetDescendants<CppParser.ClassDefinitionContext>();
				foreach (var classDefinition in classDefinitions) {
					// Skip unnamed types, such as "enum { Foo, Bar };"
					string name = classDefinition.TypeName;
					if (name == null) continue;

					var classDraft = new CppClassDraft(classDefinition.GetCodePosition(), name, classDefinition.BaseTypeNames);
					typeDrafts.Add(name, classDraft);
				}
			}

			// Decide on a single concrete (non-draft) type per name
			var types = new Dictionary<string, ICppType> {
				["void"] = PrimitiveCppType.Void,
				["bool"] = PrimitiveCppType.Bool,
				["int"] = PrimitiveCppType.Number,
				["char"] = PrimitiveCppType.Number,
				["short"] = PrimitiveCppType.Number,
				["long"] = PrimitiveCppType.Number,
				["float"] = PrimitiveCppType.Number,
				["double"] = PrimitiveCppType.Number,
				["size_t"] = PrimitiveCppType.Number,
				["ptrdiff_t"] = PrimitiveCppType.Number,
				["wchar_t"] = PrimitiveCppType.String
			};
			foreach (string typeName in typeDrafts.Keys) {
				ResolveType(typeName, typeDrafts, types);
			}

			return types;
		}

		private static IList<string> unknownTypes = new List<string>(); // TODO: remove

		private static void ResolveType(string typeName, MultiValueDictionary<string, ICppTypeDraft> typeDrafts, Dictionary<string, ICppType> types) {
			if (types.ContainsKey(typeName)) return;

			if (typeDrafts.ContainsKey(typeName)) {
				IReadOnlyCollection<ICppTypeDraft> drafts = typeDrafts[typeName];
				Func<string, ICppType> resolveName = name => {
					ResolveType(name, typeDrafts, types);
					return types[name];
				};
				var typeCandidates = drafts
					.Select(typeDraft => ResolveTypeDraft(typeDraft, resolveName))
					.ToList();
				ICppType mergedType = MergeTypes(typeCandidates);
				types[typeName] = mergedType;
			} else {
				types[typeName] = PrimitiveCppType.Unknown;
				unknownTypes.Add(typeName);
			}
		}

		/// <summary>
		/// Takes one or more concrete types and joins them into one.
		/// Required whenever a type has multiple definitions based on #ifdef's.
		/// </summary>
		/// <param name="types"></param>
		/// <returns></returns>
		private static ICppType MergeTypes(IReadOnlyCollection<ICppType> types) {
			if (!types.Any()) throw new ArgumentException("types is empty.");

			// If at least one version is a class: merge classes
			if (types.OfType<CppClass>().Any()) {
				var classes = types.OfType<CppClass>().ToList();
				var baseTypes = classes
					.SelectMany(c => c.BaseClasses)
					.Distinct()
					.ToList();
				var minimumBaseTypes = baseTypes
					.Where(a => !baseTypes.Any(b => b.AncestorClasses.Contains(a)))
					.ToList();
				return new CppClass(classes.First().Name, minimumBaseTypes);
			}

			// Assume primitive type and go by precedence list
			var precedences = new[] {
				PrimitiveCppType.Bool,
				PrimitiveCppType.Function,
				PrimitiveCppType.String,
				PrimitiveCppType.Number,
				PrimitiveCppType.Void,
				PrimitiveCppType.Unknown
			};
			foreach (PrimitiveCppType precedentType in precedences) {
				if (types.Any(type => type == precedentType)) return precedentType;
			}

			throw new ArgumentException($"No primitive or class type specified for type {types.First().Name}.");
		}

		/// <summary>
		/// Returns a concrete type for the specified type draft
		/// </summary>
		private static ICppType ResolveTypeDraft(ICppTypeDraft typeDraft, Func<string, ICppType> resolveName) {
			if (typeDraft is CppClassDraft) {
				// Create concrete class
				CppClassDraft classDraft = (CppClassDraft) typeDraft;
				var baseTypes = classDraft.BaseTypeNames
					.Select(resolveName)
					.OfType<CppClass>();
				return new CppClass(classDraft.Name, baseTypes);
			}
			if (typeDraft is CppTypedefDraft) {
				// Determine what the typedef stands for
				var typedef = (CppTypedefDraft) typeDraft;
				var targetType = resolveName(typedef.TargetName);

				// Function-to-anything becomes function, ignoring the actual type
				if (typedef.IsFunction) return PrimitiveCppType.Function;

				// Pointer-to-number becomes string.
				// This guess is good enough for Lua interop scenarios.
				if (typedef.IsPointer && targetType == PrimitiveCppType.Number) return PrimitiveCppType.String;

				// Otherwise just use the typedef's target type
				return targetType;
			}
			throw UnexpectedValueException.Create(typeDraft);
		}
	}

}