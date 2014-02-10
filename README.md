# MoaiUtils

Utilities for the Moai SDK

At one point, MoaiUtils will become a collection of useful command-line utilities for people using the [Moai SDK](http://getmoai.com/). For now, it consists of a single tool, CreateApiDescription.

## CreateApiDescription.exe

Given a local copy of the Moai source code, this tool will extract all kinds of information from the code and its comments. It will then generate a code-completion file for [ZeroBrane Studio](http://studio.zerobrane.com/). Using this file, you'll get far superior code completion for Moai than with the built-in support. For a detailed description with screenshots, see the [Moai forum entry]( http://getmoai.com/forums/moaiutils-1-0-better-code-completion-in-zerobrane-t2473/#p12878).

### How to use

1. Download MoaiUtils

   Download MoaiUtils.zip from the [current release of MoaiUtils](https://github.com/DanielSWolf/MoaiUtils/releases) and unpack it.

2. Run CreateApiDescription

   Point the tool to your local Moai source directory and tell it where to create its output. On Windows, a typical invocation will look like this: `CreateApiDescription.exe -i "D:\moai-dev\src" -o "D:\temp"`. For a complete list of command-line options, see below.
   If you're on something other than Windows (i.e., Linux or OS X), you can run the tool using [Mono](http://www.mono-project.com/Main_Page). There's no need to recompile or anything. Just run `mono CreateApiDescription.exe <arguments>`.

   This will create a file called moai.lua containing all code completion information.

3. Replace the code-completion file

   ZeroBrane Studio already ships with a moai.lua file in `ZeroBraneStudio\api\lua`. Just replace it with the new one and you're done!

4. Enjoy! :-)

   Tip: By default, ZeroBrane Studio only shows the first few lines of the help text. To see the entire documentation, press Ctrl+T (as in tooltip).

### Command-line options

* `-i`, `--input`

  Required. The Moai `src` directory.

* `-o`, `--output`

  Required. The output directory where the code completion file(s) will be created.
  
* `--pathFormat`

  CreateApiDescription will print all kinds of useful warnings regarding possible errors in your Moai code. This option determines the paths to these code files will be displayed in messages. Valid options are:
  
  * `Absolute` (default), e.g. `c:\dev\moai\src\moai-sim\MOAITexture.cpp`. This may get quite long, but allows you to copy-and-paste the paths to open the files.
  * `Relative`, e.g. `moai-sim\MOAITexture.cpp`. This makes for less clutter, but it may become a bit tedious to manually open the files.
  * `URI`, e.g. `file:///c:/dev/moai/src/moai-sim/MOAITexture.cpp`. If you redirect the output to a text file, some editors (such as [Notepad++](http://notepad-plus-plus.org/) on Windows) will display the paths as clickable links, allowing you to easily open them with a click (or, for Notepad++, a double-click).
