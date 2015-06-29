namespace CppParser.CodeIssues {

	public interface ICodeIssue {

		CodePosition Position { get; }
		string Message { get; }

	}

}