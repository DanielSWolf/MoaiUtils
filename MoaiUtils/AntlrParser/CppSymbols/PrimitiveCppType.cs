namespace CppParser.CppSymbols {

	public class PrimitiveCppType : ICppType {

		public static readonly PrimitiveCppType Void = new PrimitiveCppType("void");
		public static readonly PrimitiveCppType Bool = new PrimitiveCppType("bool");
		public static readonly PrimitiveCppType Number = new PrimitiveCppType("number");
		public static readonly PrimitiveCppType String = new PrimitiveCppType("string");
		public static readonly PrimitiveCppType Function = new PrimitiveCppType("function");
		public static readonly PrimitiveCppType Unknown = new PrimitiveCppType("<unknown>");

		public string Name { get; }

		private PrimitiveCppType(string name) {
			Name = name;
		}

		public override string ToString() {
			return Name;
		}

	}

}