call java org.antlr.v4.Tool -o Java *.g4
call javac Java\*.java
cd Java
java org.antlr.v4.runtime.misc.TestRig MoaiCpp file -gui "%1"
cd ..