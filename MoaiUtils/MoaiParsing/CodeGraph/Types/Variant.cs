namespace MoaiUtils.MoaiParsing.CodeGraph.Types {

	public class Variant : IType {
		public static readonly Variant Instance = new Variant();

		private Variant() {}

		public string Name {
			get { return "variant"; }
		}

		public string Description {
			get { return "This can be any type."; }
		}

		public string Signature {
			get { return Name; }
		}

		public bool Exists {
			get { return true; }
		}

		public override string ToString() {
			return Signature;
		}

	}

}