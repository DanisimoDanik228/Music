using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Music.MainFunction;
using MusicBot;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TestTgBot
{
    [TestClass]
    public sealed class Test1
    {
        private TelegramBotClient _bot;
        private CancellationToken _cancelToken;
        private bool _isStarted = false;
        
        private void InitTest()
        {
            if (_isStarted)
                return;

            _isStarted = true;

            //MusicBot.Program.Main([]);

            //_bot = MusicBot.Program._botClient;
            //_cancelToken = new CancellationToken();
        }
        private static Update MakeTextMessage(string text, long userId = 1396730464, int chatId = 69, string userName = "werty2648")
        {
            return new Update
            {
                Message = new Message
                {
                    From = new User
                    {
                        Id = userId,
                        IsBot = false,
                        FirstName = "Test",
                        LastName = "User",
                        Username = userName
                    },
                    Chat = new Chat
                    {
                        Id = userId,
                        Type = ChatType.Private,
                        FirstName = "firstName",
                        LastName = "lastName",
                        Username = userName
                    },
                    Date = DateTime.Now,
                    Text = text,
                    Id = chatId
                }
            };
        }
        public static Update MakeCallbackQuery(string callbackData, long userId = 1396730464, string chatId = "69", string userName = "werty2648")
        {
            return new Update
            {
                CallbackQuery = new CallbackQuery
                {
                    From = new User
                    {
                        Id = userId,
                        IsBot = false,
                        FirstName = "Test",
                        LastName = "User",
                        Username = userName
                    },
                    Message = new Message
                    {
                        Chat = new Chat
                        {
                            Id = userId,
                            Type = ChatType.Private,
                            FirstName = "firstName",
                            LastName = "lastName",
                            Username = userName
                        },
                        Date = DateTime.UtcNow.AddMinutes(-1)
                    },
                    Data = callbackData,
                    Id = chatId
                }
            };
        }
        
        [TestMethod]
        [DoNotParallelize]
        public void TestStartMethod()
        {
            InitTest();
            var updateStart = MakeTextMessage("/start");

            var result = MusicBot.Program.MakeTestResponse( updateStart);

            Assert.AreEqual("Choose what do you want to find", result.First().text);
            Assert.AreEqual(2, result.First().markup.InlineKeyboard.Count());
        }

        [TestMethod]
        [DoNotParallelize]
        public void TestMethodDowloadAll()
        {
            InitTest();

            MusicBot.Program.HandleUpdateAsync(_bot,MakeTextMessage("/start"),_cancelToken);

            string numItem = "0"; // SongChosen
            var result1 = MusicBot.Program.MakeTestResponse(MakeCallbackQuery(numItem));
            Assert.AreEqual("Enter the song name", result1.First().text);


            var result2 = MusicBot.Program.MakeTestResponse(MakeTextMessage("Kill"));

            int buttonDowloadAll = 1;
            int countFindSong = result2.First().markup.InlineKeyboard.Count() - buttonDowloadAll;
            int idDowloadAllButton = countFindSong + 1 + buttonDowloadAll;

            var result3 = MusicBot.Program.MakeTestResponse(MakeCallbackQuery(idDowloadAllButton.ToString()));

            foreach (var item in result3.First().files)
            {
                Console.WriteLine("[Test OutPut]: " + item);
            }

            Assert.AreEqual(countFindSong, result3.First().files.Count(),"Not equal count dowload files");
        }

        [TestMethod]
        [DoNotParallelize]
        public void TestMethodNotFoundSong()
        {
            InitTest();

            MusicBot.Program.MakeTestResponse(MakeTextMessage("/start"));

            string numItem = "0"; // SongChosen
            var result1 = MusicBot.Program.HandleUpdateAsync(_bot, MakeCallbackQuery(numItem), _cancelToken).Result;
            Assert.AreEqual("Enter the song name", result1.First().text);


            var result2 = MusicBot.Program.MakeTestResponse(MakeTextMessage("sdfjsifnsnfbs98gsgk"));

            Assert.AreEqual(null,result2.First().markup);

            Assert.AreEqual("No Found Song", result2.First().text);
        }
    }
}
