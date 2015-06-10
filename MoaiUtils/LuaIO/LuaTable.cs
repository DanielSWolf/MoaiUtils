using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace MoaiUtils.LuaIO {

	/// <summary>
	/// This class imitates the behavior of a table in Lua.
	/// In addition, it guarantees that items are kept in the same order in which they were added.
	/// While not technically necessary, this allows generated Lua files to be structured meaningfully.
	/// </summary>
	public class LuaTable : IDictionary<object, object> {
		private readonly OrderedDictionary elements = new OrderedDictionary();
		private int nextAutoIndex = 1;

		public int Count {
			get { return elements.Count; }
		}

		public void Clear() {
			elements.Clear();
		}

		public bool ContainsKey(object key) {
			return elements.Contains(key);
		}

		public object this[object key] {
			get {
				if (!ContainsKey(key)) {
					throw new InvalidOperationException(string.Format("There is no element with key [{0}].", key));
				}
				return elements[key];
			}
			set {
				KeyValuePair<object, object>? pair = CleanKeyValuePair(key, value);
				if (pair != null) {
					elements[pair.Value.Key] = pair.Value.Value;
				}
			}
		}

		public void Add(object key, object value) {
			if (ContainsKey(key)) {
				throw new InvalidOperationException(string.Format("There already is an element with key [{0}].", key));
			}
			this[key] = value;
		}

		public void Add(object value) {
			if (value is LuaComment) {
				Add(new LuaCommentKey(), value);
			} else {
				Add((double) nextAutoIndex++, value);
			}
		}

		public bool Remove(object key) {
			if (ContainsKey(key)) {
				elements.Remove(key);
				return true;
			}
			return false;
		}

		public ICollection<object> Keys {
			get { return new ReadOnlyCollection<object>(elements.Keys.Cast<object>().ToList()); }
		}

		public ICollection<object> Values {
			get { return new ReadOnlyCollection<object>(elements.Values.Cast<object>().ToList()); }
		}

		public IEnumerable<object> SequencePart {
			get {
				for (int index = 1;; index++) {
					if (ContainsKey(index)) {
						yield return elements[index];
					} else {
						yield break;
					}
				}
			}
		}

		private KeyValuePair<object, object>? CleanKeyValuePair(object key, object value) {
			// Key must be set.
			if (!(key is bool || key is double || key is int || key is string || key is LuaTable || key is LuaFunction || key is LuaCommentKey)) {
				throw new ArgumentException(string.Format(
					"Unsupported key [{0}]. Keys must be of type bool, double/int, string, LuaTable, LuaFunction, or LuaCommentKey.",
					key ?? "null"));
			}

			// Value should be set - null value means ignore this.
			if (value == null) {
				return null;
			}
			if (!(value is bool || value is double || value is int || value is string || value is LuaTable || value is LuaFunction || value is LuaComment)) {
				throw new ArgumentException(string.Format(
					"Unsupported value [{0}]. Values must be of type bool, double/int, string, LuaTable, LuaFunction, or LuaComment.",
					value));
			}

			// At this point, key and value are both set. Check that they are compatible.
			if ((key is LuaCommentKey) != (value is LuaComment)) {
				throw new ArgumentException("LuaCommentKey and LuaComment must be used together.");
			}

			// Apply trivial conversions
			if (key is int) key = (double) (int) key;
			if (value is int) value = (double) (int) value;

			return new KeyValuePair<object, object>(key, value);
		}

		#region IDictionary overhead

		public IEnumerator<KeyValuePair<object, object>> GetEnumerator() {
			return elements
				.Cast<DictionaryEntry>()
				.Select(entry => new KeyValuePair<object, object>(entry.Key, entry.Value))
				.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		public void Add(KeyValuePair<object, object> item) {
			Add(item.Key, item.Value);
		}

		bool ICollection<KeyValuePair<object, object>>.Contains(KeyValuePair<object, object> item) {
			throw new NotSupportedException();
		}

		void ICollection<KeyValuePair<object, object>>.CopyTo(KeyValuePair<object, object>[] array, int arrayIndex) {
			int targetIndex = arrayIndex;
			foreach (DictionaryEntry entry in elements) {
				array[targetIndex] = new KeyValuePair<object, object>(entry.Key, entry.Value);
				targetIndex++;
			}
		}

		bool ICollection<KeyValuePair<object, object>>.Remove(KeyValuePair<object, object> item) {
			throw new NotSupportedException();
		}

		bool IDictionary<object, object>.TryGetValue(object key, out object value) {
			if (ContainsKey(key)) {
				value = elements[key];
				return true;
			}
			value = null;
			return false;
		}

		bool ICollection<KeyValuePair<object, object>>.IsReadOnly {
			get { return false; }
		}

		#endregion
	}

	public static class LuaTableExtensions {
		public static LuaTable ToLuaList(this IEnumerable elements) {
			var result = new LuaTable();
			foreach (object element in elements) {
				result.Add(element);
			}
			return result;
		}

		public static LuaTable ToLuaDictionary<TElement>(this IEnumerable<TElement> elements, Func<TElement, object> keySelector) {
			var result = new LuaTable();
			foreach (TElement element in elements) {
				result.Add(keySelector(element), element);
			}
			return result;
		}

		public static LuaTable ToLuaDictionary<TElement>(this IEnumerable<TElement> elements, Func<TElement, object> keySelector, Func<TElement, object> valueSelector) {
			var result = new LuaTable();
			foreach (TElement element in elements) {
				result.Add(keySelector(element), valueSelector(element));
			}
			return result;
		}

		public static LuaTable ToLuaDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> pairs) {
			var result = new LuaTable();
			foreach (var pair in pairs) {
				result.Add(pair.Key, pair.Value);
			}
			return result;
		}
	}

}