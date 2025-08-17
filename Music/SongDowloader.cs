using Music.MainFunction;
using MusicBot;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TagLib.Riff;
using Telegram.Bot.Types;

namespace Music
{
    public abstract class AbstractSongDowloader
    {
        protected string webStorage;

        protected Stack<string> dowloadedFiles;
        protected const int MaxCountSongForSearchSong = 5;

        protected AbstractSongDowloader(string webStorage)
        {
            dowloadedFiles = new();
            this.webStorage = webStorage;
            
            if(!Directory.Exists(this.webStorage))
               Directory.CreateDirectory(this.webStorage);
        }

        protected void FixTitleName(string filePath, InfoSong info)
        {
            var file = TagLib.File.Create(filePath);
            file.Tag.Title = info.songName;
            file.Tag.Artists = [info.artist];
            file.Save();
        }

        public abstract List<InfoSong> GetInfoSong(string inputName, int count);
        public abstract string CreateUrlForSearch(string inputName);
        public void CopyAllFilesToStorageServer()
        {
            while (dowloadedFiles.Any())
            {
                var filePath = dowloadedFiles.Pop();

                var filename = Path.GetFileName(filePath);

                var destination = Path.Combine(webStorage, filename);

                if (!System.IO.File.Exists(destination))
                { 
                    System.IO.File.Copy(filePath , destination);
                    //MainItem.WriteLine($"File copy from: {filePath} to: {destination}");
                }
            }
        }

        public static IWebDriver SetupDriver()
        {
            ChromeDriverService service = ChromeDriverService.CreateDefaultService();
            service.EnableVerboseLogging = false;
            service.SuppressInitialDiagnosticInformation = true;
            service.HideCommandPromptWindow = true;

            ChromeOptions options = new ChromeOptions();

            options.PageLoadStrategy = PageLoadStrategy.Normal;

            options.AddArgument("--window-size=1920,1080");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--headless");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--disable-crash-reporter");
            options.AddArgument("--disable-extensions");
            options.AddArgument("--disable-in-process-stack-traces");
            options.AddArgument("--disable-logging");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--log-level=3");
            options.AddArgument("--output=/dev/null");
            options.AddExcludedArgument("enable-logging");

            return new ChromeDriver(options);
        }
        public string DowloadMusic(InfoSong info, string destinationDowloadFolder)
        {
            using (var client = new WebClient())
            {
                var filename = $"{info.artist} - {info.songName}";
                var fullPath = Path.Combine(destinationDowloadFolder, filename) + ".mp3";
                client.DownloadFile(info.songUrl, fullPath);

                try
                { 
                    FixTitleName(fullPath,info);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error in FixTitleName: " + ex.Message);
                }

                dowloadedFiles.Push(fullPath);

                return fullPath;
            } 
        }


