using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using MoaiUtils.Common;
using MoaiUtils.Tools;

namespace MoaiUtils.MoaiParsing {
    public class MoaiVersionInfo {
        public MoaiVersionInfo(DirectoryInfo moaiDirectory) {
            var versionFileInfo = moaiDirectory.GetFileInfo(@"src\config-default\moai_version.h");
            try {
                // Read version file
                string versionText = File.ReadAllText(versionFileInfo.FullName);

                // Extract version, revision, and author
                Match versionMatch = Regex.Match(versionText, @"MOAI_SDK_VERSION_MAJOR_MINOR\s+([0-9]+)\.([0-9]+)");
                Major = int.Parse(versionMatch.Groups[1].Value);
                Minor = int.Parse(versionMatch.Groups[2].Value);
                Revision = int.Parse(Regex.Match(versionText, @"MOAI_SDK_VERSION_REVISION\s+(-?[0-9]+)").Groups[1].Value, CultureInfo.InvariantCulture);
                Author = Regex.Match(versionText, @"MOAI_SDK_VERSION_AUTHOR\s+""(.*?)""").Groups[1].Value;
            } catch (Exception e) {
                throw new PlainTextException("Error determining Moai version from '{0}'. {1}",
                    versionFileInfo.FullName, e.Message);
            }
        }

        public int Major { get; private set; }
        public int Minor { get; private set; }
        public int Revision { get; private set; }
        public string Author { get; private set; }

        public override string ToString() {
            string result = string.Format("Moai SDK {0}.{1}", Major, Minor);
            if (Revision >= 0) {
                result += string.Format(" revision {0}", Revision);
            }
            if (Author != string.Empty) {
                result += string.Format(Revision > 1 ? " ({0})" : " (interim version by {0})", Author);
            }

            return result;
        }
    }
}