using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Music;


namespace MusicBot
{
    public class AllArtistsSongs
    {
        private const int _countSongForArtist = 20;
        public static List<string> Dowloads(string urlArtist, string folder, string tempArtistFolder)
        {
            var info = SongInfo.FindSongsInfo(urlArtist, _countSongForArtist);
            List<string> res = new();

            foreach (var item in info)
            {
                Console.WriteLine(item);
                Console.WriteLine(item.songName);
                var path = DownloadSong.Download(item, folder, tempArtistFolder);
                res.Add(path);
            }

            return res;
        }
    }

}