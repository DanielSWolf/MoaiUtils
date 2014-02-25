# MoaiUtils

_Utilities for the Moai SDK_

MoaiUtils is a collection of useful command-line utilities to use with the [Moai SDK](http://getmoai.com/). For now, it consists of two tools: [DocExport](#docexport) and [DocLint](#doclint).
MoaiUtils is open source; a pre-compiled version can be downloaded on the [releases site](https://github.com/DanielSWolf/MoaiUtils/releases).

## DocExport

DocExport analyzes Moai's C++ source code and extracts the code comments documenting the Lua API. This information can then be exported to a number of formats, usually to create code-completion files for text editors.

### Creating a documentation file

DocExport supports the following command-line arguments:

* `-i`, `--input` The Moai base directory.
* `-o`, `--output` The output directory where the code completion file(s) will be created.
* `-f`, `--format` The export format. Valid options are `ZeroBrane`, `SublimeText`, or `XML`. For a description of these formats, see below.

On Windows, a typical invocation will look like this: `DocExport.exe -i "D:\moai-dev" -o "D:\temp" -f ZeroBrane`.
If you're on something other than Windows (i.e., Linux or OS X), you can run the tool using [Mono](http://www.mono-project.com/Main_Page). There's no need to recompile. Just run `mono DocExport.exe <arguments>`.

### Documentation file formats

#### ZeroBrane Studio

DocExport can create code-completion files for [ZeroBrane Studio](http://studio.zerobrane.com/), the free Lua IDE. This gives ZeroBrane Studio full access to all of Moai's classes, fields and methods, along with overloads, return values, and full documentation. For some screenshots, see this (otherwise dated) [Moai forum entry]( http://getmoai.com/forums/moaiutils-1-0-better-code-completion-in-zerobrane-t2473/#p12878) for MoaiTools 1.0.

##### How to use

ZeroBrane Studio already ships with a `moai.lua` file in `ZeroBraneStudio\api\lua`. Just replace it with the new one.

_Tip:_ By default, ZeroBrane Studio only shows the first few lines of the help text. To see the entire documentation, press `Ctrl+T` (as in tooltip).

#### Sublime Text

Sublime Text is a powerful text editor, but no IDE. Thus, support for Sublime Text is limited to simple code completion for class, field and method names.

##### How to use

Place the generated `moai_lua.sublime-completions` file anywhere in your `Packages` folder, ideally inside of `/Lua`. You can open the packages folder from Sublime Text via the "Browse Packages" menu option. Depending on the version of Sublime Text and your OS, the location of the `/Lua` folder may vary.

#### XML

DocExport can generate XML files containing all extracted API information. This is useful if you want to write your own tool and need the Moai API in a structured format. _Note:_ DocExport can easily be extended to support additional export formats. So if you need code completion for editor/IDE X, it might actually be easier to build that support right into DocExport (and let everybody profit from it) than to write your own tool based on the XML output.

## DocLint

DocLint analyzes Moai's API documentation and prints useful warnings if there may be problems. This is very useful when extending Moai with your own classes and methods. It's easy to get the documentation format wrong. Doxygen won't complain but silently generate false HTML documentation.

DocLint finds all kinds of documentaion problems, including:
* Misspelled types
* Syntax errors in your annotations
* Missing or multiple annotations
* Logical errors, such as impossible method signatures

DocLint will even analyze your method implementations and warn you if a method accesses an undocumented parameter.

### Running DocLint

DocLint supports the following command-line arguments:

* `-i`, `--input` The Moai base directory.
* `-u`, `--pathsAsUri` Optional. Formats file paths as URIs. Some text editors (such as [Notepad++](http://notepad-plus-plus.org/) on Windows) will display them as clickable links, allowing you to easily navigate to the problematic source files.

On Windows, a typical invocation will look like this: `DocLint.exe -i "D:\moai-dev"`.
If you're on something other than Windows (i.e., Linux or OS X), you can run the tool using [Mono](http://www.mono-project.com/Main_Page). There's no need to recompile. Just run `mono DocLint.exe <arguments>`.
