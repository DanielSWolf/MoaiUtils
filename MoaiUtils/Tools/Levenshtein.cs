using System;

namespace MoaiUtils.Tools {

	public static class Levenshtein {

		/// <summary>
		/// Computes the Levenshtein distance between two strings.
		/// The smaller the result, the more similar the values.
		/// Adapted from http://en.wikipedia.org/wiki/Levenshtein_distance#Iterative_with_two_matrix_rows
		/// </summary>
		public static int Distance(string s, string t) {
			// Handle degenerate cases
			if (s == t) return 0;
			if (s.Length == 0) return t.Length;
			if (t.Length == 0) return s.Length;

			// Create two work vectors of integer distances
			int[] v0 = new int[t.Length + 1];
			int[] v1 = new int[t.Length + 1];

			// Initialize v0 (the previous row of distances).
			// This row is A[0][i]: edit distance for an empty s.
			// The distance is just the number of characters to delete from t
			for (int i = 0; i < v0.Length; i++) {
				v0[i] = i;
			}

			for (int i = 0; i < s.Length; i++) {
				// Calculate v1 (current row distances) from the previous row v0

				// First element of v1 is A[i+1][0].
				// Edit distance is delete (i+1) chars from s to match empty t
				v1[0] = i + 1;

				// Use formula to fill in the rest of the row
				for (int j = 0; j < t.Length; j++) {
					var cost = (s[i] == t[j]) ? 0 : 1;
					v1[j + 1] = Math.Min(v1[j] + 1, Math.Min(v0[j + 1] + 1, v0[j] + cost));
				}

				// Swap v1 (current row) and v0 (previous row) for next iteration
				int[] vTemp = v0;
				v0 = v1;
				v1 = vTemp;
			}

			// The vectors where swapped one last time at the end of the last loop,
			// that is why the result is now in v0 rather than in v1.
			return v0[t.Length];
		}

		/// <summary>
		/// Returns the similarity between two strings. 0 = completely different .. 1 = identical.
		/// </summary>
		public static double Similarity(string s, string t) {
			int maxLength = Math.Max(s.Length, t.Length);
			if (maxLength == 0) return 1.0;

			return 1 - ((double) Distance(s, t)/maxLength);
		}

	}

}