using DotNetEnv;
using Music;
using Music.MainFunction;
using Music.Pattern;
using Music.PostgresSQL;
using OpenQA.Selenium.DevTools.V136.Debugger;
using OpenQA.Selenium.DevTools.V136.Network;
using Sprache;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.IO.Pipelines;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;


namespace MusicBot
{
    public class Program
    {
        private const long _errorChatId = 1396730464; // tg: @werty2648 
        private static long chatId = -1;
        private static string _token = Environment.GetEnvironmentVariable("ApiKeys_SecretTgToken");
        public static TelegramBotClient _botClient;
        private static List<Update> previousMessage;
        private static ReplyKeyboardMarkup replyKeyboardMarkup = new ReplyKeyboardMarkup(new KeyboardButton[] {"/start"})
        {
            ResizeKeyboard = true
        };

        private static AbstractHandler handlerMessage = null;
        public static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            //PostgresDateBase.Demo();

            if (!MainItem.IsProcessRunning("HttpServer"))
            {
                string url;
                do
                {
                    Console.WriteLine("Enter webUrlStorage");
                    url = Console.ReadLine();
                }
                while (await NetworkItem.PingUrl(url));

                NetworkItem._urlWebStorage = url;
            }
            else
            {
                NetworkItem._urlWebStorage = $"http://{NetworkItem.GetZeroTierIp()}:{NetworkItem._Port}/";
            }

            MainItem.WriteLine("Load urlWebStorage: " + NetworkItem._urlWebStorage);

            if (String.IsNullOrEmpty(_token))
            {
                MainItem.WriteLine("Not found token in EnvironmentVariable");
                MainItem.Write("Enter the token:");

                _token = Console.ReadLine();
            }

            _botClient = new TelegramBotClient(_token);

            using var cts = new CancellationTokenSource();

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery }
            };

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
        public static async Task<IEnumerable<ResponseHandler>> HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Message is { } m)
                MainItem.WriteLine($"{m.Chat.Id} : {m.Text}");

            if (update.Message is { } message)
            {
                if (message.Text == "/start")
                {
                    DictionaryKeyboardMarkup.Clear();
                    chatId = message.Chat.Id;
                    previousMessage = new List<Update>();

                    var a = new InitHandler();
                    var b = new SongNameHandler();
                    var c = new ChosenOptionFind();
                    var d = new ChosenSongName();
                    var last = new LastHandler();
                    a.SetNext(b).SetNext(c).SetNext(d).SetNext(last);

                    handlerMessage = a;
                }
            }

            IEnumerable<ResponseHandler> result = null;

            if (handlerMessage != null)
            {
                result = handlerMessage.Handle(update, previousMessage);

                MainItem.WriteLine($"Result of handler message is :");

                if (result != null)
                { 
                    foreach (var item in result)
                        MainItem.WriteLine(item.ToString());

                    foreach (var item in result)
                        MakeResponse(item);
                }

            }

            if (previousMessage != null)
                previousMessage.Add(update);

            return result;
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
        private static void MakeResponse(ResponseHandler data)
        {
            if(data.text != null)
                _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: data.text,
                    replyMarkup: data.markup
                );

            if (data.files != null)
            {
                foreach (var item in data.files)
                {
                    SendFileAsync(chatId,item);
                }
            }
        }

        public static IEnumerable<ResponseHandler> MakeTestResponse(Update update)
        {
            if (update.Message is { } m)
                MainItem.WriteLine($"{m.Chat.Id} : {m.Text}");

            if (update.Message is { } message)
            {
                if (message.Text == "/start")
                {
                    DictionaryKeyboardMarkup.Clear();
                    chatId = message.Chat.Id;
                    previousMessage = new List<Update>();

                    var a = new InitHandler();
                    var b = new SongNameHandler();
                    var c = new ChosenOptionFind();
                    var d = new ChosenSongName();
                    var last = new LastHandler();
                    a.SetNext(b).SetNext(c).SetNext(d).SetNext(last);

                    handlerMessage = a;
                }
            }

            IEnumerable<ResponseHandler> result = null;

            if (handlerMessage != null)
            {
                result = handlerMessage.Handle(update, previousMessage);
            }

            if (previousMessage != null)
                previousMessage.Add(update);

            return result;
        }
    }
}