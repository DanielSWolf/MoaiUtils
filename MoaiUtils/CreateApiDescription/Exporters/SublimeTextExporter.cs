using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MoaiUtils.LuaIO;
using MoaiUtils.MoaiParsing.CodeGraph;
using MoaiUtils.Tools;

namespace MoaiUtils.CreateApiDescription.Exporters {
    public class SublimeTextExporter : IApiExporter {
        public void Export(IEnumerable<MoaiType> types, string header, DirectoryInfo outputDirectory) {
            // Create contents
            LuaTable contentsTable = new LuaTable {
                { "scope", "source.lua" },
                { "completions", CreateCompletionListTable(types) }
            };

            // Write to file
            var targetFileInfo = outputDirectory.GetFileInfo("moai_lua.sublime-completions");
            LuaTableWriter.Write(contentsTable, targetFileInfo, returnStatement: false,
                headComment: new LuaComment(header, blankLineAfter: true));
        }

        private LuaTable CreateCompletionListTable(IEnumerable<MoaiType> types) {
            LuaTable completionListTable = new LuaTable();
            foreach (var type in types.OrderBy(type => type.Name)) {
                // Add class name
                completionListTable.Add(new LuaComment(String.Format("class {0}", type.Name), blankLineBefore: true));
                completionListTable.Add(type.Name);

                // Add fields
                var fields = type.AllMembers
                    .OfType<MoaiField>()
                    .OrderBy(field => field.Name);
                foreach (var field in fields) {
                    completionListTable.Add(string.Format("{0}.{1}", type.Name, field.Name));
                }

                // Add methods
                var methods = type.AllMembers
                    .OfType<MoaiMethod>()
                    .OrderBy(method => method.Name);
                foreach (var method in methods) {
                    completionListTable.Add(string.Format("{0}.{1}( )", type.Name, method.Name));
                }
            }

            return completionListTable;
        }
    }
}