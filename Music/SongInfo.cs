using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.IO;

using InfoSong = (string artist, string songName, string songUrl, string urlArtist);
public class SongInfo
{
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

        return new ChromeDriver(service, options);
    }

    static (string artist, string song, string urlArtist) GetNameSong(IWebDriver driver)
    {
        var h1 = driver.FindElement(By.TagName("h1"));
        string[] parts = h1.Text.Trim().Split(new[] { " - " }, 2, StringSplitOptions.None);
        string artist = parts[0].Trim();
        string song = parts.Length > 1 ? parts[1].Trim() : "";
        string urlArtist = h1.FindElement(By.CssSelector("a")).GetAttribute("href");
        return (artist, song + ".mp3", urlArtist);
    }

    static InfoSong FindApi(string url)
    {
        var driver = SetupDriver();
        try
        {
            driver.Navigate().GoToUrl(url);
            Thread.Sleep(2000);

            var (artist, song, urlArtist) = GetNameSong(driver);

            string songUrl = driver.FindElement(By.CssSelector("a.b_btn.download.no-ajix[href*='/api/']")).GetAttribute("href");
            return (artist, song, songUrl, urlArtist);
        }
        finally
        {
            driver.Quit();
        }
    }

    public static string UbgradeUrl(string songName)
    {
        return $"https://sefon.pro/search/{songName.Replace(" ", "%20")}";
    }
    public static List<InfoSong> FindSongsInfo(string url, int count)
    {
        var driver = SetupDriver();
        try
        {
            driver.Navigate().GoToUrl(url);
            Thread.Sleep(2000);


            var songPages = driver.FindElements(By.CssSelector("a[href*='/mp3/']"));

            List<(string artist, string songName, string songUrl, string urlArtist)> res = new();

            for (int i = 0; i < count * 2 && i < songPages.Count(); i += 2)
                res.Add(FindApi(songPages[i].GetAttribute("href")));

            return res;
        }
        finally
        {
            driver.Quit();
        }
    }
}