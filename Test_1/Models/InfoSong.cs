using System.Text.Json.Serialization;
using Test_1.Models.Enum;

namespace Test_1.Models
{
    public class InfoSong
    {
        [JsonConstructor]
        public InfoSong(SourseSite sourseSite)
        {
            this.sourseSite = sourseSite;
        }

        public string artist { get; set; }
        public string songName { get; set; }
        public string songUrl { get; set; }
        public string artistUrl { get; set; }
        public string dowloadLink { get; set; }
        // May be nust used string
        public SourseSite sourseSite { get; set; }

        public override string ToString()
        {
            return $"{artist} __ {songName}";
        }
    }
}
