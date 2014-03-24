﻿using System.Collections.Generic;
using System.IO;
using MoaiUtils.MoaiParsing.CodeGraph;

namespace MoaiUtils.DocExport.Exporters {
    public interface IApiExporter {
        void Export(IEnumerable<Type> types, string header, DirectoryInfo outputDirectory);
    }
}