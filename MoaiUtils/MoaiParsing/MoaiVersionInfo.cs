using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MoaiUtils.Common;
using MoaiUtils.Tools;

namespace MoaiUtils.MoaiParsing {

	public class MoaiVersionInfo {
		public MoaiVersionInfo(DirectoryInfo moaiDirectory) {
			DirectoryInfo configDir = moaiDirectory.GetDirectoryInfo(@"src\config-default");
			try {
				// Read all version files
				string versionText = configDir.GetFiles("*.h")
					.Select(file => file.ReadAllText())
					.Join("\n");

				// Extract values
				Major = ParseInt("MOAI_SDK_VERSION_MAJOR", versionText);
				Minor = ParseInt("MOAI_SDK_VERSION_MINOR", versionText);
				Revision = ParseInt("MOAI_SDK_VERSION_REVISION", versionText);
				Author = Regex.Match(versionText, @"MOAI_SDK_VERSION_AUTHOR\s+""(.*?)""").Groups[1].Value;
			} catch (Exception e) {
				throw new PlainTextException("Error determining Moai version from '{0}'. {1}",
					configDir.FullName, e.Message);
			}
		}

		public int Major { get; private set; }
		public int Minor { get; private set; }
		public int Revision { get; private set; }
		public string Author { get; private set; }

		public override string ToString() {
			StringBuilder result = new StringBuilder();
			result.AppendFormat("Moai SDK {0}.{1}", Major, Minor);
			if (Revision > 0) {
				result.AppendFormat(".{0}", Revision);
			}
			if (Author != string.Empty) {
				result.AppendFormat(" (interim version by {0})", Author);
			}

			return result.ToString();
		}

		private static int ParseInt(string name, string versionText) {
			string valueString = Regex.Match(versionText, name + @"\s+(-?[0-9]+)").Groups[1].Value;
			return int.Parse(valueString, CultureInfo.InvariantCulture);
		}
	}

}