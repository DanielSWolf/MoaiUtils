using CommandLine;

namespace MoaiUtils.DocExport {
    public class Configuration {
        [Option('i', "input", Required = true,
            HelpText = "The Moai base directory")]
        public string InputDirectory { get; set; }

        [Option('o', "output", Required = true,
            HelpText = "The output directory where the code completion file(s) will be created")]
        public string OutputDirectory { get; set; }

        [Option('f', "format", Required = true,
            HelpText = "The export format. Valid options are ZeroBrane, SublimeText, or XML.")]
        public ExportFormat ExportFormat { get; set; }
    }
}
