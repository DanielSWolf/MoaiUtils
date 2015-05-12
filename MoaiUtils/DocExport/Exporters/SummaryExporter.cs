using System.IO;
using System.Linq;
using MoaiUtils.MoaiParsing;
using MoaiUtils.MoaiParsing.CodeGraph;
using MoaiUtils.MoaiParsing.CodeGraph.Types;
using MoaiUtils.Tools;

namespace MoaiUtils.DocExport.Exporters {
    public class SummaryExporter : IApiExporter {

        public void Export(MoaiClass[] classes, string header, DirectoryInfo outputDirectory) {
            using (var file = outputDirectory.GetFileInfo("moai.txt").CreateText()) {
                var exportClasses = classes
                    .Where(c => c.IsScriptable)
                    .OrderBy(c => c.Name);
                foreach (MoaiClass moaiClass in exportClasses) {
                    file.WriteLine("class {0}{1}{2}",
                        moaiClass.Name,
                        moaiClass.BaseClasses.Any() ? " : " : "",
                        moaiClass.BaseClasses.Select(c => c.Name).Join(", "));
                    foreach (var constant in moaiClass.Members.OfType<Constant>().OrderBy(c => c.Name)) {
                        file.WriteLine("\t[const]  {0}", constant.Name);
                    }
                    foreach (var flag in moaiClass.Members.OfType<Flag>().OrderBy(f => f.Name)) {
                        file.WriteLine("\t[flag]   {0}", flag.Name);
                    }
                    foreach (var attribute in moaiClass.Members.OfType<Attribute>().OrderBy(a => a.Name)) {
                        file.WriteLine("\t[attrib] {0}", attribute.Name);
                    }
                    foreach (var method in moaiClass.Members.OfType<Method>()) {
                        file.WriteLine("\t[method] {0} : {1} -> {2}",
                            method.Name,
                            method.InParameterSignature != null ? method.InParameterSignature.ToString(SignatureGrouping.Any) : "?",
                            method.OutParameterSignature != null ? method.OutParameterSignature.ToString(SignatureGrouping.Any) : "?");
                    }
                }
            }
        }
    }
}