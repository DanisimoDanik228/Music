using Music.MainFunction;
using MusicBot;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TagLib.Riff;
using Telegram.Bot.Types;

namespace Music
{
    public class SongDowloader
    {
        private const string webStorage = @"C:\Users\Werty\source\repos\Code\C#\Server\HttpServer\bin\Debug\net8.0\uploads";
        public readonly string mainFolder;
        public readonly string mainFolderArtist;
        private Stack<string> listOfMustCopyFiles;

        public SongDowloader(string userFolderName)
        {
            listOfMustCopyFiles = new Stack<string>();

            if (!Directory.Exists(userFolderName))
                Directory.CreateDirectory(userFolderName);

            mainFolder = Path.Combine(userFolderName, MainItem.CurrentTime());
            if (!Directory.Exists(mainFolder))
                Directory.CreateDirectory(mainFolder);

            mainFolderArtist = Path.Combine(mainFolder, "Artist");
            if (!Directory.Exists(mainFolderArtist))
                Directory.CreateDirectory(mainFolderArtist);
        }
        private const int MaxCountSongForSearchSong = 10;
        private const int MaxCountSongForArtistSong = 20;



        public static string CreateUrlForSearch(string songName) => $"https://sefon.pro/search/{songName.Replace(" ", "%20")}";
        private static InfoSong GetFirstNameSong(IWebDriver driver)
        {
            var h1 = driver.FindElement(By.TagName("h1"));
            string[] parts = h1.Text.Trim().Split(new[] { " - " }, 2, StringSplitOptions.None);
            string artist = parts[0].Trim();
            string song = parts.Length > 1 ? parts[1].Trim() : "";
            string urlArtist = h1.FindElement(By.CssSelector("a")).GetAttribute("href");


            return new InfoSong(
                artist: artist, 
                song: song + ".mp3", 
                songUrl: null,
                urlArtist: urlArtist);
        }
        private static string GetFilenameFromUrl(string url)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                return Path.GetFileNameWithoutExtension(response.ResponseUri.LocalPath);
            }
        }
        private static InfoSong FindApi(string url)
        {
            var driver = SetupDriver();
            try
            {
                driver.Navigate().GoToUrl(url);

                var tempInfoSong = GetFirstNameSong(driver);

                string songUrl = driver.FindElement(By.CssSelector("a.b_btn.download.no-ajix[href*='/api/']")).GetAttribute("href");

                tempInfoSong.songUrl = songUrl;
                tempInfoSong.originalNameSong = GetFilenameFromUrl(songUrl);
                return tempInfoSong;
            }
            finally
            {
                driver.Quit();
            }
        }
        static IWebDriver SetupDriver()
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
        static IWebDriver SetupDriverForDowload(string downloadFolder)
        {
            if (!Directory.Exists(downloadFolder))
                Directory.CreateDirectory(downloadFolder);

            string fullPath = Path.GetFullPath(downloadFolder);

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

            options.AddUserProfilePreference("download.default_directory", fullPath);
            options.AddUserProfilePreference("download.prompt_for_download", false);
            options.AddUserProfilePreference("disable-popup-blocking", "true");
            options.AddUserProfilePreference("safebrowsing.enabled", "true");

            return new ChromeDriver(options);
        }
        public static List<InfoSong> FindSongsInfoFromUrl(string urlToFind, int count = 1)
        {
            var driver = SetupDriver();

            try
            {
                driver.Navigate().GoToUrl(urlToFind);

                var mainSection = driver.FindElement(By.CssSelector("div.main"));
                var songPages = mainSection.FindElements(By.CssSelector("a[href*='/mp3/']"));

                if (songPages.Count == 0)
                    return [];

                List<InfoSong> res = new();

                for (int i = 0; i < count * 2 && i < songPages.Count(); i += 2)
                    res.Add(FindApi(songPages[i].GetAttribute("href")));

                return res;
            }
            finally
            {
                driver.Quit();
            }
        }
        public List<string> DowloadSongsArtistToFolder(string urlArtist)
        {
            var info = FindSongsInfoFromUrl(urlArtist, MaxCountSongForArtistSong);
            List<string> res = new();

            foreach (var item in info)
            {
                var path = DownloadSongToFolder(item, mainFolderArtist);
                res.Add(path);
            }

            return res;
        }
        private static void FixTitleName(string filename, string name)
        {
            var file = TagLib.File.Create(filename);
            file.Tag.Title = name;
            file.Save();
        }
        public string DownloadSongToFolder(InfoSong info, string folderToDowload)
        {
            var driver = SetupDriverForDowload(folderToDowload);
            try
            {
                int currentCount = Directory.GetFiles(folderToDowload, "*.mp3").Length;
                driver.Navigate().GoToUrl(info.songUrl);

                while (currentCount + 1 != Directory.GetFiles(folderToDowload, "*.mp3").Length)
                    Thread.Sleep(500);

                var dowloadedFilename = Path.Combine(folderToDowload, GetFilenameFromUrl(info.songUrl) + ".mp3");
                //var resultFilename = Path.Combine(folderToDowload, info.songName + ".mp3");
                //System.IO.File.Move(dowloadedFilename, resultFilename);

                FixTitleName(dowloadedFilename,info.songName);

                listOfMustCopyFiles.Push(dowloadedFilename);

                return dowloadedFilename;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                driver.Quit();
            }

            return "";
        }
        public void CopyAllFilesToStorageServer()
        {
            while (listOfMustCopyFiles.Any())
            {
                var filename = listOfMustCopyFiles.Pop();

                var destinationFilename = Path.Combine(webStorage, System.IO.Path.GetFileName(filename));

                if (!System.IO.File.Exists(destinationFilename))
                    System.IO.File.Copy(filename, Path.Combine(webStorage,System.IO.Path.GetFileName(filename)));
            }
        }
    }
}