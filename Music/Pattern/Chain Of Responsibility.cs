using Microsoft.Extensions.FileProviders;
using Music.MainFunction;
using OpenQA.Selenium.DevTools.V136.WebAuthn;
using OpenQA.Selenium.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using static System.Net.Mime.MediaTypeNames;

namespace Music.Pattern
{
    public interface IHandler
    {
        IHandler SetNext(IHandler handler);
        bool Handle(Update request, List<Update> previousMessage);
    }

    public abstract class AbstractHandler : IHandler
    {
        public ITelegramBotClient _botClient;
        public const string _urlWebStorage = "http://10.147.18.220:8080/";
        public const long _errorChatId = 1396730464; // tg: @werty2648 
        public readonly long _chatId;

        public IHandler _nextHandler { get; private set; }

        public IHandler SetNext(IHandler handler)
        {
            _nextHandler = handler;
            return handler;
        }

        public virtual bool Handle(Update request, List<Update> previousMessage)
        {
            if (_nextHandler != null)
                return _nextHandler.Handle(request, previousMessage);

            return false;
        }

        public AbstractHandler(ITelegramBotClient botClient, long charId)
        {
            _botClient = botClient;
            _chatId = charId;
        }

        public async void SendFileAsync(string filePath)
        {
            try
            {
                var fileSize = new FileInfo(filePath).Length;

                if (fileSize <= 50 * 1024 * 1024) // 50MB
                {
                    using var stream = System.IO.File.OpenRead(filePath);
                    await _botClient.SendDocumentAsync(
                        chatId: _chatId,
                        document: new InputFileStream(stream, Path.GetFileName(filePath))
                        );
                }
                else
                {
                    throw new Exception($"Large file:{filePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error with send file: {ex.Message}");
            }
        }
    }

    public class SongNameHandler : AbstractHandler
    {
        public SongNameHandler(ITelegramBotClient botClient, long charId) : base(botClient, charId)
        {
        }

        private static bool IsSongName(List<Update> previousMessage)
        {
            return (previousMessage.Any()) && (previousMessage.Last().CallbackQuery != null) && (previousMessage.Last().CallbackQuery.Data == "song");
        }

        public override bool Handle(Update request, List<Update> previousMessage)
        {
            try
            {
                if (request.Message is { } message)
                {
                    if (IsSongName(previousMessage))
                    {
                        var info = SongDowloader.FindSongsInfoFromUrl(SongDowloader.CreateUrlForSearch(message.Text)).First();
                       
                        _botClient.SendTextMessageAsync(
                            text: $"Find song name is {info.songName}\n" +
                            $"Find artist name is: {info.artist}",
                            chatId: _chatId
                            );

                        var dowloader = new SongDowloader(Path.Combine(MainItem.directoryDowload, _chatId.ToString()));

                        var filename = dowloader.DownloadSongToFolder(info, dowloader.mainFolder);
                        SendFileAsync(filename);

                        if (!System.IO.File.Exists(filename))
                            throw new Exception("Not Found file: " + filename);

                        _botClient.SendTextMessageAsync(
                            text: $"You can see all files on the: {_urlWebStorage}",
                            chatId: _chatId
                        );

                        dowloader.CopyAllFilesToStorageServer();

                        return true;
                    }
                }
                else if (request.CallbackQuery is { } callbackQuery)
                {
                    var data = callbackQuery.Data;

                    if (data == "song")
                    {
                        _botClient.SendMessage(
                            chatId: _chatId,
                            text: "Enter the song name"
                            );

                        return true;
                    }
                }

                return _nextHandler.Handle(request, previousMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in SongNameHandler: " + ex.Message);

                return false;
            }
        }
    }

    public class ArtistNameHandler  : AbstractHandler
    {
        public ArtistNameHandler(ITelegramBotClient botClient, long charId) : base(botClient, charId)
        {
        }

        private static bool IsArtistName(List<Update> previousMessage)
        {
            return (previousMessage.Any()) && (previousMessage.Last().CallbackQuery != null) && (previousMessage.Last().CallbackQuery.Data == "artist");
        }

        public override bool Handle(Update request,List<Update> previousMessage)
        {
            try
            {

                if (request.Message is { } message)
                {
                    if (IsArtistName(previousMessage))
                    {
                        var info = SongDowloader.FindSongsInfoFromUrl(SongDowloader.CreateUrlForSearch(message.Text),20);

                        var dowloader = new SongDowloader(Path.Combine(MainItem.directoryDowload,_chatId.ToString()));

                        foreach (var item in info)
                        { 
                            var filename = dowloader.DownloadSongToFolder(item,dowloader.mainFolderArtist);
                            //SendFileAsync(filename);
                            _botClient.SendTextMessageAsync(
                                text: $"Find artist name is {item.artist}\n" +
                                $"Find url Artist is: {item.urlArtist}",
                                chatId: _chatId
                                );
                        }

                        _botClient.SendTextMessageAsync(
                            text: $"You can see all files on the: {_urlWebStorage}",
                            chatId:_chatId
                            );


                        dowloader.CopyAllFilesToStorageServer();

                        return true;
                    }
                }
                else if (request.CallbackQuery is { } callbackQuery)
                {
                    var data = callbackQuery.Data;

                    if (data == "artist")
                    {
                        _botClient.SendMessage(
                            chatId: _chatId,
                            text: "Enter the artist name"
                            );

                        return true;
                    }
                }

                return _nextHandler.Handle(request, previousMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in ArtistNameHandler: " + ex.Message);

                return false;
            }
        }
    }

    public class InitHandler : AbstractHandler
    {
        private InlineKeyboardMarkup _mainInlineKeyboard;

        public InitHandler(ITelegramBotClient botClient, long charId) : base(botClient, charId)
        {
            _mainInlineKeyboard = MainItem.CreateInlineKeyboard([("Song", "song"), ("Artist", "artist")]);
        }
        public override bool Handle(Update request, List<Update> previousMessage)
        {
            try
            {
                if (request.Message is { } message)
                {
                    if (message.Text == "/start")
                    {
                        _botClient.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: "Choose what do you want to find",
                            replyMarkup: _mainInlineKeyboard);

                        return true;
                    }
                }

                return _nextHandler.Handle(request,previousMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in InitHandler: " + ex.Message);

                return false;
            }
        }
    }

    public class LastHandler : AbstractHandler
    {
        public LastHandler(ITelegramBotClient botClient, long charId) : base(botClient, charId)
        {
        }

        public override bool Handle(Update request, List<Update> previousMessage) => false;
    }
}
