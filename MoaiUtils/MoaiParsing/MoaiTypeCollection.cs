﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MoaiUtils.MoaiParsing.CodeGraph;
using MoaiUtils.Tools;
using MoreLinq;

namespace MoaiUtils.MoaiParsing {
    [Flags]
    public enum MatchMode {
        Strict = 0,
        FindSimilar = 1,
        FindSynonyms = 2
    }

    public class MoaiTypeCollection : IEnumerable<MoaiType> {
        private readonly Dictionary<string, MoaiType> typesByName = new Dictionary<string, MoaiType>();

        public MoaiTypeCollection(bool initializeWithPrimitives) {
            if (initializeWithPrimitives) {
                var primitiveTypeNames = new[] {
                    "nil", "boolean", "number", "string", "userdata", "function", "thread", "table"
                };
                foreach (string primitiveTypeName in primitiveTypeNames) {
                    GetOrCreate(primitiveTypeName, null).IsPrimitive = true;
                }
            }
        }

        public MoaiType GetOrCreate(string typeName, FilePosition documentationPosition) {
            MoaiType result = typesByName.ContainsKey(typeName)
                ? typesByName[typeName]
                : typesByName[typeName] = new MoaiType { Name = typeName };
            if (documentationPosition != null) {
                result.DocumentationReferences.Add(documentationPosition);
            }
            return result;
        }

        public MoaiType Find(string typeName, MatchMode matchMode, Predicate<MoaiType> allow = null) {
            if (allow == null) allow = type => true;

            if (typesByName.ContainsKey(typeName)) {
                MoaiType result = typesByName[typeName];
                if (allow(result)) return result;
            }
            if (matchMode.HasFlag(MatchMode.FindSynonyms)) {
                MoaiType result = GetBySynonym(typeName);
                if (result != null && allow(result)) return result;
            }
            if (matchMode.HasFlag(MatchMode.FindSimilar)) {
                MoaiType result = GetFuzzy(typeName, allow);
                if (result != null) return result;
            }
            return null;
        }

        private MoaiType GetFuzzy(string typeName, Predicate<MoaiType> allow) {
            // Find type with closest name
            MoaiType closestType = typesByName
                .Where(pair => allow(pair.Value))
                .Select(pair => pair.Value)
                .MinBy(type => Levenshtein.Distance(type.Name, typeName));

            // Return type only if its name is reasonably similar
            double similarity = Levenshtein.Similarity(closestType.Name, typeName);
            return (similarity >= 0.6) ? closestType : null;
        }

        private MoaiType GetBySynonym(string typeName) {
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
        
        IEnumerator<MoaiType> IEnumerable<MoaiType>.GetEnumerator() {
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