using System;

namespace CppParser.CodeIssues {

	public abstract class CodeIssueBase : ICodeIssue {

		public CodePosition Position { get; }
		public string Message { get; }

		protected CodeIssueBase(CodePosition position, params object[] messageArgs) {
			Position = position;

			var messageAttribute = (CodeIssueMessageAttribute) Attribute.GetCustomAttribute(GetType(), typeof(CodeIssueMessageAttribute));
			if (messageAttribute == null) {
				throw new Exception($"Type {GetType()} does not have a {nameof(CodeIssueMessageAttribute)}.");
			}
			Message = messageAttribute.FormatMessage(messageArgs);
		}

	}

}