using System;
using System.Collections.Generic;
using System.Linq;
using CppParser.CodeIssues;
using CppParser.CppSymbols;
using MoaiUtils.Tools;

namespace CppParser {

	public class CppTypesExtractor : IProcessingStep {

		private readonly IReadOnlyList<CppParser.FileContext> cppParseTrees;

		private readonly List<ICodeIssue> codeIssues = new List<ICodeIssue>();

		private MultiValueDictionary<string, ICppTypeDraft> typeDrafts;
		private Dictionary<string, ICppType> types;

		public CppTypesExtractor(IReadOnlyList<CppParser.FileContext> cppParseTrees) {
			this.cppParseTrees = cppParseTrees;
		}

		public IReadOnlyCollection<ICodeIssue> CodeIssues => codeIssues;
		public IReadOnlyDictionary<string, ICppType> Types => types;

		public void Run(IProgress<double> progress) {
			// Collect type drafts (potentially multiple entries per name)
			typeDrafts = GetTypeDrafts(cppParseTrees);

			// Initialize type lookup with built-in types
			types = GetBuiltInTypes();

			// Decide on a single concrete (non-draft) type per name
			foreach (string typeName in typeDrafts.Keys) {
				ResolveTypeName(typeName, null);
			}
		}

		private static Dictionary<string, ICppType> GetBuiltInTypes() {
			var types = new Dictionary<string, ICppType> {
				// Primitive types
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
				["wchar_t"] = PrimitiveCppType.String,

				// STL
				["string"] = PrimitiveCppType.String,
				["map"] = new CppClass("map"),
				["set"] = new CppClass("set"),
				["vector"] = new CppClass("vector"),
				["list"] = new CppClass("list"),
				["iterator"] = new CppClass("iterator"),

				// Windows
				["in_addr"] = new CppClass("in_addr"),
				["sockaddr"] = new CppClass("sockaddr"),
				["hostent"] = new CppClass("hostent"),
				["SOCKET"] = PrimitiveCppType.Number
			};

			// Ultra-specific number types such as "uint_least32_t"
			foreach (string unsignedPrefix in new[] { "", "u" }) {
				foreach (string specifier in new[] { "", "_least", "_fast" }) {
					foreach (int width in new[] { 8, 16, 32, 64 }) {
						types[$"{unsignedPrefix}int{specifier}{width}_t"] = PrimitiveCppType.Number;
					}
				}
			}

			return types;
		}

		private static MultiValueDictionary<string, ICppTypeDraft> GetTypeDrafts(IReadOnlyList<CppParser.FileContext> fileContexts) {
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
			return typeDrafts;
		}

		private ICppType ResolveTypeName(string typeName, CodePosition reference) {
			if (types.ContainsKey(typeName)) {
				// Type has already been resolved
				return types[typeName];
			}

			if (typeDrafts.ContainsKey(typeName)) {
				// Type can be resolved
				IReadOnlyCollection<ICppTypeDraft> drafts = typeDrafts[typeName];
				var typeCandidates = drafts
					.Select(ResolveTypeDraft)
					.ToList();
				ICppType mergedType = MergeTypes(typeCandidates);
				return types[typeName] = mergedType;
			}

			// Type cannot be resolved
			codeIssues.Add(new UnknownTypeCodeIssue(reference, typeName));
			return types[typeName] = PrimitiveCppType.Unknown;
		}

		/// <summary>
		/// Returns a concrete type for the specified type draft
		/// </summary>
		private ICppType ResolveTypeDraft(ICppTypeDraft typeDraft) {
			if (typeDraft is CppClassDraft) {
				// Create concrete class
				CppClassDraft classDraft = (CppClassDraft) typeDraft;
				var baseTypes = classDraft.BaseTypeNames
					.Select(baseTypeName => ResolveTypeName(baseTypeName, classDraft.CodePosition))  // TODO: Get precise position
					.OfType<CppClass>();
				return new CppClass(classDraft.Name, baseTypes);
			}
			if (typeDraft is CppTypedefDraft) {
				// Determine what the typedef stands for
				var typedef = (CppTypedefDraft) typeDraft;
				var targetType = ResolveTypeName(typedef.TargetName, typedef.CodePosition); // TODO: Get precise position

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

	}

}