using System.Text.Json;
using Test_1.Models;

namespace Test_1.Models.TestData
{
    public class Fake_SongData
    {
        public static List<InfoSong> Get(string filenameJson)
        {
            var json = File.ReadAllText(filenameJson);

            var res = JsonSerializer.Deserialize<List<InfoSong>>(json);

            if (res == null)
            {
                throw new Exception("Invalid fake_songdata.json");
            }

            return res;
        }
    }
}
