using System.Collections.Generic;
using System.IO;
using MoaiUtils.MoaiParsing.CodeGraph;

namespace MoaiUtils.CreateApiDescription.Exporters {
    public interface IApiExporter {
        void Export(IEnumerable<MoaiType> types, DirectoryInfo outputDirectory);
    }
}