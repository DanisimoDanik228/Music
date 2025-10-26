using Microsoft.AspNetCore.Components.Sections;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Text;
using Test_1.Models;

namespace Test_1.Models.Dowloaders
{
    public static class Dowloader
    {
        private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();
        private static string SanitizeFileName(string fileName, char replacementChar = ' ')
        {
            if (string.IsNullOrEmpty(fileName))
                return fileName;

            var sanitized = new StringBuilder(fileName.Length);

            foreach (char c in fileName)
            {
                if (Array.IndexOf(InvalidFileNameChars, c) >= 0)
                    sanitized.Append(replacementChar);
                else
                    sanitized.Append(c);
            }

            return sanitized.ToString();
        }
        public static string CompresToZip(string folder)
        {
            var parentFolder = Directory.GetParent(folder).FullName;
            var zipFile = Path.Combine(parentFolder, DateTime.Now.ToString("HH-mm-ss")+".zip");

            ZipFile.CreateFromDirectory(folder, zipFile, CompressionLevel.Optimal, includeBaseDirectory: false);

            return zipFile;
        }

        public static async Task DowloadFilesAsync(IEnumerable<InfoSong> urls,string destination)
        {
            const int maxConcurrent = 5;

            var semaphore = new SemaphoreSlim(maxConcurrent);
            var tasks = urls.Select(async (info) =>
            {
                await semaphore.WaitAsync();
                try
                {
                    await DowloadFileAsync(info, destination);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
        }

        public async static Task<string> DowloadFileAsync(InfoSong info, string destination)
        {
            var safeFilename = $"{SanitizeFileName(info.artist)} - {SanitizeFileName(info.songName)}.mp3";
            var fullPath = Path.Combine(destination, safeFilename);

            using (var client = new HttpClient())
            {
                try
                {
                    client.Timeout = TimeSpan.FromSeconds(20);
                  
                    byte[] fileBytes = await client.GetByteArrayAsync(info.dowloadLink);
                    await File.WriteAllBytesAsync(fullPath, fileBytes);

                    try
                    {
                        //FixTitleName(fullPath, info);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error in FixTitleName: " + ex.Message);
                    }

                    //dowloadedFiles.Push(fullPath);
                    return fullPath;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error downloading file: " + ex.Message);
                    throw;
                }
            }
        }
    }
}
