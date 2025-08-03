using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.IO;
using System.Threading;
using InfoSong = (string artist, string songName, string songUrl, string urlArtist);

public class DownloadSong
{
    static IWebDriver SetupDriver(string downloadFolder)
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

    public static void Download(InfoSong info, string folder)
    {
        var driver = SetupDriver(folder);
        try
        {
            int currentCount = Directory.GetFiles(folder, "*.mp3").Length;
            driver.Navigate().GoToUrl(info.songUrl);

            while (currentCount + 1 != Directory.GetFiles(folder, "*.mp3").Length)
            {
                Thread.Sleep(500);
            }

            Console.WriteLine($"File save in: {folder}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            driver.Quit();
        }
    }
}