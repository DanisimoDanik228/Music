namespace Test_1.Models
{
    public class InfoSong
    {
        public InfoSong(string artist, string song, string songUrl, string urlArtist)
        {
            this.artist = artist;
            this.songName = song;
            this.songUrl = songUrl;
            this.artistUrl = urlArtist;
        }

        public InfoSong()
        {
            
        }

        public string artist { get; set; }
        public string songName { get; set; }
        public string songUrl { get; set; }
        public string artistUrl { get; set; }
        public string dowloadLink { get; set; }

        public override string ToString()
        {
            return $"{artist} __ {songName} __ {songUrl} __ {artistUrl}";
        }
    }
}
