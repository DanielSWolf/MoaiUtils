using System.Collections.Generic;
using System.Linq;
using CppParser.CppSymbols;

namespace CppParser {

	public partial class CppParser {

		public partial class FileContext {

			public CodePosition CodePosition => this.GetCodePosition();

		}

		public partial class ClassDefinitionContext {

			public string TypeName => typeSpecifier()?.TypeName ?? Id()?.GetText();

			public IEnumerable<string> BaseTypeNames {
				get {
					return baseClause()?.typeSpecifier().Select(typeSpecifier => typeSpecifier.TypeName)
						?? Enumerable.Empty<string>();
				}
			}

		}

		public partial class DeclaratorContext {

			public string Name => this.GetDescendant<NameDeclaratorContext>(includeSelf: true).Id()?.GetText();
			public bool IsFunction => this.GetDescendants<FunctionDeclaratorContext>(includeSelf: true).Any();
			public bool IsPointer => this.GetDescendants<PointerDeclaratorContext>(includeSelf: true).Any();

		}

		public partial class TypeContext {

			public CppTypedefDraft TypedefDraft {
				get {
					string name = declarator().Name;
					string targetName = typeSpecifier().TypeName;
					return new CppTypedefDraft(this.GetCodePosition(), name, targetName, declarator().IsFunction, declarator().IsPointer);
				}
			}

		}

		public partial class TypeSpecifierContext {

			public string TypeName => Id()?.GetText()
				?? IntType().GetText().Split(' ').Last();

		}

	}

}