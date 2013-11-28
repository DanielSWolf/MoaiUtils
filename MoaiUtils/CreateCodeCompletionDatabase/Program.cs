using System;
using System.IO;
using System.Linq;
using CreateCodeCompletionDatabase.Graph;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Layout;

namespace CreateCodeCompletionDatabase {
    class Program {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program).Assembly.GetName().Name);

        static int Main(string[] args) {
            // Configure log4net
            BasicConfigurator.Configure(new ConsoleAppender { Layout = new SimpleLayout() });

            try {
                var parser = new MoaiCodeParser();
                parser.Parse(new DirectoryInfo(@"D:\dev\projects\moai\src"));
                return 0;
            } catch (Exception e) {
                log.Fatal(e.Message, e);
                return 1;
            }
        }
    }
}
