namespace CppParser.CodeIssues {

	[CodeIssueMessage("Error parsing source code file: {0}", "<detailed information>")]
	public class ParsingErrorCodeIssue : CodeIssueBase {

		public ParsingErrorCodeIssue(CodePosition position, string message) : base(position, message) {}

	}

}