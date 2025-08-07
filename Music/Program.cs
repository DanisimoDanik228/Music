using System.Diagnostics;
using System.IO;
using System.IO.Compression;
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

using InfoSong = (string artist, string songName, string songUrl, string urlArtist);

partial class Program
{
    private static string currentDir = Directory.GetCurrentDirectory();

    private static string stringStorageDowloadMusic = Path.Combine(currentDir, "DowloadedMusic");
    private static string stringStorageDowloadMusicArtist = Path.Combine(stringStorageDowloadMusic, "Artist");
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
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        _botClient.StartReceiving(
            HandleUpdateAsync,
            HandlePollingErrorAsync,
            receiverOptions,
            cts.Token
        );

        var me = await _botClient.GetMeAsync();
        Console.WriteLine($"Bot @{me.Username} running!");

        Console.ReadLine();
        cts.Cancel();

    }

    [Obsolete]
    private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not { } message)
            return;

        Console.WriteLine(message.Chat.Id + " : " + message.Text);

        var replyKeyboardMarkup = new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] {"/start"}
        })
        {
            ResizeKeyboard = true
        };

        string? text = message.Text;

        if (String.IsNullOrEmpty(text))
            return;

        if (text == "/start")
        {
            SendTextMessage("_lol_",message);
            return;
        }


        string idFolder = Path.Combine(stringStorageDowloadMusic, message.Chat.Id.ToString());
        if (!Directory.Exists(idFolder))
            Directory.CreateDirectory(idFolder);

        string timeFolder = Path.Combine(idFolder, CurrentTime());
        if (!Directory.Exists(timeFolder))
            Directory.CreateDirectory(timeFolder);


        var fullSongInfo = GetSongInfo(text,timeFolder);
        var fullPath = DownloadSong.Download(fullSongInfo, timeFolder);

        var musicName = Path.GetFileName(fullPath);
        SendFileAsync(message.Chat.Id, fullPath);
        CopyToUploadsStorage(musicName, timeFolder, stringStorageServer);


        string artistFolder = Path.Combine(timeFolder, "Songer");
        if (!Directory.Exists(artistFolder))
            Directory.CreateDirectory(artistFolder);


        DowloadArtist(fullSongInfo, artistFolder);
        var files = Directory.GetFiles(artistFolder);
        foreach (var file in files)
        {
            SendFileAsync(message.Chat.Id, file);
            CopyToUploadsStorage(Path.GetFileName(file), artistFolder, stringStorageServer);
        }
    }
    private static void CopyToUploadsStorage(string nameSong,string sourceFolder, string destFolder)
    {
        string source = Path.Combine(sourceFolder, nameSong);
        string destination = Path.Combine(destFolder, nameSong);

        if (!System.IO.File.Exists(destination))
            System.IO.File.Copy(source, destination);
    }
    private static InfoSong GetSongInfo(string name, string timeFolder)
    {
        var urlSong = SongInfo.UbgradeUrl(name);
        var info = SongInfo.FindSongsInfo(urlSong, 1).First();

        return info;
    }

    public static string TransliterateToLatin(string input)
    {
        var translitMap = new Dictionary<char, string>
        {
            ['а'] = "a",
            ['б'] = "b",
            ['в'] = "v",
            ['г'] = "g",
            ['д'] = "d",
            ['е'] = "e",
            ['ё'] = "yo",
            ['ж'] = "zh",
            ['з'] = "z",
            ['и'] = "i",
            ['й'] = "y",
            ['к'] = "k",
            ['л'] = "l",
            ['м'] = "m",
            ['н'] = "n",
            ['о'] = "o",
            ['п'] = "p",
            ['р'] = "r",
            ['с'] = "s",
            ['т'] = "t",
            ['у'] = "u",
            ['ф'] = "f",
            ['х'] = "kh",
            ['ц'] = "ts",
            ['ч'] = "ch",
            ['ш'] = "sh",
            ['щ'] = "shch",
            ['ъ'] = "",
            ['ы'] = "y",
            ['ь'] = "",
            ['э'] = "e",
            ['ю'] = "yu",
            ['я'] = "ya"
        };

        var sb = new StringBuilder();

        foreach (char c in input.ToLower())
        {
            if (translitMap.TryGetValue(c, out var latin))
                sb.Append(latin);
            else if (char.IsLetterOrDigit(c) || c == ' ')
                sb.Append(c);
            else
                sb.Append('-'); 
        }

        return sb.ToString().Replace(' ', '-'); 
    }

    private static void DowloadArtist(InfoSong info, string folderToDowload)
    {
        string chars = "\\/:*?\"<>|";
        char[] newNameArtist = info.artist.ToCharArray();

        for (int i = 0; i < newNameArtist.Length; i++)
            if (chars.Contains(newNameArtist[i]))
                newNameArtist[i] = '_';

        AllArtistsSongs.Dowloads(info.urlArtist, folderToDowload);
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
    private static async void SendTextMessage(string text,Message message)
    {
        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
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
            cancellationToken: cancellationToken);
    }
}