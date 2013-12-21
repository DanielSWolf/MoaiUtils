using System.Reflection;

namespace MoaiUtils.Common {
    public static class CurrentUtility {
        public static AssemblyName AssemblyName {
            get { return Assembly.GetEntryAssembly().GetName(); }
        }

        private static string Name {
            get { return AssemblyName.Name; }
        }

        public static string Signature {
            get { return string.Format("{0} v{1}", Name, AssemblyName.Version.ToString(2)); }
        }

        public static string MoaiUtilsHint {
            get { return string.Format("{0} is part of MoaiUtils (https://github.com/DanielSWolf/MoaiUtils).", Name); }
        }
    }
}