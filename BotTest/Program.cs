using System.Text;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

Console.OutputEncoding = Encoding.UTF8;

var botClient = new TelegramBotClient("7536310758:AAFFy1rT715pq3mRT34uvH2ZbBttsRpt4GA");

using var cts = new CancellationTokenSource();

var receiverOptions = new ReceiverOptions
{
    AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery }
};

botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    errorHandler: HandlePollingErrorAsync,
    receiverOptions: receiverOptions,
    cancellationToken: cts.Token
);

async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    // Обработка обычных сообщений
    if (update.Message is { } message)
    {
        Console.WriteLine(message.Chat.Id + " : " + message.Text);

        if (message.Text == "/start")
        {
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                InlineKeyboardButton.WithCallbackData("Подопция 1", "sub1"),
                InlineKeyboardButton.WithCallbackData("Подопция 2", "sub2"),
                InlineKeyboardButton.WithCallbackData("Подопция 3", "sub3")
            });

            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Выберите подопцию:",
                replyMarkup: inlineKeyboard,
                cancellationToken: cancellationToken);
        }
    }

    // Обработка нажатий на инлайн-кнопки
    else if (update.CallbackQuery is { } callbackQuery)
    {
        // notification
        //await botClient.AnswerCallbackQueryAsync(
        //callbackQueryId: callbackQuery.Id,
        //text: $"Вы нажали: {callbackQuery.Data}",
        //cancellationToken: cancellationToken);
        
        await botClient.SendTextMessageAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            text: $"Вы выбрали: {callbackQuery.Data}",
            cancellationToken: cancellationToken);
    }
}

Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    var ErrorMessage = exception switch
    {
        ApiRequestException apiRequestException
            => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };

    Console.WriteLine(ErrorMessage);
    return Task.CompletedTask;
}

Console.WriteLine("Бот запущен. Нажмите любую клавишу для остановки...");
Console.ReadKey();
cts.Cancel();