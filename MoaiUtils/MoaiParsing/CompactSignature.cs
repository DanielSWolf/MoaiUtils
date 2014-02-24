using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MoaiUtils.Tools;
using MoreLinq;

namespace MoaiUtils.MoaiParsing {
    /// <summary>
    /// Utility class to create a shorthand signature that represents multiple overloads
    /// (something like "number radius | (number innerRadius, number outerRadius)")
    /// See http://stackoverflow.com/a/20712653/52041
    /// </summary>
    public static class CompactSignature {
        public static ISignature FromOverloads(Parameter[][] overloads) {
            // Make sure there are no duplicate parameters
            foreach (Parameter[] parameters in overloads) {
                var duplicateParameters = parameters
                    .GroupBy(parameter => parameter)
                    .Where(duplicateGroup => duplicateGroup.Count() > 1)
                    .Select(duplicateGroup => duplicateGroup.First());
                if (duplicateParameters.Any()) {
                    throw new ApplicationException(string.Format(
                        "One or more duplicate parameters: {0}",
                        duplicateParameters.Select(param => param.ToString()).Join(", ")));
                }
            }

            // Get all occurring parameters in order
            Parameter[] allParameters = GetAllParameters(overloads);

            // Convert overloads to bool arrays
            var boolOverloads = overloads
                .Select(usedParameters => new Overload(allParameters.Select(usedParameters.Contains).ToArray()))
                .ToArray();

            return FromOverloads(allParameters, boolOverloads);
        }

        private static Parameter[] GetAllParameters(Parameter[][] overloads) {
            Parameter[] allParameters = overloads
                .SelectMany(overload => overload)
                .Distinct()
                .OrderBy((a, b) => {
                    if (a == b) return 0;
                    bool aBeforeB = overloads.Any(overload => overload.SkipWhile(p => p != a).Contains(b));
                    bool bBeforeA = overloads.Any(overload => overload.SkipWhile(p => p != b).Contains(a));
                    return (aBeforeB) ? -1 : bBeforeA ? 1 : 0;
                })
                .ToArray();

            // Make sure each overload observes this order
            foreach (Parameter[] actualParameters in overloads) {
                Parameter[] expectedParameters = allParameters.Intersect(actualParameters).ToArray();
                if (!expectedParameters.SequenceEqual(actualParameters)) {
                    throw new ApplicationException(string.Format(
                        "Inconsistent parameter order. Expected: {0}; actual: {1}",
                        expectedParameters.Select(param => param.ToString()).Join(", "),
                        actualParameters.Select(param => param.ToString()).Join(", ")));
                }
            }

            return allParameters;
        }

        private static ISignature FromOverloads(Parameter[] allParameters, Overload[] overloads) {
            // Make sure all overloads are distinct
            overloads = overloads.Distinct().ToArray();

            // If there is only one overload: use a Sequence
            if (overloads.Length == 1) {
                return new Sequence(overloads.Single().ToParameterList(allParameters));
            }

            // If there is an empty overload: use an Option and recurse
            Overload emptyOverload = overloads.FirstOrDefault(overload => overload.All(flag => !flag));
            if (emptyOverload != null) {
                return new Option {
                    Value = FromOverloads(allParameters, overloads.Except(new[] { emptyOverload }).ToArray())
                };
            }

            // Find contiguous areas of parameters that are independent from the rest.
            // Use a Sequence containing the constant parts and recurse for the independent areas.
            Interval[] independentAreas = GetIndependentAreas(new HashSet<Overload>(overloads));
            if (independentAreas.Any()) {
                var sequence = new Sequence();
                // Pad independent areas with zero-length dummy intervals
                var pairs = independentAreas.Prepend(new Interval()).Concat(new Interval()).Pairwise();
                foreach (Tuple<Interval, Interval> pair in pairs) {
                    if (pair.Item1.Length > 0) {
                        sequence.Add(FromArea(allParameters, overloads, pair.Item1));
                    }
                    for (int i = pair.Item1.End; i < pair.Item2.Start; i++) {
                        sequence.Add(allParameters[i]);
                    }
                }
                return sequence;
            }

            // If there are no independent areas:
            // Find all partitions of the overloads and use the one that yields the shortest representation.
            // Ignore the trivial one-group partition as this will result in an endless recursion.
            IEnumerable<Overload[][]> overloadPartitions = Partitioning.GetAllPartitions(overloads)
                .Where(partition => partition.Length > 1);
            IEnumerable<Choice> possibleRepresentations = overloadPartitions
                .Select(overloadGroups => new Choice(overloadGroups.Select(overloadGroup => FromOverloads(allParameters, overloadGroup.ToArray()))));
            return possibleRepresentations.MinBy(representation => representation.ParameterCount);
        }

        private static ISignature FromArea(Parameter[] allParameters, Overload[] overloads, Interval area) {
            Parameter[] trimmedParameters = allParameters
                .Subsequence(area.Start, area.Length)
                .ToArray();
            Overload[] trimmedOverloads = overloads
                .Select(overload => new Overload(overload.Subsequence(area.Start, area.Length)))
                .ToArray();
            return FromOverloads(trimmedParameters, trimmedOverloads);
        }

