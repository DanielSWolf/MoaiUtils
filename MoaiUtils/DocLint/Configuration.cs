using CommandLine;

namespace MoaiUtils.DocLint {
    public class Configuration {
        [Option('i', "input", Required = true,
            HelpText = "The Moai base directory")]
        public string InputDirectory { get; set; }

        [Option('u', "pathsAsUri",
            HelpText = "Formats file paths as URIs. This allows for clickable links in some text editors.")]
        public bool PathsAsUri { get; set; }
    }
}
