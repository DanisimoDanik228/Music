using Microsoft.AspNetCore.Components.Sections;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using Test_1.Models;

namespace Test_1.Dowloaders
{

    public static class Dowloader
    {
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
            using (var client = new HttpClient())
            {
                var filename = $"{info.artist} - {info.songName}";
                var fullPath = Path.Combine(destination, filename) + ".mp3";

                try
                {
                    // Скачивание файла
                    byte[] fileBytes = await client.GetByteArrayAsync(info.songUrl);
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
