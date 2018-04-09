using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using CommandLine;

namespace BookScanDownloader
{
    class Options
    {
        [Option('h', "host", Required = true, HelpText = "Host address.ex. ftp://ftp154.bookscan.co.jp:8021/")]
        public string Host { get; set; }

        [Option('n', "name", Required = true, HelpText = "Your account name.")]
        public string UserName { get; set; }

        [Option('p', "password", Required = true, HelpText = "Your account password.")]
        public string Password { get; set; }

        [Option('l', "localpath", Required = true, HelpText = "Destination local path.")]
        public string LocalPath { get; set; }
    }

    class Program
    {
        private static Options _options;

        static void Main(string[] args)
        {
            var result = Parser.Default.ParseArguments<Options>(args).WithParsed(x => RunOptionsAndRetrunExitCode(x));

            if (result.Tag == ParserResultType.Parsed)
            {
                var host = _options.Host;
                if (!host.EndsWith("/"))
                {
                    host += "/";
                }

                var credential = new NetworkCredential(_options.UserName, _options.Password);
                var localPath = _options.LocalPath;

                try
                {
                    DownloadFtpDirectory(host, credential, localPath);
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(e);
                }
            }
        }

        static int RunOptionsAndRetrunExitCode(Options options)
        {
            _options = options;
            return 0;
        }

        private static void DownloadFtpDirectory(string uri, NetworkCredential credential, string localPath)
        {
            FtpWebRequest request = (FtpWebRequest) WebRequest.Create(uri);
            request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
            request.Credentials = credential;

            List<string> list = new List<string>();

            using (FtpWebResponse response = (FtpWebResponse) request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    list.Add(reader.ReadLine());
                }
            }

            foreach(var line in list)
            {
                string[] tokens = line.Split(new[] {' '}, 9, StringSplitOptions.RemoveEmptyEntries);
                string name = tokens[8];
                string permissions = tokens[0];

                string localFilePath = Path.Combine(localPath, name);
                var fileUri = uri + Uri.EscapeDataString(name);

                if (permissions[0] == 'd')
                {
                    if (!Directory.Exists(localFilePath))
                    {
                        Directory.CreateDirectory(localFilePath);
                    }

                    DownloadFtpDirectory(fileUri + "/", credential, localFilePath);
                }
                else
                {
                    Console.WriteLine($"{name}");

                    if (!File.Exists(localFilePath))
                    {
                        DownloadFtpFile(fileUri, credential, localFilePath);
                    }
                }
            }
        }

        private static void DownloadFtpFile(string fileUri, NetworkCredential credential, string localFilePath)
        {
            FtpWebRequest downloadRequest = (FtpWebRequest) WebRequest.Create(fileUri);
            downloadRequest.Method = WebRequestMethods.Ftp.DownloadFile;
            downloadRequest.Credentials = credential;

            using (FtpWebResponse response = (FtpWebResponse) downloadRequest.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (Stream fileStream = File.Create(localFilePath))
            {

                byte[] buffer = new byte[10240];
                int read;
                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    fileStream.Write(buffer, 0, read);
                }
            }
        }
    }
}
