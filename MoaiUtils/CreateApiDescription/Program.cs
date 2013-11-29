using System;
using System.IO;
using CommandLine;
using CommandLine.Text;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Layout;

namespace CreateApiDescription {
    class Program {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program).Assembly.GetName().Name);

        static int Main(string[] args) {
            // Configure log4net
            BasicConfigurator.Configure(new ConsoleAppender { Layout = new SimpleLayout() });

            try {
                // Parse command line arguments
                var configuration = new Configuration();
                if (!Parser.Default.ParseArguments(args, configuration)) {
                    Console.WriteLine(HelpText.AutoBuild(configuration,
                        current => HelpText.DefaultParsingErrorsHandler(configuration, current)));
                    return 1;
                }

                // Parse Moai code
                var parser = new MoaiCodeParser();
                parser.Parse(new DirectoryInfo(configuration.InputDirectory));

                return 0;
            } catch (Exception e) {
                log.Fatal(e.Message, e);
                return 1;
            }
        }
    }
}
