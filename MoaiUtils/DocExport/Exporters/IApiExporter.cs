using System.Collections.Generic;
using System.IO;
using MoaiUtils.MoaiParsing.CodeGraph.Types;

namespace MoaiUtils.DocExport.Exporters {
    public interface IApiExporter {
        void Export(IEnumerable<MoaiClass> classes, string header, DirectoryInfo outputDirectory);
    }
}