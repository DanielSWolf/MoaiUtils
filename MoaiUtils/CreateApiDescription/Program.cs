using System;
using System.IO;
using System.Linq;
using MoaiUtils.Common;
using MoaiUtils.CreateApiDescription.Exporters;
using MoaiUtils.MoaiParsing;

namespace MoaiUtils.CreateApiDescription {
    class Program {
        private static int Main(string[] args) {
            return Bootstrapper.Start<Configuration>(args, Main);
        }

        private static void Main(Configuration configuration) {
            if (!Directory.Exists(configuration.OutputDirectory)) {
                throw new PlainTextException("Output directory '{0}' does not exist.", configuration.OutputDirectory);
            }

            // Parse Moai code
            var parser = new MoaiCodeParser(statusCallback: s => Console.WriteLine("[] {0}", s));
            parser.Parse(new DirectoryInfo(configuration.InputDirectory));

            // Show warning count
            if (parser.Warnings.Any()) {
                Console.WriteLine("\nParsing resulted in {0} warnings.", parser.Warnings.Count);
            }

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
        }
    }
}
