namespace CppParser.CodeIssues {

	[CodeIssueMessage("An assumption about the file structure within the source directory was wrong. {0}", "<detailed error description>")]
	public class UnexpectedFileStructureCodeIssue : CodeIssueBase {

		public UnexpectedFileStructureCodeIssue(string message) : base(null, message) {}

	}

}