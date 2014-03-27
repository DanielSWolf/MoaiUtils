using System.IO;
using MoaiUtils.MoaiParsing.CodeGraph.Types;

namespace MoaiUtils.DocExport.Exporters {
    public interface IApiExporter {
        void Export(MoaiClass[] classes, string header, DirectoryInfo outputDirectory);
    }
}