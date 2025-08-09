using OpenQA.Selenium.DevTools.V136.Network;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.IO.Pipelines;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Music;
using OpenQA.Selenium.DevTools.V136.Debugger;


namespace MusicBot
{
    partial class Program
    {
        private static string currentDir = Directory.GetCurrentDirectory();

        private static string stringStorageDowloadMusic = Path.Combine(currentDir, "DowloadedMusic");
        private static string stringStorageServer = @"C:\Users\Werty\source\repos\Code\C#\Server\HttpServer\bin\Debug\net8.0\uploads";

        private static string _token = Environment.GetEnvironmentVariable("ApiKeys_SecretTgToken");

        private static long _errorChatId = 1396730464; // tg: @werty2648 

        private static TelegramBotClient _botClient;

        private static string CurrentTime() => DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff");

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            _botClient = new TelegramBotClient(_token);

            using var cts = new CancellationTokenSource();

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery }
            };


            inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                    InlineKeyboardButton.WithCallbackData("Song", "song"),
                    InlineKeyboardButton.WithCallbackData("Artist", "artist")
            });

            _botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                errorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
            );

            var me = await _botClient.GetMeAsync();
            Console.WriteLine($"Bot @{me.Username} running!");

            Console.ReadLine();
            cts.Cancel();
        }

        static ReplyKeyboardMarkup replyKeyboardMarkup = new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] {"/start"}
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        };

        public static InlineKeyboardMarkup inlineKeyboard;

        private const string urlWebStorage = "http://10.147.18.220:8080/";

        private static string previousMessage = null;

        private static long chatId = -1;
        [Obsolete]
        private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Message is { } message)
            {
                Console.WriteLine(message.Chat.Id + " : " + message.Text);
                chatId = message.Chat.Id;

                string? text = message.Text;

                if (String.IsNullOrEmpty(text))
                    return;

                if (text == "/start")
                {
                    await botClient.SendTextMessageAsync(
                        chatId: _errorChatId,
                        text: "Choose what do you want to find",
                        replyMarkup: inlineKeyboard,
                        cancellationToken: cancellationToken);
                    return;
                }

                if (String.IsNullOrEmpty(previousMessage))
                    return;

                string idFolder = Path.Combine(stringStorageDowloadMusic, message.Chat.Id.ToString());
                if (!Directory.Exists(idFolder))
                    Directory.CreateDirectory(idFolder);

                string timeFolder = Path.Combine(idFolder, CurrentTime());
                if (!Directory.Exists(timeFolder))
                    Directory.CreateDirectory(timeFolder);


                var fullSongInfo = GetSongInfo(text, timeFolder);

                if (fullSongInfo == null)
                { 
                    SendTextMessage($"Not Found Info", message.Chat.Id);
                    return;
                }

                if (previousMessage == "song")
                {
                    string tempSongFolder = Path.Combine(timeFolder, "temp");
                    if (!Directory.Exists(tempSongFolder))
                        Directory.CreateDirectory(tempSongFolder);

                    var fullPath = DownloadSong.Download(fullSongInfo, timeFolder, tempSongFolder);

                    if (fullPath.Count() == 0)
                    {
                        SendTextMessage($"Not Found song with {text} name", message.Chat.Id);
                    }
                    else
                    {
                        SendTextMessage($"Find song: {fullSongInfo.songName}", message.Chat.Id);
                        SendTextMessage($"Find artist name: {fullSongInfo.artist}", message.Chat.Id);
                        SendTextMessage($"Url the artist: {fullSongInfo.urlArtist}", message.Chat.Id);

                        var musicName = Path.GetFileName(fullPath);
                        SendFileAsync(message.Chat.Id, fullPath);
                        CopyToUploadsStorage(musicName, timeFolder, stringStorageServer);

                        DeleteTempsFolder(tempSongFolder);
                    }
                }
                else
                if (previousMessage == "artist")
                {
                    SendTextMessage($"Find artist name: {fullSongInfo.artist}", message.Chat.Id);
                    SendTextMessage($"Url the artist: {fullSongInfo.urlArtist}", message.Chat.Id);

                    string artistFolder = Path.Combine(timeFolder, "Songer");
                    if (!Directory.Exists(artistFolder))
                        Directory.CreateDirectory(artistFolder);

                    string tempArtistFolder = Path.Combine(artistFolder, "temp");
                    if (!Directory.Exists(tempArtistFolder))
                        Directory.CreateDirectory(tempArtistFolder);

                    var songPaths = DowloadArtist(fullSongInfo, artistFolder, tempArtistFolder);

                    if (songPaths.Count == 0)
                    {
                        SendTextMessage("Not found song artist", message.Chat.Id);
                    }
                    else
                    {

                        foreach (var file in songPaths)
                        {
                            await SendFileAsync(message.Chat.Id, file);
                            CopyToUploadsStorage(Path.GetFileName(file), artistFolder, stringStorageServer);
                        }

                        DeleteTempsFolder(tempArtistFolder);
                        ZipFile.CreateFromDirectory(artistFolder, $"{Path.Combine(stringStorageServer, fullSongInfo.artist)}.zip");
                    }
                }


                SendTextMessage($"You can load this on: {urlWebStorage} with _name_", message.Chat.Id);

                previousMessage = null;
            }
            else if (update.CallbackQuery is { } callbackQuery)
            {
                // notification
                //await botClient.AnswerCallbackQueryAsync(
                //callbackQueryId: callbackQuery.Id,
                //text: $"Вы нажали: {callbackQuery.Data}",
                //cancellationToken: cancellationToken);

                var data = callbackQuery.Data;
                previousMessage = data;

                if (data == "song")
                {
                    SendTextMessage("Enter the song name", chatId);
                }
                else if (data == "artist")
                {
                    SendTextMessage("Enter the artist name", chatId);
                }
            }
        }

        private static void DeleteTempsFolder(params string[] tempFolders)
        {
            foreach (var tempFolder in tempFolders)
                Directory.Delete(tempFolder);
        }
        private static void CopyToUploadsStorage(string nameSong, string sourceFolder, string destFolder)
        {
            string source = Path.Combine(sourceFolder, nameSong);
            string destination = Path.Combine(destFolder, nameSong);

            if (!System.IO.File.Exists(destination))
                System.IO.File.Copy(source, destination);
        }
        private static InfoSong GetSongInfo(string name, string timeFolder)
        {
            var urlSong = SongInfo.UbgradeUrl(name);
            var info = SongInfo.FindSongsInfo(urlSong, 1);

            if (info.Count == 0)
                return null;

            return info.First();
        }

        private static List<string> DowloadArtist(InfoSong info, string folderToDowload,string tempArtistFolder)
        {
            string chars = "\\/:*?\"<>|";
            char[] newNameArtist = info.artist.ToCharArray();

            for (int i = 0; i < newNameArtist.Length; i++)
                if (chars.Contains(newNameArtist[i]))
                    newNameArtist[i] = '_';

            var res = AllArtistsSongs.Dowloads(info.urlArtist, folderToDowload, tempArtistFolder);
            return res;
        }


        public static async Task<bool> SendFileAsync(long chatId, string filePath, string caption = "")
        {
            try
            {
                var fileSize = new FileInfo(filePath).Length;

                if (fileSize <= 50 * 1024 * 1024)
                {
                    await using var stream = System.IO.File.OpenRead(filePath);
                    await _botClient.SendDocumentAsync(
                        chatId: chatId,
                        replyMarkup: replyKeyboardMarkup,
                        document: new InputFileStream(stream, Path.GetFileName(filePath)),
                        caption: caption);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при отправке файла: {ex.Message}");
                return false;
            }
        }
        [Obsolete]
        private static async void SendTextMessage(string text, long chatId)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                replyMarkup : replyKeyboardMarkup,
                text: text);
        }
        private static async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Ошибка API Telegram:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(errorMessage, botClient);

            await botClient.SendTextMessageAsync(
                chatId: _errorChatId,
                text: errorMessage,
                replyMarkup: replyKeyboardMarkup,
                cancellationToken: cancellationToken);
        }
    }
}