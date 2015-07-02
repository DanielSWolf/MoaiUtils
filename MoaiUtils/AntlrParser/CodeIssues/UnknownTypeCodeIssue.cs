namespace CppParser.CodeIssues {

	[CodeIssueMessage("Reference to type '{0}', which doesn't seem to be defined in the Moai sources.", "<type name>")]
	public class UnknownTypeCodeIssue : CodeIssueBase {

		public UnknownTypeCodeIssue(CodePosition position, string typeName) : base(position, typeName) {}

	}

}