using System;
using System.Collections.Generic;
using System.Linq;
using MoaiUtils.Tools;

namespace CppParser.CppSymbols {

	public class CppClass : ICppType {

		public CppClass(string name, IEnumerable<CppClass> baseClasses) {
			if (name == null) throw new ArgumentNullException(nameof(name));
			if (baseClasses == null) throw new ArgumentNullException(nameof(baseClasses));

			Name = name;
			BaseClasses = baseClasses.ToList();
		}

		public CppClass(string name, params CppClass[] baseClasses) : this(name, (IEnumerable<CppClass>) baseClasses) { }

		public string Name { get; }
		public IReadOnlyCollection<CppClass> BaseClasses { get; }

		public IEnumerable<CppClass> AncestorClasses => BaseClasses
			.SelectMany(baseType => baseType.AncestorClasses)
			.Distinct();

		public override string ToString() {
			return BaseClasses.Any()
				? $"class {Name} : {BaseClasses.Select(bc => bc.Name).Join(", ")}"
				: $"class {Name}";
		}

	}

}