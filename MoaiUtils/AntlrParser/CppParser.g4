parser grammar CppParser;

options { tokenVocab = CppLexer; }

file : topLevelStatement* EOF ;

topLevelStatement
	: declaration
	| typeDef
	| usingDirective
	| functionDefinition
	| classDefinition
	| constructorDefinition
	| destructorDefinition
	| '{' topLevelStatement* '}'
	| Namespace Id? '{' topLevelStatement* '}'
	| ';' ;

declaration :
	('template' templateParamsBlock)?
	(typeSpecifier | 'class' | 'struct' | 'union' | 'enum') declarator ('=' expression)? ';' ;

typeDef
	: 'typedef' ('class' | 'struct' | 'union' | 'enum')? type ';' ;

usingDirective
	: 'using' 'namespace' .*? ';' ;

functionDefinition :
	functionDocBlock?
	('template' templateParamsBlock)?
	type bracesBlock ;

classDefinition :
	classDocBlock?
	'typedef'? ('template' templateParamsBlock)?
	('class' | 'struct' | 'union' | 'enum') (typeSpecifier baseClause?)? bracesBlock Id? ';' ;

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
	: (typeSpecifier '::')? Id?						# Name
	| declarator bracketsBlock						# Array
	| declarator templateArgsBlock? parensBlock		# Function
	| '*' declarator								# Pointer
	| '&' declarator								# Reference
	| '(' declarator ')'							# Group
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
	: TextTag text=DocWord+ ;

constTagLine
	: ConstTag name=DocWord (text=DocWord+)? ;

flagTagLine
	: FlagTag name=DocWord (text=DocWord+)? ;

attributeTagLine
	: AttributeTag name=DocWord (text=DocWord+)? ;

overloadList
	: overloadTagLine? overloadBlock (overloadTagLine overloadBlock)* ;

overloadTagLine
	: OverloadTag (text=DocWord+)? ;

overloadBlock
	: inParamTagLine* optionalInParamTagLine* outParamTagLine* ;

inParamTagLine
	: InParamTag paramType=DocWord (name=DocWord (text=DocWord+)?)? ;

optionalInParamTagLine
	: OptionalInParamTag paramType=DocWord (name=DocWord (text=DocWord+)?)? ;

outParamTagLine
	: OutParamTag paramType=DocWord (name=DocWord (text=DocWord+)?)? ;
