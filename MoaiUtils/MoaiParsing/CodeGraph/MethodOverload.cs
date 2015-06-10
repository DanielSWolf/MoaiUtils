using System.Collections.Generic;

namespace MoaiUtils.MoaiParsing.CodeGraph {

	public class MethodOverload {
		public MethodOverload() {
			InParameters = new List<InParameter>();
			OutParameters = new List<OutParameter>();
		}

		public Method OwningMethod { get; set; }
		public bool IsStatic { get; set; }
		public List<InParameter> InParameters { get; private set; }
		public List<OutParameter> OutParameters { get; private set; }
	}

}