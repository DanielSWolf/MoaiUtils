using System;

namespace MoaiUtils.Tools {

	public class UnexpectedValueException<T> : Exception {

		public UnexpectedValueException(T value)
			: base($"Unexpeced {typeof(T).Name} value: {(object) value ?? "null"}") { }

	}

	public static class UnexpectedValueException {

		public static UnexpectedValueException<T> Create<T>(T value) {
			return new UnexpectedValueException<T>(value);
		}

	}

}