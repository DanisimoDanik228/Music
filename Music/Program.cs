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


class Program
{
    private static string storageFolder = Path.Combine(Directory.GetCurrentDirectory(), "DowloadedMusic");
    private static string storageFolderArtist = Path.Combine(storageFolder, "Songers");

    private static string _token = Environment.GetEnvironmentVariable("ApiKeys_SecretTgToken");

    private static TelegramBotClient _botClient;
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

        var files = Dowload(text);

        SendFileAsync(message.Chat.Id, files.fileSong, "Your song");
        SendFileAsync(message.Chat.Id,files.fileArtist,"Your artist's song");
    }

    private static long _errorChatId = 1396730464; // tg: @werty2648
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
    private static (string fileSong,string fileArtist) Dowload(string name)
    {
        var urlSong = SongInfo.UbgradeUrl(name);
        var info = SongInfo.FindSongsInfo(urlSong, 1).First();

        string folder = Path.Combine(storageFolder, info.songName);

        string chars = "\\/:*?\"<>|";
        char[] newNameArtist = info.artist.ToCharArray();

        for (int i = 0; i < newNameArtist.Length; i++)
            if (chars.Contains(newNameArtist[i]))
                newNameArtist[i] = '_';

        string folderArtist = Path.Combine(storageFolderArtist, new string(newNameArtist));

        DownloadSong.Download(info.songUrl, folder);
        AllArtistsSongs.Dowloads(info.urlArtist, folderArtist);


        ZipFile.CreateFromDirectory(folder, $"{folder}.zip");
        ZipFile.CreateFromDirectory(folderArtist, $"{folderArtist}.zip");

        return ($"{folder}.zip", $"{folderArtist}.zip");
    }

    private static async Task<bool> SendLargeFileAsync(long chatId, string filePath, string caption)
    {
        try
        {
            // Разбиваем файл на части
            var tempDir = Path.Combine(Path.GetTempPath(), "TelegramUpload");
            Directory.CreateDirectory(tempDir);

            var chunkSize = 45 * 1024 * 1024; // 45 МБ
            var partNumber = 1;

            await using (var inputStream = System.IO.File.OpenRead(filePath))
            {
                var buffer = new byte[chunkSize];
                int bytesRead;

                while ((bytesRead = await inputStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    var partPath = Path.Combine(tempDir, $"{Path.GetFileName(filePath)}.part{partNumber}");
                    await using (var partStream = System.IO.File.Create(partPath))
                    {
                        await partStream.WriteAsync(buffer, 0, bytesRead);
                    }

                    // Отправляем часть
                    await using (var partStream = System.IO.File.OpenRead(partPath))
                    {
                        await _botClient.SendDocumentAsync(
                            chatId: chatId,
                            document: new InputFileStream(partStream, $"{Path.GetFileName(filePath)}.part{partNumber}"),
                            caption: partNumber == 1 ? $"Файл {Path.GetFileName(filePath)} (часть {partNumber})" : "");
                    }

                    System.IO.File.Delete(partPath);
                    partNumber++;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при отправке большого файла: {ex.Message}");
            return false;
        }
    }
    public static async Task<bool> SendFileAsync(long chatId, string filePath, string caption = "")
    {
        try
        {
            var fileSize = new FileInfo(filePath).Length;

            // Если файл меньше 50 МБ - отправляем напрямую
            if (fileSize <= 50 * 1024 * 1024)
            {
                await using var stream = System.IO.File.OpenRead(filePath);
                await _botClient.SendDocumentAsync(
                    chatId: chatId,
                    document: new InputFileStream(stream, Path.GetFileName(filePath)),
                    caption: caption);
                return true;
            }
            // Если файл больше 50 МБ - используем альтернативный метод
            else
            {
                return await SendLargeFileAsync(chatId, filePath, caption);
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
}