lexer grammar MoaiCppLexer ;

// Operators

Scope : '::' ;

LeftParen : '(' ;
RightParen : ')' ;
LeftBracket : '[' ;
RightBracket : ']' ;
LeftBrace : '{' ;
RightBrace : '}' ;

Less : '<' ;
LessEqual : '<=' ;
Greater : '>' ;
GreaterEqual : '>=' ;
LeftShift : '<<' ;
RightShift : '>>' ;

Plus : '+' ;
PlusPlus : '++' ;
Minus : '-' ;
MinusMinus : '--' ;

Star : '*' ;
Div : '/' ;
Mod : '%' ;
And : '&' ;
Or : '|' ;
AndAnd : '&&' ;
OrOr : '||' ;

Caret : '^' ;
Not : '!' ;
Tilde : '~' ;
Question : '?' ;
Colon : ':' ;
Semi : ';' ;
Comma : ',' ;
Assign : '=' ;

StarAssign : '*=' ;
DivAssign : '/=' ;
ModAssign : '%=' ;
PlusAssign : '+=' ;
MinusAssign : '-=' ;
LeftShiftAssign : '<<=' ;
RightShiftAssign : '>>=' ;
AndAssign : '&=' ;
XorAssign : '^=' ;
OrAssign : '|=' ;

Equal : '==' ;
NotEqual : '!=' ;
Arrow : '->' ;
Dot : '.' ;
Ellipsis : '...' ;

// Keywords (the few we care about)
Class : 'class' ;
Struct : 'struct' ;
Union : 'union' ;
Enum : 'enum' ;
TypeDef : 'typedef' ;
Template : 'template' ;
TypeName : 'typename' ;
Namespace : 'namespace' ;
Using : 'using' ;

// Literals

IntLiteral : ([0-9]+ | '0' [xX] HexDigit+ ) [uUlL]* ;

FloatLiteral
	: ([0-9]* '.' [0-9]+ | [0-9]+ '.') ([eE] [-+]? [0-9]+)? [lLfF]?
	| [0-9]+ [eE] [-+]? [0-9]+ [lLfF]? ;

CharLiteral : 'L'? ['] (~['\\\r\n] | EscapeSequence)* ['] ;

StringLiteral : ('@' | 'L')? '"' (~["\\\r\n] | EscapeSequence)* '"' ; // '@' is Objective C

BoolLiteral : 'true' | 'false' ;

fragment EscapeSequence
	: '\\' ['"?abfnrtv\\] // simple
	| '\\' [0-7]([0-7]([0-7])?)? // octal
	| '\\x' HexDigit+ // hexadecimal
	| '\\u' HexQuad | '\\U' HexQuad HexQuad ; // Unicode

fragment HexDigit : [0-9a-fA-F] ;
fragment HexQuad : HexDigit HexDigit HexDigit HexDigit ;

// Integer types

IntType : (TypeModifier Whitespace (TypeModifier Whitespace)*)? ('short' | 'long' | 'int' | 'char') ;
fragment TypeModifier : 'short' | 'long' | 'signed' | 'unsigned' ;

// Comments, whitespace etc.

Modifier
	: ('const' | 'mutable' | 'volatile'							// type specifiers
	| 'register' | 'static' | 'extern'							// storage duration specifiers
	| 'explicit' | 'inline' | 'virtual' | 'override' | 'final'	// specifiers
	| '__' [a-zA-Z0-9_]*										// compiler extensions
	) -> skip;
AccessSpecifier : ('private' | 'protected' | 'public') ':'? -> skip ;
Preproc : '#' ~[\r\n]* -> skip ;
LineComment : '//' ~[\r\n]* -> skip ;
BlockComment : '/*' .*? '*/' -> skip ;
Whitespace : [ \t\r\n]+ -> skip ;
ObjectiveCDirective
	: ('@'
	| ('@interface' | '@implementation' | '@protocol') .*? '@end'
	) -> skip ;
LinkageSpecification : 'extern' Whitespace? StringLiteral -> skip ;
CommonMacro // That's the price for not implementing a full preprocessor :-(
	: ( 'CALLBACK' | 'WINAPI' | 'JNICALL' | 'JNIEXPORT' | 'AKU_API' | 'F_CALLBACK'
	| 'SUPPRESS_EMPTY_FILE_WARNING'
	) -> skip ;

// Misc

Id
	: [a-zA-Z_] [a-zA-Z0-9_]*
	| 'operator' Whitespace ~[ \t\r\n(]+ (Whitespace? '[' Whitespace? ']')? ;
