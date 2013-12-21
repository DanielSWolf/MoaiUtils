using System.Collections.Generic;
using System.IO;
using MoaiUtils.CreateApiDescription.CodeGraph;

namespace MoaiUtils.CreateApiDescription.Exporters {
    public interface IApiExporter {
        void Export(IEnumerable<MoaiType> types, DirectoryInfo outputDirectory);
    }
}