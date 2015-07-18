using Antlr4.Runtime;

namespace CppParser {

	public class ContextValue<TValue, TContext> : IContextProvider<TContext>
		where TContext : ParserRuleContext {

		public ContextValue(TValue value, TContext context) {
			Value = value;
			Context = context;
		}

		public TContext Context { get; }
		public TValue Value { get; }

	}

	public static class ContextValue {

		public static ContextValue<TValue, TContext> Create<TValue, TContext>(TValue value, TContext context)
			where TContext : ParserRuleContext {
			
			return new ContextValue<TValue, TContext>(value, context);
		}

	}

}