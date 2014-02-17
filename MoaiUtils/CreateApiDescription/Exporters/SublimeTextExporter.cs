using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MoaiUtils.Common;
using MoaiUtils.CreateApiDescription.CodeGraph;
using MoaiUtils.LuaIO;
using MoaiUtils.Tools;

namespace MoaiUtils.CreateApiDescription.Exporters {
    public class SublimeTextExporter : IApiExporter {
        public void Export(IEnumerable<MoaiType> types, DirectoryInfo outputDirectory) {
            // Create head comment
            var commentLines = new[] {
                "Documentation of the Moai SDK (http://getmoai.com/)",
                string.Format(CultureInfo.InvariantCulture, "Generated on {0:d} by {1}", DateTime.Now, CurrentUtility.Signature),
                CurrentUtility.MoaiUtilsHint
            };
            var headComment = new LuaComment(commentLines, blankLineAfter: true);

            // Create contents
            LuaTable contentsTable = new LuaTable {
                { "scope", "source.lua" },
                { "completions", CreateCompletionListTable(types) }
            };

            // Write to file
            var targetFileInfo = outputDirectory.GetFileInfo("moai_lua.sublime-completions");
            LuaTableWriter.Write(contentsTable, targetFileInfo, returnStatement: false, headComment: headComment);
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