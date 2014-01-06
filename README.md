MoaiUtils
=========

Utilities for the Moai SDK

At one point, MoaiUtils will become a collection of useful command-line utilities for people using the Moai SDK (http://getmoai.com/). For now, it consists of a single tool, CreateApiDescription.

CreateApiDescription
--------------------

Given a local copy of the Moai source code, this tool will extract all kinds of information from the code and its comments. It will then generate a code-completion file for ZeroBrane Studio (http://studio.zerobrane.com/). Using this file, you'll get far superior code completion for Moai than with the built-in support. For command-line options, just call the executable without any arguments.