        private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();
        protected static string SanitizeFileName(string fileName, char replacementChar = ' ')
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
    }

    public class SongDowloaderSefon : AbstractSongDowloader
    {
        public SongDowloaderSefon(string webStorage):base(webStorage)
        {
        }

        private static InfoSong FindApi(string url)
        {
            var driver = SetupDriver();
            try
            {
                driver.Navigate().GoToUrl(url);
                InfoSong info = new();

                var h1 = driver.FindElement(By.TagName("h1"));
                string[] parts = SanitizeFileName(h1.Text).Trim().Split(new[] { " - " }, 2, StringSplitOptions.None);
                info.artist = parts[0].Trim();
                info.songName = parts.Length > 1 ? parts[1].Trim() : "";
                info.urlArtist = h1.FindElement(By.CssSelector("a")).GetAttribute("href");
                info.songUrl = driver.FindElement(By.CssSelector("a.b_btn.download.no-ajix[href*='/api/']")).GetAttribute("href");

                return info;
            }
            finally
            {
                driver.Quit();
            }
        }
        public override List<InfoSong> GetInfoSong(string inputName, int count)
        {
            var driver = SetupDriver();
            try
            {
                List<InfoSong> res = new();
                driver.Navigate().GoToUrl(CreateUrlForSearch(inputName));

                var mainSection = driver.FindElements(By.CssSelector("div.main"));

                if (mainSection.Count == 0)
                    return [];

                var songPages = mainSection[0].FindElements(By.CssSelector("a[href*='/mp3/']"));

                for (int i = 0; i < count * 2 && i < songPages.Count() && i < 2 * MaxCountSongForSearchSong; i += 2)
                    res.Add(FindApi(songPages[i].GetAttribute("href")));

                return res;
            }
            finally
            {
                driver.Quit();
            }
        }
        public override string CreateUrlForSearch(string inputName)
        {
            return $"https://sefon.pro/search/{inputName.Replace(" ", "%20")}";
        }
    }

    public class SongDowloaderMp3Party : AbstractSongDowloader
    {
        public SongDowloaderMp3Party(string webStorage) : base(webStorage)
        {
        }

        public override List<InfoSong> GetInfoSong(string inputName, int count)
        {
            var driver = SetupDriver();
            try
            {
                List<InfoSong> res = new();
                driver.Navigate().GoToUrl(CreateUrlForSearch(inputName));

                var mainSection = driver.FindElements(By.CssSelector("div.playlist"));

                if (mainSection.Count == 0)
                    return [];

                var songPages = mainSection[0].FindElements(By.CssSelector("a[href*='/music/']"));

                for (int i = 0; i < count * 2 && i < songPages.Count() && i < 2 * MaxCountSongForSearchSong; i +=2)
                {
                    InfoSong info = new();

                    string fullSongName = songPages[i].Text;

                    string[] parts = SanitizeFileName(fullSongName).Trim().Split(new[] { " - " }, 2, StringSplitOptions.None);

                    info.artist = parts[0];
                    if(parts.Length > 1)
                        info.songName = parts[1];

                    string urlMusic = songPages[i].GetAttribute("href");
                    string linkDowload = MakeDowloadLink(urlMusic);

                    info.songUrl = linkDowload;
                    info.urlArtist = "__ notdef __";

                    res.Add(info);
                }

                return res;
            }
            finally
            {
                driver.Quit();
            }
        }
        private static string MakeDowloadLink(string url)
        {
            string id = url.Split('/').Last();

            return $"https://dl1.mp3party.net/download/{id}";
        }

        public override string CreateUrlForSearch(string inputName)
        {
            return $"https://mp3party.net/search?q={inputName.Replace(" ", "%20")}";
        }
    }

    public class SongDowloaderMuzofond : AbstractSongDowloader
    {
        public SongDowloaderMuzofond(string webStorage) : base(webStorage)
        {
        }

        public override string CreateUrlForSearch(string inputName)
        {
            return $"https://muzofond.fm/search/{inputName.Replace(" ","%20")}";
        }

        public override List<InfoSong> GetInfoSong(string inputName, int count)
        {
            var driver = SetupDriver();
            try
            {
                List<InfoSong> res = new();
                driver.Navigate().GoToUrl(CreateUrlForSearch(inputName));

                var mainSection = driver.FindElements(By.CssSelector("ul.mainSongs.unstyled.songs"));

                if (mainSection.Count == 0)
                    return [];

                var songPages = mainSection[0].FindElements(By.CssSelector("li.item"));

                for (int i = 0; i < count && i < songPages.Count() && i < MaxCountSongForSearchSong; i ++)
                {
                    InfoSong info = new();

                    info.urlArtist = "__ not def __";
                    info.songUrl = songPages[i].FindElement(By.CssSelector("li.play")).GetAttribute("data-url");
                    info.artist = SanitizeFileName(songPages[i].FindElement(By.CssSelector("span.artist")).Text);
                    info.songName = SanitizeFileName(songPages[i].FindElement(By.CssSelector("span.track")).Text);

                    res.Add(info);
                }

                return res;
            }
            finally
            {
                driver.Quit();
            }
        }
    }
}