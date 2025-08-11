using Music;
using Music.MainFunction;
using Music.Pattern;
using OpenQA.Selenium.DevTools.V136.Debugger;
using OpenQA.Selenium.DevTools.V136.Network;
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
    partial class Program
    {
        private const long _errorChatId = 1396730464; // tg: @werty2648 
        private static long chatId = -1;
        private static string _token = Environment.GetEnvironmentVariable("ApiKeys_SecretTgToken");
        private static TelegramBotClient _botClient;
        private static List<Update> previousMessage;
        private static ReplyKeyboardMarkup replyKeyboardMarkup = new ReplyKeyboardMarkup(new KeyboardButton[] {"/start"})
        {
            ResizeKeyboard = true
        };

        private static AbstractHandler handlerMessage = null;
        static async Task Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.OutputEncoding = Encoding.UTF8;

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
        private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
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

                    var a = new InitHandler(_botClient,chatId);
                    var b = new SongNameHandler(_botClient,chatId);
                    var c = new ArtistNameHandler(_botClient,chatId);
                    var last = new LastHandler(_botClient, chatId);
                    a.SetNext(b).SetNext(c).SetNext(last);

                    handlerMessage = a;
                }
            }

            if (handlerMessage != null)
            {
                var result = handlerMessage.Handle(update, previousMessage);

                MainItem.WriteLine($"Result of handler message is {result}");
            }

            if (previousMessage != null)
                previousMessage.Add(update);
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
    }
}