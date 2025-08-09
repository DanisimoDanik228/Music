using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Music
{
    public class InfoSong(string artist, string song, string songUrl, string urlArtist)
    {
        public string artist = artist; 
        public string songName = song; 
        public string songUrl = songUrl; 
        public string urlArtist = urlArtist;
    }
}
