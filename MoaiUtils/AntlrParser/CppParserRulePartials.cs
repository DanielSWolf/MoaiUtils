using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using CppParser.CppSymbols;

namespace CppParser {

	public partial class CppParser {

		public partial class ClassDefinitionContext : ICppClassDraft {

			public ClassDefinitionContext Context => this;

			public TokenValue<string> Name => typeSpecifier()?.TypeName ?? Id()?.Symbol.TextToTokenValue();
			string ICppTypeDraft.Name => Name.Value;

			public IReadOnlyCollection<TokenValue<string>> BaseTypeNames {
				get {
					return baseClause()?.typeSpecifier().Select(typeSpecifier => typeSpecifier.TypeName).ToArray()
						?? new TokenValue<string>[0];
				}
			}

			IReadOnlyCollection<string> ICppClassDraft.BaseTypeNames => BaseTypeNames.Select(name => name.Value).ToList();

			// Skip unnamed types, such as "enum { Foo, Bar };"
			public bool IntroducesSymbol => Name != null;

		}

		public partial class TypedefContext : ICppTypedefDraft {

			public TypedefContext Context => this;

			public TokenValue<string> Name => type().declarator().Name;
			string ICppTypeDraft.Name => Name.Value;

			public TokenValue<string> TargetName => type().typeSpecifier().TypeName;
			string ICppTypedefDraft.TargetName => TargetName.Value;

			public bool IsFunction => type().declarator().IsFunction;
			public bool IsPointer => type().declarator().IsPointer;

			// Skip redundant typedefs, such as "typedef struct lua_State lua_State;"
			public bool IntroducesSymbol => Name.Value != TargetName.Value;

		}

		public partial class DeclaratorContext {

			public TokenValue<string> Name => this.GetDescendant<NameDeclaratorContext>(includeSelf: true).Id()?.Symbol.TextToTokenValue();
			public bool IsFunction => this.GetDescendants<FunctionDeclaratorContext>(includeSelf: true).Any();
			public bool IsPointer => this.GetDescendants<PointerDeclaratorContext>(includeSelf: true).Any();

		}

		public partial class TypeSpecifierContext {

			public TokenValue<string> TypeName {
				get {
					IToken id = Id()?.Symbol;
					if (id != null) return id.TextToTokenValue();

					IToken intType = IntType().Symbol;
					return TokenValue.Create(intType.Text.Split(' ').Last(), intType);
				}
			}

		}

	}

}