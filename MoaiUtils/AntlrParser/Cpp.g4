parser grammar Cpp;

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
	('template' templateParamsBlock)?
	type bracesBlock ;

classDefinition :
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

