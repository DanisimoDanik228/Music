using Microsoft.VisualStudio.TestPlatform.TestHost;
using MusicBot;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TestTgBot
{
    [TestClass]
    public sealed class Test1
    {

        [TestMethod]
        public void TestMethod1()
        {
            MusicBot.Program.Main([]);

            var bot = MusicBot.Program._botClient;
            var cancelToken = new CancellationToken();

            var updateStart = MakeTextMessage("/start");

            var result = MusicBot.Program.HandleMessage(updateStart);

            Assert.AreEqual("Choose what do you want to find", result.First().text);
            Assert.AreEqual(2, result.First().markup.InlineKeyboard.Count());
        }

        private static Update MakeTextMessage(string text, long userId = 1396730464, string userName = "werty2648")
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
                    Id = 1396730464
                }
            };
        }
    }
}
