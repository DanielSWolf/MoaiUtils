using Antlr4.Runtime;

namespace CppParser {

	public class TokenValue<TValue> : ITokenProvider {

		public TokenValue(TValue value, IToken token) {
			Value = value;
			Token = token;
		}

		public IToken Token { get; }
		public TValue Value { get; }

	}

	public static class TokenValue {

		public static TokenValue<TValue> Create<TValue>(TValue value, IToken token) {
			return new TokenValue<TValue>(value, token);
		}

	}

}