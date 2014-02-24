using System;
using System.IO;
using System.Linq;
using CommandLine;
using CommandLine.Text;
using MoaiUtils.Common;
using MoaiUtils.CreateApiDescription.Exporters;
using MoaiUtils.MoaiParsing;
using MoaiUtils.MoaiParsing.CodeGraph;

namespace MoaiUtils.CreateApiDescription {
    class Program {
        static int Main(string[] args) {
            // Configure log4net
            Console.WriteLine(CurrentUtility.Signature);
            Console.WriteLine(CurrentUtility.MoaiUtilsHint);
            Console.WriteLine();

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
                var parser = new MoaiCodeParser(statusCallback: Console.WriteLine);
                parser.Parse(new DirectoryInfo(configuration.InputDirectory));

                // Show warning count
                if (parser.Warnings.Any()) {
                    Console.WriteLine("Parsing resulted in {0} warnings.", parser.Warnings.Count);
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
                Console.WriteLine(e);
                Console.WriteLine("Terminating application.");
                return 1;
            }
        }
    }
}
