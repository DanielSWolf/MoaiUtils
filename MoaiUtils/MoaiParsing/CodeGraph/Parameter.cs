using MoaiUtils.MoaiParsing.CodeGraph.Types;

namespace MoaiUtils.MoaiParsing.CodeGraph {

	public abstract class Parameter {
		public string Name { get; set; }
		public string Description { get; set; }
		public IType Type { get; set; }

		public override string ToString() {
			return string.Format("{0} {1}", Type, Name);
		}
	}

}