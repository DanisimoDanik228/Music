using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Music
{
    public class InfoSong
    {
        public InfoSong(string artist, string song, string songUrl, string urlArtist)
        {        
            this.artist = artist;
            this.songName = song;
            this.songUrl = songUrl;
            this.urlArtist = urlArtist;
        }
        public InfoSong()
        {
        }

        public string artist; 
        public string songName; 
        public string songUrl; 
        public string urlArtist;
        
        public override string ToString()
        {
            return $"{artist} __ {songName} __ {songUrl} __ {urlArtist}";
        }
    }
}
