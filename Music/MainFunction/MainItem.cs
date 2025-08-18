using Music.PostgresSQL;
using OpenQA.Selenium.DevTools.V136.DOM;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using TagLib.Ape;
using TagLib.Mpeg;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Music.MainFunction
{
    using Music.PostgresSQL;
    public static class DictionaryKeyboardMarkup
    {
        private static Dictionary<string, (int left, int right)> existGroup = new();
        private static int currentCountMembers = 0;
        private static List<string> data = new List<string>();
        public static InlineKeyboardMarkup CreateInlineKeyboard(IEnumerable<(string name, string data)> mas, string nameGroup)
        {
            if (existGroup.ContainsKey(nameGroup))
                throw new ArgumentException($"This group {nameGroup} already exist");

            int sizeGroup = mas.Count();
            int left = currentCountMembers;
            currentCountMembers += sizeGroup;

            existGroup[nameGroup] = (left, left + sizeGroup - 1);

            List<InlineKeyboardButton[]> result = new();

            foreach (var item in mas)
            {
                data.Add(item.data);
                result.Add([InlineKeyboardButton.WithCallbackData(item.name, left.ToString())]);

                left++;
            }

            return new InlineKeyboardMarkup(result);
        }
        public static string GetNameGroup(int numItem)
        {
            foreach (var item in existGroup)
            {
                if ((item.Value.left <= numItem) && (numItem <= item.Value.right))
                    return item.Key;
            }

            throw new ArgumentException($"Group for {numItem} don't exist");
        }
        public static string GetData(int numItem)
        {
            return data[numItem];
        }
        public static int GetNumItem(Update data)
        {
            return int.Parse(data.CallbackQuery.Data);
        }
        public static (int left, int right) GetRangeGroup(string nameGroup)
        {
            if (!existGroup.ContainsKey(nameGroup))
                throw new ArgumentException($"This group {nameGroup} dont exist");

            return existGroup[nameGroup];
        }
        public static void Clear()
        {
            existGroup.Clear();
            currentCountMembers = 0;
            data.Clear();
        }
    }
    public static class MainItem
    {
        public static string GetUniqueFileName(string path)
        {
            string directory = Path.GetDirectoryName(path);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
            string extension = Path.GetExtension(path);
            string newPath = path;

            int count = 1;

            while (System.IO.File.Exists(newPath))
            {
                string tempFileName = $"{fileNameWithoutExtension} ({count}){extension}";
                newPath = Path.Combine(directory, tempFileName);
                count++;
            }

            return newPath;
        }

        public static string currentDir = Directory.GetCurrentDirectory();
        public static string directoryDowload = Path.Combine(currentDir, "DowloadFiles");
        public static string webStorage = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ServerMusic", "uploads");
        public static string CurrentTime() => DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff");

        public static void Write(string text)
        {
            var t = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(text);

            Console.ForegroundColor = t;
        }
        public static void WriteLine(string text)
        {
            var t = Console.BackgroundColor;

            Console.BackgroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(text);

            Console.BackgroundColor = t;
        }

        public static string Serialize(InfoSong info)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(InfoSong));
            using (var stringWriter = new StringWriter())
            {
                serializer.Serialize(stringWriter, info);
                string infoSong = stringWriter.ToString();

                return infoSong;
            }
        }
        public static InfoSong DeSerialize(string xmlData)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(InfoSong));
            InfoSong info;

            using (var stringReader = new StringReader(xmlData))
                info = (InfoSong)serializer.Deserialize(stringReader);

            return info;
        }

        public static SongDowloaderSefon songDowloaderSefon = new(webStorage);
        public static SongDowloaderMp3Party songDowloaderMp3Party = new(webStorage);
        public static SongDowloaderMuzofond songDowloaderMuzofond = new(webStorage);

        public static List<InfoSong> GetInfoSongFromAllSource(string inputName, int count)
        {
            List<InfoSong> res = new();

            Parallel.Invoke(
                () => res.AddRange(songDowloaderSefon.GetInfoSong(inputName, count)),
                () => res.AddRange(songDowloaderMp3Party.GetInfoSong(inputName, count)),
                () => res.AddRange(songDowloaderMuzofond.GetInfoSong(inputName, count))
            );

            return res;
        }

        public static string DowloadMusicFromAllSource(InfoSong info, string destinationDowloadFolder)
        {
            return songDowloaderMp3Party.DowloadMusic(info, destinationDowloadFolder);
        }

        public static void CopyAllFilesToStorageServerFromAllSource()
        {
            songDowloaderMp3Party.CopyAllFilesToStorageServer();
            songDowloaderMuzofond.CopyAllFilesToStorageServer();
            songDowloaderSefon.CopyAllFilesToStorageServer();
        }
        public static bool IsProcessRunning(string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            return processes.Length > 0;
        }

        public static string MakeFingerprint(string filePathSong)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "fpcalc",
                    Arguments = $"\"{filePathSong}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            int fingerprintIndex = output.IndexOf("FINGERPRINT=");
            return output.Substring(fingerprintIndex + "FINGERPRINT=".Length).Trim();
        }

        public static PostgresDateBase repo = new PostgresDateBase("Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=76127612d");

    }
    public static class NetworkItem
    {
        public const int _Port = 8080;
        public static string _urlWebStorage;

        public static IPAddress GetZeroTierIp()
        {
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface ni in interfaces)
            {
                if (ni.Description.Contains("ZeroTier"))
                {
                    IPInterfaceProperties ipProps = ni.GetIPProperties();
                    UnicastIPAddressInformationCollection unicastAddresses = ipProps.UnicastAddresses;

                    var ip = unicastAddresses.First().Address;
                    foreach (UnicastIPAddressInformation addr in unicastAddresses)
                    {
                        if (addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            return addr.Address;
                        }
                    }
                }
            }

            return null;
        }

        public static async Task<bool> PingUrl(string url)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));
                    return response.IsSuccessStatusCode; 
                }
            }
            catch
            {
                return false; 
            }
        }
    }
}