        private static Interval[] GetIndependentAreas(HashSet<Overload> overloads) {
            var independentAreas = new List<Interval>();
            int index = 0;
            var totalArea = new Interval { Start = 0, Length = overloads.First().Count };
            while (index < totalArea.Length) {
                bool isConstant = overloads.All(overload => overload[index] == overloads.First()[index]);
                if (isConstant) {
                    index++;
                    continue;
                }

                var area = new Interval { Start = index, Length = 1 };
                while (!IsIndependent(area, overloads)) {
                    area.Length++;
                }
                if (!area.Equals(totalArea)) {
                    independentAreas.Add(area);
                }
                index += area.Length;
            }

            return independentAreas.ToArray();
        }

        private static bool IsIndependent(Interval area, HashSet<Overload> overloads) {
            foreach (Overload basisOverload in overloads) {
                Overload generatedOverload = new Overload(basisOverload);
                foreach (Overload areaOverload in overloads) {
                    areaOverload.CopyTo(generatedOverload, area.Start, area.Start, area.Length);
                    if (!overloads.Contains(generatedOverload)) {
                        return false;
                    }
                }
            }
            return true;
        }

        /// Slim wrapper around a bool array.
        /// Each boolean indicates whether the corresponding parameter is present in the overload.
        private class Overload : IEnumerable<bool> {
            private readonly bool[] values;

            public Overload(IEnumerable<bool> list) {
                values = list.ToArray();
            }

            public IEnumerator<bool> GetEnumerator() {
                return values.Cast<bool>().GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() {
                return GetEnumerator();
            }

            public int Count {
                get { return values.Length; }
            }

            public bool this[int index] {
                get { return values[index]; }
                set { values[index] = value; }
            }

            public void CopyTo(Overload other, int sourceIndex, int targetIndex, int count) {
                for (int i = 0; i < count; i++) {
                    other[targetIndex + i] = this[targetIndex + i];
                }
            }

            public IList<Parameter> ToParameterList(Parameter[] allParameters) {
                if (allParameters.Length != Count) {
                    throw new ArgumentException("Parameter count does not match.");
                }
                var result = new List<Parameter>();
                for (int i = 0; i < Count; i++) {
                    if (this[i]) {
                        result.Add(allParameters[i]);
                    }
                }
                return result;
            }

            #region Equality members

            protected bool Equals(Overload other) {
                return this.SequenceEqual(other);
            }

            public override bool Equals(object obj) {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Equals((Overload) obj);
            }

            public override int GetHashCode() {
                return this.Aggregate(0, (current, flag) => (current << 1) | (flag ? 1 : 0));
            }

            public static bool operator ==(Overload left, Overload right) {
                return Equals(left, right);
            }

            public static bool operator !=(Overload left, Overload right) {
                return !Equals(left, right);
            }

            #endregion
        }

        private struct Interval {
            public int Start { get; set; }
            public int Length { get; set; }
            public int End { get { return Start + Length; } }
        }
    }

    public interface ISignature {
        string ToString(SignatureGrouping grouping);
        int ParameterCount { get; }
    }

    public class Parameter : ISignature {
        public string Type { get; set; }
        public string Name { get; set; }
        public bool ShowName { get; set; }

        public string ToString(SignatureGrouping grouping) {
            string result = ShowName && Name != null
                ? string.Format("{0} {1}", Type, Name)
                : Type;
            return grouping == SignatureGrouping.Parentheses
                ? result.Enclose("(", ")")
                : result;
        }

        public override string ToString() {
            return String.Format("{0} {1}", Type, Name);
        }

        public int ParameterCount {
            get { return 1; }
        }

        #region Equality members

        protected bool Equals(Parameter other) {
            return string.Equals(Type, other.Type) && string.Equals(Name, other.Name);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Parameter) obj);
        }

        public override int GetHashCode() {
            unchecked {
                return ((Type != null ? Type.GetHashCode() : 0) * 397) ^ (Name != null ? Name.GetHashCode() : 0);
            }
        }

        #endregion
    }

    public class Sequence : List<ISignature>, ISignature {
        public Sequence() { }
        public Sequence(IEnumerable<ISignature> collection) : base(collection) { }

        public string ToString(SignatureGrouping grouping) {
            if (Count == 1) {
                return this.Single().ToString(grouping);
            }

            string result = this
                .Select(signature => signature.ToString(SignatureGrouping.Any))
                .Join(", ");
            return grouping == SignatureGrouping.None
                ? result
                : result.Enclose("(", ")");
        }

        public int ParameterCount {
            get { return this.Sum(signature => signature.ParameterCount); }
        }
    }

    public class Choice : List<ISignature>, ISignature {
        public Choice() { }
        public Choice(IEnumerable<ISignature> collection) : base(collection) { }

        public string ToString(SignatureGrouping grouping) {
            if (Count == 1) {
                return this.Single().ToString(grouping);
            }

            string result = this
                .Select(signature => signature.ToString(SignatureGrouping.Any))
                .Join(" | ");
            return grouping == SignatureGrouping.None
                ? result
                : result.Enclose("(", ")");
        }

        public int ParameterCount {
            get { return this.Sum(signature => signature.ParameterCount); }
        }
    }

    public class Option : ISignature {
        public ISignature Value { get; set; }

        public string ToString(SignatureGrouping grouping) {
            string result = Value.ToString(SignatureGrouping.None);
            return grouping == SignatureGrouping.Parentheses
                ? result.Enclose("(", ")")
                : result.Enclose("[", "]");
        }

        public int ParameterCount {
            get { return Value.ParameterCount; }
        }
    }

    public enum SignatureGrouping {
        None,
        Any,
        Parentheses
    }
}