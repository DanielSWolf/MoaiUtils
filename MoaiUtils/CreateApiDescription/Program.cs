using System;
using System.IO;
using System.Linq;
using CommandLine;
using CommandLine.Text;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using MoaiUtils.Common;
using MoaiUtils.CreateApiDescription.Exporters;
using MoaiUtils.MoaiParsing;
using MoaiUtils.MoaiParsing.CodeGraph;

namespace MoaiUtils.CreateApiDescription {
    class Program {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program).Assembly.GetName().Name);

        static int Main(string[] args) {
            // Configure log4net
            BasicConfigurator.Configure(new ConsoleAppender { Layout = new SimpleLayout() });

            log.Info(CurrentUtility.Signature);
            log.Info(CurrentUtility.MoaiUtilsHint);

            try {
                // Parse command line arguments
                var configuration = new Configuration();
                if (!Parser.Default.ParseArguments(args, configuration)) {
                    Console.WriteLine(HelpText.AutoBuild(configuration,
                        current => HelpText.DefaultParsingErrorsHandler(configuration, current)));
                    return 1;
                }
                if (!Directory.Exists(configuration.OutputDirectory)) {
                    throw new ApplicationException(string.Format("Output directory '{0}' does not exist.", configuration.OutputDirectory));
                }

                // Parse Moai code
                var parser = new MoaiCodeParser(statusCallback: message => log.Info(message));
                parser.Parse(new DirectoryInfo(configuration.InputDirectory), configuration.MessagePathFormat);

                // Show warning count
                if (parser.Warnings.Any()) {
                    log.WarnFormat("Parsing resulted in {0} warnings.", parser.Warnings.Count);
                }

                var methods = parser.DocumentedTypes
                    .SelectMany(type => type.Members.OfType<MoaiMethod>())
                    .OrderByDescending(method => method.Overloads.Aggregate(0, (count, o) => o.InParameters.Count + count));

                // Export API description
                IApiExporter exporter;
                switch (configuration.ExportFormat) {
                    case ExportFormat.ZeroBrane:
                        exporter = new ZeroBraneExporter();
                        break;
                    case ExportFormat.SublimeText:
                        exporter = new SublimeTextExporter();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                exporter.Export(parser.DocumentedTypes, new DirectoryInfo(configuration.OutputDirectory));

                return 0;
            } catch (Exception e) {
                log.Fatal(e.Message, e);
                return 1;
            }
        }
    }
}
