using System;
using System.Globalization;
using System.IO;
using System.Linq;
using MoaiUtils.Common;
using MoaiUtils.DocExport.Exporters;
using MoaiUtils.MoaiParsing;

namespace MoaiUtils.DocExport {
    class Program {
        private static int Main(string[] args) {
            return Bootstrapper.Start<Configuration>(args, Main);
        }

        private static void Main(Configuration configuration) {
            if (!Directory.Exists(configuration.OutputDirectory)) {
                throw new PlainTextException("Output directory '{0}' does not exist.", configuration.OutputDirectory);
            }

            // Parse Moai code
            var parser = new MoaiParser(statusCallback: s => Console.WriteLine("[] {0}", s));
            parser.Parse(new DirectoryInfo(configuration.InputDirectory));

            // Show warning count
            if (parser.Warnings.Any()) {
                Console.WriteLine("\nParsing resulted in {0} warnings. For more information, run DocLint.", parser.Warnings.Count);
            }

            // Export API description
            string header = string.Format(CultureInfo.InvariantCulture,
                "Documentation for {0} (http://getmoai.com/)\n"
                + "Generated on {1:yyyy-MM-dd} by {2}.\n"
                + CurrentUtility.MoaiUtilsHint,
                parser.MoaiVersionInfo, DateTime.Now, CurrentUtility.Signature);
            IApiExporter exporter;
            switch (configuration.ExportFormat) {
                case ExportFormat.ZeroBrane:
                    exporter = new ZeroBraneExporter();
                    break;
                case ExportFormat.SublimeText:
                    exporter = new SublimeTextExporter();
                    break;
                case ExportFormat.XML:
                    exporter = new XmlExporter();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            DirectoryInfo outputDirectory = new DirectoryInfo(configuration.OutputDirectory);
            exporter.Export(parser.DocumentedClasses.ToArray(), header, outputDirectory);

            Console.WriteLine("\nGenerated Moai documentation in {0} format in '{1}'.",
                configuration.ExportFormat, outputDirectory.FullName);
        }
    }
}
