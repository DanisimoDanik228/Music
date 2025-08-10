using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;

namespace Music.MainFunction
{
    public static class MainItem
    {
        public static string currentDir = Directory.GetCurrentDirectory();
        public static string directoryDowload = Path.Combine(currentDir,"DowloadFiles");
        public static string CurrentTime() => DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff");
        public static InlineKeyboardMarkup CreateInlineKeyboard(IEnumerable<(string name, string data)> mas)
        {
            List<InlineKeyboardButton[]> result = new();

            foreach (var item in mas)
                result.Add([InlineKeyboardButton.WithCallbackData(item.name, item.data)]);

            return new InlineKeyboardMarkup(result);
        }

        public static void WriteLine(string text)
        {
            var t = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(text);

            Console.ForegroundColor = t;
        }
    }
}
