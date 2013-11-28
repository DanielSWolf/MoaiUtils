using CommandLine;

namespace CreateCodeCompletionDatabase {
    public class Configuration {
        [Option('i', "input", Required = true,
            HelpText = "The Moai src directory")]
        public string InputDirectory { get; set; }

        [Option('o', "output", Required = true,
            HelpText = "The output directory where the code completion file(s) will be created")]
        public string OutputDirectory { get; set; }
    }
}
