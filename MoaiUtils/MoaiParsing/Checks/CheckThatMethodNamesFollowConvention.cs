using MoaiUtils.MoaiParsing.CodeGraph;

namespace MoaiUtils.MoaiParsing.Checks {

	public class CheckThatMethodNamesFollowConvention : CheckBase {

		public override void Run() {
			foreach (Method method in Methods) {
				// Check that @lua annotation sticks to convention
				if (!method.MethodPosition.NativeMethodName.StartsWith("_")) {
					Warnings.Add(method.MethodPosition, WarningType.UnexpectedValue,
						"Unexpected C++ method name '{0}'. By convention, the name of a Lua method implementation shold start with an underscore.",
						method.MethodPosition.NativeMethodName);
				}
				string expectedName = method.MethodPosition.NativeMethodName.Substring(1);
				if (method.Name != expectedName) {
					Warnings.Add(method.MethodPosition, WarningType.UnexpectedValue,
						"@lua annotation has unexpected value '{0}'. By convention expected '{1}'.",
						method.Name, expectedName);
				}
			}
		}

	}

}