using Microsoft.Extensions.Configuration;
using System.Net;
using Test_1.Core;
using Test_1.Models;
using Test_1.Models.Dowloaders;
using Test_1.Models.SiteParse;
using Test_1.Models.TestData;

namespace Test_1.Services
{
    public class ProductManager
    {
        public ProductManager()
        {

        }

        public async Task<(string zipFile,List<ResultInfoSong> songs)> FindDownloadMusic(string nameSong)
        {
            string zipFile;
            List<InfoSong> songs;

            if (AppSetting.DEBUG_USE_LOCAL_DATA)
            {
                songs = Fake_SongData.Get(AppSetting.DEBUG_PATH_JSON);
                zipFile = AppSetting.DEBUG_PATH_ZIP;
            }
            else
            {
                songs = await ParseMuzofond.GetInfoSong(nameSong);
                songs.AddRange(await ParseMp3Party.GetInfoSong(nameSong));

                string mainFolder = Path.Combine(AppSetting.PATH_STORAGE, DateTime.Now.ToString("HH-mm-ss"));
                zipFile = mainFolder + ".zip";

                Directory.CreateDirectory(mainFolder);

                await Dowloader.DowloadFilesAsync(songs, mainFolder);

                Dowloader.CompresToZip(mainFolder,zipFile);
            }

            if (!File.Exists(zipFile))
                throw new FileLoadException("Failed to create file",zipFile);

            var sizeBytes = new FileInfo(zipFile).Length;
            if (sizeBytes < 1_000_000)
                throw new WebException($"Failed to download files. Real size -> {sizeBytes}");

            List<ResultInfoSong> downloadSongs = new List<ResultInfoSong>(songs.Count);

            string directoryPath = zipFile.Remove(zipFile.IndexOf("."));
            for (int i = 0; i < songs.Count; i++)
            {
                downloadSongs.Add(new());
                downloadSongs[i].data = songs[i];
                downloadSongs[i]._successDowload = CheckDownload(songs[i], directoryPath);
            }

            return (zipFile, downloadSongs);
        }

        private static bool CheckDownload(InfoSong song, string destinationFolder)
        {
            string filename = Path.Combine(destinationFolder, song.ToString());

            var flag1 = File.Exists(filename);

            if (!flag1)
                return false;

            return new FileInfo(filename).Length > 1_000_000; // 1 MB
        }
    }
}
