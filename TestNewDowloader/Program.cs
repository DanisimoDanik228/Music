using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.DevTools.V137.Network;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

class Program
{
    public static string CreateUrlForSearchMp3Party(string songName) => $"https://mp3party.net/search?q={songName.Replace(" ", "%20")}";

    static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        AudioFingerprintComparer.start(["Повод2.mp3", "Повод1.mp3"]);

        string name = "Пчеловод";

        var url = CreateUrlForSearchMp3Party(name);

        FindApi(url);
    }


    private static void FindApi(string url)
    {
        var driver = SetupDriver();
        try
        {
            driver.Navigate().GoToUrl(url);

            var songUrl = driver.FindElement(By.CssSelector("div.playlist"));
            var a = songUrl.FindElements(By.CssSelector("a.track__title.js-track-title"));

            int i = 0;
            foreach (var item in a)
            {
                var link = item.GetAttribute("href");
                i++;

                string linkDowload = MakeDowloadLink(link);

                DownloadFile(linkDowload, path, i.ToString() + ".mp3");
            }
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

    static void DownloadFile(string url, string path, string filename)
    {
        using (var client = new WebClient())
        {
            var fullPath = Path.Combine(path, filename);
            client.DownloadFile(url, fullPath);

            var file = TagLib.File.Create(fullPath);
            var name = file.Tag.Title;
            var artist = file.Tag.Artists.First();
            file.Save();

            System.IO.File.Move(fullPath,Path.Combine(path, $"{artist} - {name}.mp3"));
        }
    }
}


class AudioFingerprintComparer
{
    public static void start(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: AudioCompare <file1.mp3> <file2.mp3>");
            return;
        }

        string file1 = args[0];
        string file2 = args[1];

        try
        {
            string fp1 = GetFingerprint(file1);
            string fp2 = GetFingerprint(file2);

            double similarity = CalculateSimilarity(fp1, fp2);
            Console.WriteLine($"Сходство: {similarity * 100:F2}%");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
        }
    }
    private static string GetFingerprint(string audioFile)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "fpcalc",
                Arguments = $"\"{audioFile}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new Exception($"fpcalc завершился с ошибкой (код {process.ExitCode})");
        }

        int fingerprintIndex = output.IndexOf("FINGERPRINT=");
        if (fingerprintIndex == -1)
        {
            throw new Exception("Не удалось найти отпечаток в выводе fpcalc");
        }

        return output.Substring(fingerprintIndex + "FINGERPRINT=".Length).Trim();
    }
    private static double CalculateSimilarity(string a, string b)
    {
        if (a == b) return 1.0;

        int maxLength = Math.Max(a.Length, b.Length);
        if (maxLength == 0) return 1.0;

        int distance = LevenshteinDistance(a, b);
        return 1.0 - (double)distance / maxLength;
    }
    private static int LevenshteinDistance(string a, string b)
    {
        int[,] matrix = new int[a.Length + 1, b.Length + 1];

        for (int i = 0; i <= a.Length; i++)
            matrix[i, 0] = i;

        for (int j = 0; j <= b.Length; j++)
            matrix[0, j] = j;

        for (int i = 1; i <= a.Length; i++)
        {
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = (a[i - 1] == b[j - 1]) ? 0 : 1;

                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[a.Length, b.Length];
    }
}