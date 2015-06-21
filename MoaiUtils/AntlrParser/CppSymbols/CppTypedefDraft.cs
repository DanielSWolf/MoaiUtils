using System;

namespace CppParser.CppSymbols {

	public class CppTypedefDraft : ICppTypeDraft {

		public CppTypedefDraft(CodePosition codePosition, string name, string targetName, bool isFunction = false, bool isPointer = false) {
			if (name == null) throw new ArgumentNullException(nameof(name));
			if (targetName == null) throw new ArgumentNullException(nameof(targetName));

			CodePosition = codePosition;
			Name = name;
			TargetName = targetName;
			IsFunction = isFunction;
			IsPointer = isPointer;
		}

		public CodePosition CodePosition { get; }
		public string Name { get; }
		public string TargetName { get; private set; }
		public bool IsFunction { get; private set; }
		public bool IsPointer { get; private set; }

	}

}