using System;
using System.Collections.Generic;
using System.Linq;

namespace MoaiUtils.Tools {

	public static class EnumerableExtensions {
		public static IOrderedEnumerable<T> OrderBy<T>(this IEnumerable<T> source, Func<T, T, int> compare) {
			return source.OrderBy(element => element, new InlineComparer<T>(compare));
		}

		public static IEnumerable<Tuple<T, T>> Pairwise<T>(this IEnumerable<T> elements) {
			return elements.Zip(elements.Skip(1), Tuple.Create);
		}

		public static IEnumerable<T> Subsequence<T>(this IEnumerable<T> elements, int start, int length) {
			return elements.Skip(start).Take(length);
		}

		private class InlineComparer<T> : IComparer<T> {
			private readonly Func<T, T, int> compare;

			public InlineComparer(Func<T, T, int> compare) {
				this.compare = compare;
			}

			public int Compare(T x, T y) {
				return compare(x, y);
			}
		}
	}

}