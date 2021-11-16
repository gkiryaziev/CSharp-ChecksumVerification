using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

using CommandLine;
using Force.Crc32;
using K4os.Hash.xxHash;
using Standart.Hash.xxHash;
using HashDepot;


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
                    {
                        Scan(basePath, resultFile);
                    }

                    if (o.Verify && !o.Scan)
                    {
                        Verify(basePath, resultFile);
                    }
                });
        }

        // ====================================
        // Scan
        // ====================================
        private static void Scan(string basePath, string resultFile)
        {
            if (!Directory.Exists(basePath))
            {
                Console.WriteLine($"Directory {basePath} does not exist.");
                return;
            }

            string data = $"# Checksum for {basePath}" + Environment.NewLine;
            data += "# ===================================" + Environment.NewLine;

            DirectoryInfo directory = new(basePath);

            FileInfo[] files = directory.GetFiles("*", SearchOption.AllDirectories);

            var stopWatch = Stopwatch.StartNew();

            foreach (var file in files)
            {
                string relativePath = Path.GetRelativePath(basePath, file.FullName);

                Console.Write($"{relativePath} ");

                //string fileHash = GetSHA1(file.FullName);                
                //string fileHash = GetXXHash(file.FullName);
                string fileHash = GetCRC32(file.FullName);

                Console.WriteLine($"{fileHash} {file.Length}");

                data += $"{relativePath}|{fileHash}|{file.Length}" + Environment.NewLine;
            }

            stopWatch.Stop();

            string elapsedTime = MsToString(stopWatch.ElapsedMilliseconds);

            data += "# ===================================" + Environment.NewLine;
            data += $"# Done {files.Length} files in {elapsedTime}." + Environment.NewLine;

            File.WriteAllText(resultFile, data, new UTF8Encoding(false)); // UTF8 without BOM

            Console.WriteLine("===================================");
            Console.WriteLine($"Done {files.Length} files in {elapsedTime}.");
        }

        // ====================================
        // Verify
        // ====================================
        private static void Verify(string basePath, string resultFile)
        {
            if (!Directory.Exists(basePath) || !File.Exists(resultFile))
            {
                Console.WriteLine($"Directory {basePath} or file {resultFile} does not exist.");
                return;
            }

            string data = $"# Verification for {basePath}" + Environment.NewLine;
            data += "# ===================================" + Environment.NewLine;

            string[] files = File.ReadAllLines(resultFile, new UTF8Encoding(false));

            int fileCounter = 0;

            var stopWatch = Stopwatch.StartNew();

            foreach (var file in files)
            {
                if (file[0] != '#')
                {
                    string[] fileData = file.Split("|");

                    string relativePath = fileData[0];
                    string fileHash = fileData[1];
                    long fileLength = Convert.ToInt64(fileData[2]);

                    FileInfo fileInfo = new(Path.Combine(basePath, relativePath));

                    if (fileInfo.Exists)
                    {
                        string fileHashCopy = GetCRC32(fileInfo.FullName);
                        long fileLengthCopy = fileInfo.Length;

                        Console.Write($"{relativePath} {fileHashCopy} {fileLengthCopy}");

                        if ((fileHash == fileHashCopy) && (fileLength == fileLengthCopy))
                        {
                            Console.ForegroundColor = ConsoleColor.DarkGreen;
                            Console.WriteLine(" [OK]");
                            Console.ResetColor();
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine(" [DIFF]");
                            Console.ResetColor();

                            data += $"{relativePath}|{fileHashCopy}|{fileLengthCopy}" + Environment.NewLine;
                        }
                    }
                    else
                    {
                        Console.Write($"{relativePath}");
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine(" [NONE]");
                        Console.ResetColor();

                        data += $"{relativePath}" + Environment.NewLine;
                    }

                    fileCounter++;
                }
            }

            stopWatch.Stop();

            string elapsedTime = MsToString(stopWatch.ElapsedMilliseconds);

            data += "# ===================================" + Environment.NewLine;
            data += $"# Done {fileCounter} files in {elapsedTime}." + Environment.NewLine;

            File.WriteAllText("VERIFY_" + resultFile, data, new UTF8Encoding(false)); // UTF8 without BOM

            Console.WriteLine("===================================");
            Console.WriteLine($"Done {fileCounter} files in {elapsedTime}.");
        }

        // ====================================
        // Milliseconds to string
        // ====================================
        private static string MsToString(long milliseconds)
        {
            TimeSpan timeSpan = TimeSpan.FromMilliseconds(milliseconds);

            return string.Format("{0:D2}:{1:D2}:{2:D2}:{3:D3}",
                    timeSpan.Hours,
                    timeSpan.Minutes,
                    timeSpan.Seconds,
                    timeSpan.Milliseconds);
        }

        // ====================================
        // Get CRC32 hash
        // ====================================
        private static string GetCRC32(string file, int bufferSizeMB = 32)
        {
            using FileStream fileStream = new(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSizeMB * 1024 * 1024);

            Crc32CAlgorithm crc32C = new();
            var hash = crc32C.ComputeHash(fileStream);

            return Convert.ToHexString(hash);
        }

        // ====================================
        // Get SHA1 hash
        // ====================================
        private static string GetSHA1(string file, int bufferSizeMB = 32)
        {
            using FileStream fileStream = new(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSizeMB * 1024 * 1024);
            SHA1Managed sha1 = new();
            var hash = sha1.ComputeHash(fileStream);

            return Convert.ToHexString(hash);
        }

        // ====================================
        // Get xxHash hash
        // ====================================
        private static string GetXXHash(string file, int bufferSizeMB = 32)
        {
            using FileStream fileStream = new(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSizeMB * 1024 * 1024);

            // K4os.Hash.xxHash
            var hash = new XXH32().AsHashAlgorithm().ComputeHash(fileStream);
            return Convert.ToHexString(hash);

            // Standart.Hash.xxHash
            //var hash = xxHash32.ComputeHash(fileStream, bufferSizeMB * 1024 * 1024);
            //return hash.ToString("X2");

            // HashDepot
            //var hash = XXHash.Hash32(fileStream);
            //return hash.ToString("X2");
        }
    }
}
