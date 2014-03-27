using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MoaiUtils.MoaiParsing.CodeGraph.Types;
using MoaiUtils.Tools;
using MoreLinq;

namespace MoaiUtils.MoaiParsing {
    [Flags]
    public enum MatchMode {
        Strict = 0,
        FindSimilar = 1,
        FindSynonyms = 2
    }

    public class TypeCollection : IEnumerable<IType> {
        private readonly Dictionary<string, IType> typesByName = new Dictionary<string, IType>();

        public TypeCollection(bool includePrimitives, bool includeVariant) {
            if (includePrimitives) {
                var primitiveTypeNames = new[] {
                    "nil", "boolean", "number", "string", "userdata", "function", "thread", "table"
                };
                foreach (string primitiveTypeName in primitiveTypeNames) {
                    typesByName[primitiveTypeName] = new PrimitiveLuaType(primitiveTypeName);
                }
            }
            if (includeVariant) {
                typesByName["variant"] = Variant.Instance;
            }
        }

        public IType GetOrCreate(string typeName, FilePosition documentationPosition) {
            if (typeName == "...") {
                return new Ellipsis(Variant.Instance);
            }
            if (typeName.EndsWith("...")) {
                return new Ellipsis(GetOrCreate(typeName.Substring(0, typeName.Length - 3), documentationPosition));
            }

            // Let's assume that any unknown type is a class.
            // Usually that's correct, and if the name is a typo and really refers to some other type, it doesn't matter.
            IType result = typesByName.ContainsKey(typeName)
                ? typesByName[typeName]
                : typesByName[typeName] = new MoaiClass { Name = typeName };
            if (documentationPosition != null && result is IDocumentationReferenceAware) {
                ((IDocumentationReferenceAware) result).AddDocumentationReference(documentationPosition);
            }
            return result;
        }

        public IType Find(string typeName, MatchMode matchMode = MatchMode.Strict, Predicate<IType> allow = null) {
            if (allow == null) allow = type => true;

            if (typesByName.ContainsKey(typeName)) {
                IType result = typesByName[typeName];
                if (allow(result)) return result;
            }
            if (matchMode.HasFlag(MatchMode.FindSynonyms)) {
                IType result = GetBySynonym(typeName);
                if (result != null && allow(result)) return result;
            }
            if (matchMode.HasFlag(MatchMode.FindSimilar)) {
                IType result = GetFuzzy(typeName, allow);
                if (result != null) return result;
            }
            return null;
        }

        private IType GetFuzzy(string typeName, Predicate<IType> allow) {
            // Find type with closest name
            IType closestType = typesByName
                .Where(pair => allow(pair.Value))
                .Select(pair => pair.Value)
                .MinBy(type => Levenshtein.Distance(type.Name, typeName));

            // Return type only if its name is reasonably similar
            int distance = Levenshtein.Distance(closestType.Name, typeName);
            return (distance <= 1) ? closestType : null;
        }

        private IType GetBySynonym(string typeName) {
            if (booleanSynonyms.Contains(typeName)) {
                return typesByName["boolean"];
            }
            if (numberSynonyms.Contains(typeName)) {
                return typesByName["number"];
            }
            if (stringSynonyms.Contains(typeName)) {
                return typesByName["string"];
            }
            return null;
        }

        IEnumerator<IType> IEnumerable<IType>.GetEnumerator() {
            return typesByName.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return typesByName.Values.GetEnumerator();
        }

        private readonly HashSet<string> booleanSynonyms = new HashSet<string> {
            "bool", "cpBool"
        };

        private readonly HashSet<string> numberSynonyms = new HashSet<string> {
            "num", "int", "integer", "double", "float",
            "u8", "u16", "u32", "u64", "s8", "s16", "s32", "s64", "size_t",
            "cpFloat", "cpCollisionType", "cpGroup", "cpLayers", "cpTimestamp"
        };

        private readonly HashSet<string> stringSynonyms = new HashSet<string> {
            "cc8*"
        };

    }
}