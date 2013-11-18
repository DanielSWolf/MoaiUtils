using System;
using System.CodeDom.Compiler;

namespace Tools {
    public static class IndentedTextWriterExtensions {
        public static IDisposable IndentBlock(this IndentedTextWriter indentedTextWriter) {
            return new Indenter(indentedTextWriter);
        }

        private class Indenter : IDisposable {
            private readonly IndentedTextWriter indentedTextWriter;

            public Indenter(IndentedTextWriter indentedTextWriter) {
                this.indentedTextWriter = indentedTextWriter;
                indentedTextWriter.Indent++;
            }

            public void Dispose() {
                indentedTextWriter.Indent--;
            }
        }
    }
}