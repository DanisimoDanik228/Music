using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        string name = "Лужа";

        var urlSong = SongInfo.UbgradeUrl(name);
        var info = SongInfo.FindSongsInfo(urlSong, 1).First();

        string folder = Path.Combine(@"C:\Users\Werty\source\repos\Code\C#\Music\DowloadedSongs", info.songName);

        string chars = "\\/:*?\"<>|";
        char[] newNameArtist = info.artist.ToCharArray();

        for (int i = 0; i < newNameArtist.Length; i++)
            if (chars.Contains(newNameArtist[i]))
                newNameArtist[i] = '_';

        string folderArtist = Path.Combine(@"C:\Users\Werty\source\repos\Code\C#\Music\DowloadedSongs\Songers", new string(newNameArtist));

        DownloadSong.Download(info.songUrl, folder);
        AllArtistsSongs.Dowloads(info.urlArtist, folderArtist);


        ZipFile.CreateFromDirectory(folder, $"{folder}.zip");
        ZipFile.CreateFromDirectory(folderArtist, $"{folderArtist}.zip");

        //return ($"{folder}.zip", $"{folderArtist}.zip");
    }
}