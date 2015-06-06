# Generating the parser code

I'm using [ANTLR 4](http://www.antlr.org/) to generate the actual parser code from the grammar files. While ANTLR is a great tool, it clearly comes from a Java background, so getting it to work in a .NET environment isn't completely straightforward.

## Install Java

Install the SDK for Java 6 or 7

## Download and install the ANTLR tool

* Download [antlr-4.5-complete.jar](http://www.antlr.org/download/antlr-4.5-complete.jar) and place it into a permanent directory
* Make sure you have a `CLASSPATH` environment variable; if not, create one. This environment variable should be a semicolon-separated list of paths. Append the full path of the jar file. Also make sure that `.` (a dot, representing the current directory) is one of the paths.

## Generate the parser

You can now re-generate the C# parser files by running `GenerateParsers.bat`.