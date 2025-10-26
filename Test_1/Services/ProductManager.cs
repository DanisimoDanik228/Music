using Microsoft.Extensions.Configuration;
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

        public async Task<(string zipFile, List<InfoSong> songs)> FindMusic(string nameSong, IConfiguration _configuration)
        {
            string zipFile;
            List<InfoSong> songs;

            if (_configuration["DEBUG_USE_LOCAL_DATA"] == "true")
            {
                songs = Fake_SongData.Get(_configuration["DEBUG_PATH_JSON"]);
                zipFile = _configuration["DEBUG_PATH_ZIP"];
            }
            else
            {
                songs = await ParseMuzofond.GetInfoSong(nameSong);

                var destinationFolder = @"C:\Users\Werty\Desktop\test\" + Guid.NewGuid().ToString();

                Directory.CreateDirectory(destinationFolder);

                await Dowloader.DowloadFilesAsync(songs, destinationFolder);

                zipFile = Dowloader.CompresToZip(destinationFolder);
            }

            return (zipFile, songs);
        }
    }
}
