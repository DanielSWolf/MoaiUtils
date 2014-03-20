using System.Collections.Generic;
using System.IO;
using System.Linq;
using MoaiUtils.MoaiParsing.CodeGraph;
using MoaiUtils.Tools;

namespace MoaiUtils.MoaiParsing.Checks {
    public abstract class CheckBase {
        public DirectoryInfo MoaiDirectory { get; set; }
        public MoaiTypeCollection Types { get; set; }
        public WarningList Warnings { get; set; }
        
        public abstract void Run();

        protected DirectoryInfo MoaiSrcDirectory {
            get { return MoaiDirectory.GetDirectoryInfo("src"); }
        }

        protected IEnumerable<MoaiMethod> Methods {
            get { return Types.SelectMany(type => type.Members).OfType<MoaiMethod>(); }
        }
    }
}