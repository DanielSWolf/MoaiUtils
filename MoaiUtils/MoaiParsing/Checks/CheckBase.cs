using System.Collections.Generic;
using System.IO;
using System.Linq;
using MoaiUtils.MoaiParsing.CodeGraph;
using MoaiUtils.MoaiParsing.CodeGraph.Types;
using MoaiUtils.Tools;

namespace MoaiUtils.MoaiParsing.Checks {
    public abstract class CheckBase {
        public DirectoryInfo MoaiDirectory { get; set; }
        public TypeCollection Types { get; set; }
        public WarningList Warnings { get; set; }
        
        public abstract void Run();

        protected DirectoryInfo MoaiSrcDirectory {
            get { return MoaiDirectory.GetDirectoryInfo("src"); }
        }

        protected IEnumerable<MoaiClass> Classes {
            get { return Types.OfType<MoaiClass>(); }
        }

        protected IEnumerable<Method> Methods {
            get { return Classes.SelectMany(c => c.Members).OfType<Method>(); }
        }
    }
}