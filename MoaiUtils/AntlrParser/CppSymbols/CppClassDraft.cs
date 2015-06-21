using System;
using System.Collections.Generic;
using System.Linq;

namespace CppParser.CppSymbols {

	public class CppClassDraft : ICppTypeDraft {

		public CppClassDraft(CodePosition codePosition, string name, IEnumerable<string> baseTypeNames) {
			if (codePosition == null) throw new ArgumentNullException(nameof(codePosition));
			if (name == null) throw new ArgumentNullException(nameof(name));
			if (baseTypeNames == null) throw new ArgumentNullException(nameof(baseTypeNames));

			CodePosition = codePosition;
			Name = name;
			BaseTypeNames = baseTypeNames.ToList();
		}

		public CodePosition CodePosition { get; }
		public string Name { get; }
		public IReadOnlyCollection<string> BaseTypeNames { get; }

	}

}