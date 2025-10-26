using DotNetEnv;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using System.Text.Json;
using Test_1.Core;
using Test_1.Models;
using Test_1.Services;

namespace Test_1.Test
{
    [TestFixture]
    public class UnitTest
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Env.Load("C:\\Users\\Werty\\source\\repos\\Code\\C#\\Music\\Test_1\\Core\\.env");
            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();
            AppSetting.Ctor(configuration);
        }

        [Test]
        public async Task StartTestEngine()  
        {
            var name = "Love";

            var engine = new ProductManager();
            var res = await engine.FindDownloadMusic(name);

            var folderWithZip = res.zipFile.Remove(res.zipFile.IndexOf("."));
            var files = Directory.GetFiles(folderWithZip);
            long sizeZip = new FileInfo(res.zipFile).Length;

            int countFindSongs = res.songs.Count;
            int countDownloadSongs = files.Length;

            Assert.That(countDownloadSongs == countFindSongs);

            if (countFindSongs * 1_000_000 > sizeZip)  
                Assert.Fail("Failed download files");
        }

        //[Test]
        //public void TestSerializerDeserializer()
        //{
        //    string jsonFile = AppSetting.DEBUG_PATH_JSON;

        //    string json_1 = File.ReadAllText(jsonFile);
        //    List<InfoSong> list = JsonSerializer.Deserialize<List<InfoSong>>(json_1) ?? new();
        //    string json_2 = JsonSerializer.Serialize(list);

        //    Assert.That(json_1 == json_2);
        //}
    }
}