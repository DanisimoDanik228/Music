using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Music
{
    public class InfoSong(string artist, string song, string songUrl, string urlArtist, string originalNameSong = "_not def_")
    {
        public string artist = artist; 
        public string songName = song; 
        public string songUrl = songUrl; 
        public string urlArtist = urlArtist;
        public string originalNameSong = originalNameSong;

        public override string ToString()
        {
            return $"{originalNameSong}";
        }
    }
}
