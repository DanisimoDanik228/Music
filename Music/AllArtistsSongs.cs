using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class AllArtistsSongs
{
    public static void Dowloads(string urlArtist,string folder)
    {
        var info = SongInfo.FindSongsInfo(urlArtist,3);

        foreach (var item in info)
        {
            Console.WriteLine(item);
            Console.WriteLine(item.songName);
            DownloadSong.Download(item,folder);
        }
    }
}

