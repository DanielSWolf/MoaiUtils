parser grammar CppParser;

options { tokenVocab = CppLexer; }

file : topLevelStatement* EOF ;

topLevelStatement
	: declaration
	| usingDirective
	| typeDefinition
	| functionDefinition
	| constructorDefinition
	| destructorDefinition
	| '{' topLevelStatement* '}'
	| Namespace Id? '{' topLevelStatement* '}'
	| ';' ;

declaration :
	('template' templateParamsBlock)?
	(typeSpecifier | 'class' | 'struct' | 'union' | 'enum') declarator ('=' expression)? ';' ;

typeDefinition
	: 'typedef' ('class' | 'struct' | 'union' | 'enum')? type ';'
	# Typedef
	|
	classDocBlock?
	'typedef'? ('template' templateParamsBlock)?
	('class' | 'struct' | 'union' | 'enum') (typeSpecifier baseClause?)? bracesBlock Id? ';'
	# ClassDefinition
	;

usingDirective
	: 'using' 'namespace' .*? ';' ;

functionDefinition :
	functionDocBlock?
	('template' templateParamsBlock)?
	type bracesBlock ;

baseClause
	: ':' typeSpecifier (',' typeSpecifier)* ;

constructorDefinition :
	('template' templateParamsBlock)?
	typeSpecifier '::' Id parensBlock memberInitializerList? bracesBlock ;

destructorDefinition
	: typeSpecifier '::' '~' Id parensBlock bracesBlock ;

memberInitializerList
	: ':' memberInitializer (',' memberInitializer)* ;

memberInitializer
	: (Id | typeSpecifier) '(' (expression (',' expression)*)? ')' ;

declarator
	: '(' declarator ')'							# DeclaratorGroup
	| declarator bracketsBlock						# ArrayDeclarator
	| declarator templateArgsBlock? parensBlock		# FunctionDeclarator
	| '*' declarator								# PointerDeclarator
	| '&' declarator								# ReferenceDeclarator
	| (typeSpecifier '::')? Id?						# NameDeclarator
	;

type
	: typeSpecifier declarator ;

typeSpecifier
	: (nestedNameSpecifier? Id templateArgsBlock?) | IntType;

templateParamsBlock
	: '<' (templateParam (',' templateParam)*)? '>' ;

templateParam
	: (type | 'typename' | 'class') Id? ('=' (type | expressionWithoutAngleBrackets))? ;

templateArgsBlock
	: '<' (templateArg (',' templateArg)*)? '>' ;

templateArg
	: type
	| expressionWithoutAngleBrackets;

nestedNameSpecifier
	: '::' | (Id templateArgsBlock? '::')+ ;

bracesBlock
	: '{' blockContent '}' ;

bracketsBlock
	: '[' blockContent ']' ;

parensBlock
	: '(' blockContent ')' ;

blockContent
	: ( ~('{' | '}' | '(' | ')' | '[' | ']' ) | bracesBlock | bracketsBlock | parensBlock )* ;

expression
	: (bracesBlock | bracketsBlock | parensBlock | ~('(' | ')' | '[' | ']' | '{' | '}' | ';' | ','))+ ;

expressionWithoutAngleBrackets
	: (bracesBlock | bracketsBlock | parensBlock | ~('(' | ')' | '[' | ']' | '{' | '}' | ';' | ',' | '<' | '>'))+ ;

classDocBlock :
	DocBlockStart
	luaNameTagLine
	textTagLine?
	(constTagLine | flagTagLine | attributeTagLine)*
	DocBlockEnd
	;

functionDocBlock :
	DocBlockStart
	luaNameTagLine
	textTagLine?
	overloadList
	DocBlockEnd
	;

luaNameTagLine
	: LuaNameTag name=DocWord ;

textTagLine
	: TextTag description ;

constTagLine
	: ConstTag name=DocWord description? ;

flagTagLine
	: FlagTag name=DocWord description? ;

attributeTagLine
	: AttributeTag name=DocWord description? ;

overloadList
	: overloadTagLine? overloadBlock (overloadTagLine overloadBlock)* ;

overloadTagLine
	: OverloadTag description? ;

overloadBlock
	: inParamTagLine* optionalInParamTagLine* outParamTagLine* ;

inParamTagLine
	: InParamTag paramType=DocWord (name=DocWord description?)? ;

optionalInParamTagLine
	: OptionalInParamTag paramType=DocWord (name=DocWord description?)? ;

outParamTagLine
	: OutParamTag paramType=DocWord (name=DocWord description?)? ;

description
	: DocWord+ ;