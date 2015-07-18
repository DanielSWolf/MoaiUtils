using Antlr4.Runtime;

namespace CppParser {

	public interface IContextProvider<out TContext>
		where TContext : ParserRuleContext {

		TContext Context { get; }

	}

}