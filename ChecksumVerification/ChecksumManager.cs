using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Force.Crc32;
using K4os.Hash.xxHash;
using Standart.Hash.xxHash;
using HashDepot;


namespace ChecksumVerification
{
    public class ChecksumManager
    {
        private string BasePath;
        private string ResultFile;

        public ChecksumManager(string basePath, string resultFile)
        {
            BasePath = basePath;
            ResultFile = resultFile;
        }

        // ====================================
        // Scan
        // ====================================
        public void Scan()
        {
            if (!Directory.Exists(BasePath))
            {
                Console.WriteLine($"Directory {BasePath} does not exist.");
                return;
            }

            string data = $"# Checksum for {BasePath}" + Environment.NewLine;
            data += "# ===================================" + Environment.NewLine;

            DirectoryInfo directory = new(BasePath);

            FileInfo[] files = directory.GetFiles("*", SearchOption.AllDirectories);

            var stopWatch = Stopwatch.StartNew();

            foreach (var file in files)
            {
                string relativePath = Path.GetRelativePath(BasePath, file.FullName);

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

            File.WriteAllText(ResultFile, data, new UTF8Encoding(false)); // UTF8 without BOM

            Console.WriteLine("===================================");
            Console.WriteLine($"Done {files.Length} files in {elapsedTime}.");
        }

        // ====================================
        // Verify
        // ====================================
        public void Verify()
        {
            if (!Directory.Exists(BasePath) || !File.Exists(ResultFile))
            {
                Console.WriteLine($"Directory {BasePath} or file {ResultFile} does not exist.");
                return;
            }

            string data = $"# Verification for {BasePath}" + Environment.NewLine;
            data += "# ===================================" + Environment.NewLine;

            string[] files = File.ReadAllLines(ResultFile, new UTF8Encoding(false));

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

                    FileInfo fileInfo = new(Path.Combine(BasePath, relativePath));

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

            File.WriteAllText("VERIFY_" + ResultFile, data, new UTF8Encoding(false)); // UTF8 without BOM

            Console.WriteLine("===================================");
            Console.WriteLine($"Done {fileCounter} files in {elapsedTime}.");
        }

        // ====================================
        // Milliseconds to string
        // ====================================
        private string MsToString(long milliseconds)
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
        private string GetCRC32(string file, int bufferSizeMB = 32)
        {
            using FileStream fileStream = new(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSizeMB * 1024 * 1024);

            Crc32CAlgorithm crc32C = new();
            var hash = crc32C.ComputeHash(fileStream);

            return Convert.ToHexString(hash);
        }

        // ====================================
        // Get SHA1 hash
        // ====================================
        private string GetSHA1(string file, int bufferSizeMB = 32)
        {
            using FileStream fileStream = new(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSizeMB * 1024 * 1024);
            
            SHA1Managed sha1 = new();
            var hash = sha1.ComputeHash(fileStream);

            return Convert.ToHexString(hash);
        }

        // ====================================
        // Get xxHash hash
        // ====================================
        private string GetXXHash(string file, int bufferSizeMB = 32)
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
