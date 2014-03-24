using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MoaiUtils.MoaiParsing.CodeGraph;
using MoaiUtils.Tools;
using Newtonsoft.Json.Linq;
using Type = MoaiUtils.MoaiParsing.CodeGraph.Type;

namespace MoaiUtils.DocExport.Exporters {
    public class SublimeTextExporter : IApiExporter {
        public void Export(IEnumerable<Type> types, string header, DirectoryInfo outputDirectory) {
            // Create contents
            JObject contentsObject = new JObject {
                { "scope", "source.lua" },
                { "completions", CreateCompletionListTable(types.Where(type => type.IsScriptable)) }
            };

            // Write to file
            string targetFileName = Path.Combine(outputDirectory.FullName, "moai_lua.sublime-completions");
            using (var file = File.CreateText(targetFileName)) {
                string comment = header.SplitIntoLines()
                    .Select(line => "// " + line)
                    .Join(Environment.NewLine);
                file.WriteLine(comment);
                file.WriteLine();
                file.WriteLine(contentsObject);
            }
        }

        private JArray CreateCompletionListTable(IEnumerable<Type> types) {
            JArray completionList = new JArray();
            foreach (var type in types.OrderBy(type => type.Name)) {
                // Add class name
                completionList.Add(type.Name);

                // Add fields
                var fields = type.AllMembers
                    .OfType<Field>()
                    .OrderBy(field => field.Name);
                foreach (var field in fields) {
                    completionList.Add(string.Format("{0}.{1}", type.Name, field.Name));
                }

                // Add methods
                var methods = type.AllMembers
                    .OfType<Method>()
                    .OrderBy(method => method.Name);
                foreach (var method in methods) {
                    foreach (var overload in method.Overloads) {
                        string trigger = string.Format("{0}.{1}{2}",
                            type.Name, method.Name, FormatTriggerParams(overload.InParameters));
                        string contents = overload.IsStatic
                            ? string.Format("{0}.{1}{2}", type.Name, method.Name, FormatReplacementParams(overload.InParameters))
                            : string.Format("{0}{1}", method.Name, FormatReplacementParams(overload.InParameters.Skip(1).ToList()));
                        completionList.Add(new JObject {
                            { "trigger", trigger },
                            { "contents", contents }
                        });
                    }
                }
            }

            return completionList;
        }

        private string FormatTriggerParams(List<InParameter> parameters) {
            if (!parameters.Any()) return "( )";

            StringBuilder result = new StringBuilder("( ");
            bool optional = false;
            for (int i = 0; i < parameters.Count; i++) {
                var parameter = parameters[i];
                if (parameter.IsOptional && !optional) {
                    result.Append("[");
                    optional = true;
                }
                result.Append(parameter.Name);
                if (i < parameters.Count - 1) result.Append(", ");
            }
            if (optional) result.Append("]");
            result.Append(" )");

            return result.ToString();
        }

        private string FormatReplacementParams(List<InParameter> parameters) {
            if (!parameters.Any()) return "( )";

            var paramStrings = parameters
                .Select((parameter, index) => string.Format("${{{0}:{1}}}", index + 1, parameter.Name));
            return string.Format("( {0} )", paramStrings.Join(", "));
        }

    }
}