using System;

namespace CppParser.CodeIssues {

	[CodeIssueMessage("An unexpected error has occurred. The analysis results may be incomplete. {0}", "<detailed error description>")]
	public class InternalErrorCodeIssue : CodeIssueBase {

		public InternalErrorCodeIssue(CodePosition position, Exception exception) : base(position, exception) {}

	}

}