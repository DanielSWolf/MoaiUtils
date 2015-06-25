using System;
using System.Collections.Generic;
using System.Linq;
using CppParser.CppSymbols;
using MoaiUtils.Tools;

namespace CppParser {

	public class TypesExtractor : IProcessingStep {

		private readonly IReadOnlyList<CppParser.FileContext> cppParseTrees;
		private readonly IReadOnlyList<LuaParser.ChunkContext> luaParseTrees;

		public TypesExtractor(IReadOnlyList<CppParser.FileContext> cppParseTrees, IReadOnlyList<LuaParser.ChunkContext> luaParseTrees) {
			this.cppParseTrees = cppParseTrees;
			this.luaParseTrees = luaParseTrees;
		}

		public void Run(IProgress<double> progress) {
			Dictionary<string, ICppType> cppTypes = GetCppTypes(cppParseTrees);
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