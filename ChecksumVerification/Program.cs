using CommandLine;
using System.IO;


namespace ChecksumVerification
{
    class Program
    {
        public class Options
        {
            [Option('s', "scan", Required = false, HelpText = "Scan files.")]
            public bool Scan { get; set; }

            [Option('v', "verify", Required = false, HelpText = "Verify files.")]
            public bool Verify { get; set; }

            [Option('p', "path", Required = false, HelpText = "(Default: Current directory) Path to files.")]
            public string BasePath { get; set; }

            [Option('f', "file", Required = false, HelpText = "(Default: checksum.txt) Result file.")]
            public string ResultFile { get; set; }
        }

        static void Main(string[] args)
        {
            string basePath = Directory.GetCurrentDirectory();
            string resultFile = "checksum.txt";

            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(o =>
                {
                    if (o.BasePath != null)
                        basePath = o.BasePath;

                    if (o.ResultFile != null)
                        resultFile = o.ResultFile;

                    if (o.Scan && !o.Verify)
                        new ChecksumManager(basePath, resultFile).Scan();

                    if (o.Verify && !o.Scan)
                        new ChecksumManager(basePath, resultFile).Verify();
                });
        }
    }
}
