using System.Text.Json;
using Test_1.Models;

namespace Test_1.TestData
{
    public class Fake_SongData
    {
        public static List<InfoSong> Get()
        {
            var json = File.ReadAllText(@"TestData\fake_songdata.json");

            var res = JsonSerializer.Deserialize<List<InfoSong>>(json);

            if (res == null)
            {
                throw new Exception("Invalid fake_songdata.json");
            }

            return res;
        }
    }
}
