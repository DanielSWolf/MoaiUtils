using System;

namespace CppParser.CodeIssues {

	[AttributeUsage(AttributeTargets.Class, Inherited = false)]
	public class CodeIssueMessageAttribute : Attribute {

		public string MessageFormat { get; }
		public object[] DocArgs { get; }

		public CodeIssueMessageAttribute(string messageFormat, params object[] docArgs) {
			MessageFormat = messageFormat;
			DocArgs = docArgs;
		}

		public string FormatMessage(params object[] args) {
			return string.Format(MessageFormat, args);
		}

		public string DocMessage => string.Format(MessageFormat, DocArgs);

	}

}