namespace CppParser.CppSymbols {

	public class BuiltInCppClass : CppClass {

		public BuiltInCppClass(string name, params CppClass[] baseClasses)
			: base(null, name, baseClasses) {}

	}

}