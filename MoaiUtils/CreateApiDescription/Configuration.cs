using CommandLine;

namespace MoaiUtils.CreateApiDescription {
    public class Configuration {
        [Option('i', "input", Required = true,
            HelpText = "The Moai src directory")]
        public string InputDirectory { get; set; }

        [Option('o', "output", Required = true,
            HelpText = "The output directory where the code completion file(s) will be created")]
        public string OutputDirectory { get; set; }

        [Option("pathFormat",
            HelpText = "Determines how file paths will be displayed in messages. Valid options are Absolute (default), Relative (shorter), or URI (for clickable links in some editors).")]
        public PathFormat MessagePathFormat { get; set; }
    }
}
