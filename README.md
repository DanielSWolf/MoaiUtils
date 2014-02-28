# MoaiUtils

_Utilities for the Moai SDK_

MoaiUtils is a collection of useful command-line utilities to use with the [Moai SDK](http://getmoai.com/). For now, it consists of two tools: [DocExport](#docexport) and [DocLint](#doclint).
MoaiUtils is open source; a pre-compiled version can be downloaded on the [releases site](https://github.com/DanielSWolf/MoaiUtils/releases).

## DocExport

DocExport is a tool for generating code-completion files for the Moai SDK. This gives you a better coding experience in ZeroBrane Studio and Sublime Text. DocExport analyzes Moai's C++ source code and extracts the code comments documenting the Lua API. This information can then be exported to a number of formats.

### Creating a documentation file

DocExport supports the following command-line arguments:

* `-i`, `--input` The Moai base directory.
* `-o`, `--output` The output directory where the code completion file(s) will be created.
* `-f`, `--format` The export format. Valid options are `ZeroBrane`, `SublimeText`, or `XML`. For a description of these formats, see below.

On Windows, a typical invocation will look like this: `DocExport.exe -i "D:\moai-dev" -o "D:\temp" -f ZeroBrane`.
If you're on something other than Windows (i.e., Linux or OS X), you can run the tool using [Mono](http://www.mono-project.com/Main_Page). There's no need to recompile. Just run `mono DocExport.exe <arguments>`.

### Documentation file formats

#### ZeroBrane Studio

DocExport can create code-completion files for [ZeroBrane Studio](http://studio.zerobrane.com/), a great Lua IDE (donationware). This gives ZeroBrane Studio full access to all of Moai's classes, fields and methods, along with overloads, return values, and full documentation. For some screenshots, see this (otherwise dated) [Moai forum entry]( http://getmoai.com/forums/moaiutils-1-0-better-code-completion-in-zerobrane-t2473/#p12878) for MoaiTools 1.0.

##### How to install code completion

ZeroBrane Studio already ships with a `moai.lua` file in `ZeroBraneStudio\api\lua`. Just replace it with the new one.

##### How to use

Make sure you're in Moai mode ("Project" > "Lua Interpreter" > "Moai"). As you type, ZeroBrane will show a little popup with completion suggestions. You can accept a suggestion by selecting it with the arrow keys and hitting Tab or Enter.

Whenever you type the opening parenthesis for a method call, ZeroBrane will show you a popup with the method's documentation. By default, this popup will be limited to the first few lines of the help text. To see the entire documentation, press `Ctrl+T` (as in tooltip). If you always want to see the full text, go to "Edit" > "Preferences" > "Settings: User" and add the line `acandtip.shorttip = false`, then restart.

#### Sublime Text

[Sublime Text](http://www.sublimetext.com/) is a powerful text editor. Once you learn how to use its code completion facilities, it'll let you write Moai code quickly.

##### How to install code completion

Place the generated `moai_lua.sublime-completions` file anywhere in your `Packages` folder, ideally inside of `/Lua`. You can open the packages folder from Sublime Text via the menu option "Preferences"  > "Browse Packages". Depending on the version of Sublime Text and your OS, the location of the `/Lua` folder may vary.

##### How to use

As you type, Sublime Text will show a popup with suggestions. To accept a suggestion, press Enter, Tab, or any punctuation key. Sublime Text's suggestions use a fuzzy compare algorithm based on the current word. So, to quickly enter `MOAIGridSpace.TILE_TOP_CENTER`, enter something like "gridsptile", then accept the suggestion. Make sure not to type a dot between class name and member (e.g., "gridsp.tile"), or ZeroBrane will interpret the dot as your accepting whatever it was suggesting at this point.

To call a static method like `MOAISim.openWindow()`, type somethin like "simop", and accept the suggestion. ZeroBrane will expand this to `MOAISim.openWindow( title, width, height )` and select the first argument, `title`. Just type what you want to pass as title, then hit Tab to enter the next argument.

Calling a non-static method requires two steps. Let's assume you want to call `myProp:setDeck( myDeck )`. First, enter `myProp:`. Then, to tell ZeroBrane that you want to call the method `MOAIProp.setDeck()`, enter something like "propsetd" and accept. Because that method isn't static, ZeroBrane will omit the class name and replace the part after the colon with `setDeck( deck )`. Again, as with static methods, it'll then allow you to replace the argument `deck` with your actual value.

#### XML

DocExport can generate XML files containing all extracted API information. This is useful if you want to write your own tool and need the Moai API in a structured format. _Note:_ DocExport can easily be extended to support additional export formats. So if you need code completion for editor/IDE X, it might actually be easier to build that support right into DocExport (and let everybody profit from it) than to write your own tool based on the XML output.

## DocLint

When you extend the Moai SDK and expose your own classes and methods to Lua, DocLint helps you to make sure your API documentation is correct. If it isn't, Doxygen won't complain but silently generate false HTML documentation.
DocLint works by analyzing your code comments and comparing them to what the C++ code actually does. Whenever there is a problem with the code comments, DocLint prints a warning. 

DocLint finds all kinds of documentaion problems, including:
* Misspelled types
* Syntax errors in your annotations
* Missing or multiple annotations
* Logical errors, such as impossible method signatures
* undocumented methods or parameters.

### Running DocLint

DocLint supports the following command-line arguments:

* `-i`, `--input` The Moai base directory.
* `-u`, `--pathsAsUri` Optional. Formats file paths as URIs. Some text editors (such as [Notepad++](http://notepad-plus-plus.org/) on Windows) will display them as clickable links, allowing you to easily navigate to the problematic source files.

On Windows, a typical invocation will look like this: `DocLint.exe -i "D:\moai-dev"`.
If you're on something other than Windows (i.e., Linux or OS X), you can run the tool using [Mono](http://www.mono-project.com/Main_Page). There's no need to recompile. Just run `mono DocLint.exe <arguments>`.
