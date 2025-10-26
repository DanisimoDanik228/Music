using System.Text;
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
        // May be. Must used string
        public SourseSite sourseSite { get; set; }


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
        public override string ToString()
        {
            return $"{SanitizeFileName(this.artist)} - {SanitizeFileName(this.songName)}.mp3";
        }
    }
}
