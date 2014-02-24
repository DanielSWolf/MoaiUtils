using System;
using CommandLine;
using CommandLine.Text;

namespace MoaiUtils.Common {

    public delegate void MainFunction<in TConfiguration>(TConfiguration configuration);

    public static class Bootstrapper {
        public static int Start<TConfiguration>(string[] args, MainFunction<TConfiguration> main)
            where TConfiguration : new() {
            Console.WriteLine(CurrentUtility.Signature);
            Console.WriteLine(CurrentUtility.MoaiUtilsHint);
            Console.WriteLine();

            try {
                // Parse command line arguments
                TConfiguration configuration = Activator.CreateInstance<TConfiguration>();
                if (!Parser.Default.ParseArguments(args, configuration)) {
                    Console.WriteLine(HelpText.AutoBuild(configuration,
                        current => HelpText.DefaultParsingErrorsHandler(configuration, current)));
                    return 1;
                }

                // Start application
                main(configuration);

                return 0;
            } catch (Exception e) {
                string message = e is PlainTextException ? e.Message : e.ToString();
                Console.WriteLine("\nAn error occurred.");
                Console.WriteLine(message);
                Console.WriteLine("Terminating application.");
                return 1;
            }
        }
    }
}