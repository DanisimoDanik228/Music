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

        public string artist;
        public string songName;
        public string songUrl;
        public string artistUrl;
        public string dowloadLink;

        public override string ToString()
        {
            return $"{artist} __ {songName} __ {songUrl} __ {artistUrl}";
        }
    }
}
