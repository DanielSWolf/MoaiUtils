using System.Collections.Generic;
using System.IO;
using System.Linq;
using MoaiUtils.MoaiParsing.CodeGraph;
using Newtonsoft.Json.Linq;

namespace MoaiUtils.DocExport.Exporters {
    public class SublimeTextExporter : IApiExporter {
        public void Export(IEnumerable<MoaiType> types, string header, DirectoryInfo outputDirectory) {
            // Create contents
            JObject contentsObject = new JObject {
                { "scope", "source.lua" },
                { "completions", CreateCompletionListTable(types) }
            };

            // Write to file
            string targetFileName = Path.Combine(outputDirectory.FullName, "moai_lua.sublime-completions");
            File.WriteAllText(targetFileName, contentsObject.ToString());
        }

        private JArray CreateCompletionListTable(IEnumerable<MoaiType> types) {
            JArray completionList = new JArray();
            foreach (var type in types.OrderBy(type => type.Name)) {
                // Add class name
                completionList.Add(type.Name);

                // Add fields (skip inherited ones)
                var fields = type.Members
                    .OfType<MoaiField>()
                    .OrderBy(field => field.Name);
                foreach (var field in fields) {
                    completionList.Add(string.Format("{0}.{1}", type.Name, field.Name));
                }

                // Add methods
                var methods = type.AllMembers
                    .OfType<MoaiMethod>()
                    .OrderBy(method => method.Name);
                foreach (var method in methods) {
                    completionList.Add(string.Format("{0}.{1}( )", type.Name, method.Name));
                }
            }

            return completionList;
        }
    }
}