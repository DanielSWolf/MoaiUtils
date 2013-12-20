using System.Collections.Generic;
using System.IO;
using CreateApiDescription.CodeGraph;

namespace CreateApiDescription.Exporters {
    public interface IApiExporter {
        void Export(IEnumerable<MoaiType> types, DirectoryInfo outputDirectory);
    }
}