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
using System.Xml.Linq;
using System.Xml.Serialization;
using TagLib.Ape;
using TagLib.Mpeg;
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
        public static Dictionary<string, string> chosenReplyMarkup = new();
        public ITelegramBotClient _botClient;

        public const long _errorChatId = 1396730464; // tg: @werty2648 
        public readonly long _chatId;


        public const string groupNameSong = "song_names";
        public const string groupSongOrArtist= "song_artist";

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


        // list of find songs
        private static bool IsChosenSongName(Update currentMessage)
        {
            if (currentMessage.CallbackQuery == null)
                return false;

            int numItem = DictionaryKeyboardMarkup.GetNumItem(currentMessage);

            return DictionaryKeyboardMarkup.GetNameGroup(numItem) == groupNameSong;
        }


        // text message of song name for search
        private static bool IsSongName(Update current,List<Update> previousMessage)
        {
            if (!previousMessage.Any() || current.Message == null || previousMessage.Last().CallbackQuery== null)
                return false;

            int numItem = DictionaryKeyboardMarkup.GetNumItem(previousMessage.Last());

            return DictionaryKeyboardMarkup.GetNameGroup(numItem) == groupSongOrArtist && DictionaryKeyboardMarkup.GetData(numItem) == "song";
        }


        // chose "song" from["song","artist"]
        private static bool IsChosenOptionSong(Update currentMessage)
        {
            if (currentMessage.CallbackQuery == null)
                return false;

            int numItem = DictionaryKeyboardMarkup.GetNumItem(currentMessage);

            return DictionaryKeyboardMarkup.GetNameGroup(numItem) == groupSongOrArtist && DictionaryKeyboardMarkup.GetData(numItem) == "song";
        }

        public override bool Handle(Update request, List<Update> previousMessage)
        {
            try
            {
                if (IsChosenSongName(request))
                {
                    var numItem = DictionaryKeyboardMarkup.GetNumItem(request);

                    string data = DictionaryKeyboardMarkup.GetData(numItem);

                    var dowloadFolder = Path.Combine(MainItem.directoryDowload, _chatId.ToString(), MainItem.CurrentTime());
                    if (!Directory.Exists(dowloadFolder))
                        Directory.CreateDirectory(dowloadFolder);

                    if (data == "_dowload_all_")
                    {
                        var nameGroup = DictionaryKeyboardMarkup.GetNameGroup(numItem);
                        var range = DictionaryKeyboardMarkup.GetRangeGroup(nameGroup);

                        for (int i = range.left; i <= range.right - 1; i++)
                        {
                            var xmlData = DictionaryKeyboardMarkup.GetData(i);
                            InfoSong info = MainItem.DeSerialize(xmlData);
                            var filename = MainItem.DowloadMusicFromAllSource(info, dowloadFolder);

                            SendFileAsync(filename);
                        }
                    }
                    else
                    {
                        InfoSong info = MainItem.DeSerialize(data);

                        var filename = MainItem.DowloadMusicFromAllSource(info, dowloadFolder);

                        SendFileAsync(filename);
                    }

                    _botClient.SendTextMessageAsync(
                        text: $"You can see all files on the: {NetworkItem._urlWebStorage}",
                        chatId: _chatId
                    );

                    MainItem.CopyAllFilesToStorageServerFromAllSource();

                    return true;
                }
                else if (IsSongName(request, previousMessage))
                {
                    var infoSongs = MainItem.GetInfoSongFromAllSource(request.Message.Text, 5);

                    if (infoSongs.Count() == 0)
                    {
                        _botClient.SendTextMessageAsync(
                            chatId: _chatId,
                            text:"No Found Song"
                            );    

                        return true;
                    }

                    List<(string, string)> tempList = new();

                    foreach (var item in infoSongs)
                    {
                        string sogInfoInString = MainItem.Serialize(item);
                        tempList.Add(($"{item.artist} - {item.songName}", sogInfoInString));
                    }

                    tempList.Add(($"[Dowload ALL]", "_dowload_all_"));


                    _botClient.SendTextMessageAsync(
                        text: $"Find songs",
                        chatId: _chatId,
                        replyMarkup: DictionaryKeyboardMarkup.CreateInlineKeyboard(tempList, groupNameSong)
                    );

                    return true;
                }
                else if (IsChosenOptionSong(request))
                {
                    _botClient.SendMessage(
                          chatId: _chatId,
                          text: "Enter the song name"
                          );

                    return true;
                }

                    return _nextHandler.Handle(request, previousMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in SongNameHandler: " + ex.Message);
                throw ex;

                return false;
            }
        }
    }
    
    public class ArtistNameHandler : AbstractHandler
    {
        public ArtistNameHandler(ITelegramBotClient botClient, long charId) : base(botClient, charId)
        {
        }


        // text message of artist name for search
        private static bool IsArtistName(Update current, List<Update> previousMessage)
        {
            if (!previousMessage.Any() || current.Message == null || previousMessage.Last().CallbackQuery == null)
                return false;

            int numItem = DictionaryKeyboardMarkup.GetNumItem(previousMessage.Last());

            return DictionaryKeyboardMarkup.GetNameGroup(numItem) == groupSongOrArtist && DictionaryKeyboardMarkup.GetData(numItem) == "artist";
        }


        // chose "song" from["song","artist"]
        private static bool IsChosenOptionArtist(Update currentMessage)
        {
            int numItem = DictionaryKeyboardMarkup.GetNumItem(currentMessage);

            return DictionaryKeyboardMarkup.GetNameGroup(numItem) == groupSongOrArtist && DictionaryKeyboardMarkup.GetData(numItem) == "artist";
        }

        public override bool Handle(Update request, List<Update> previousMessage)
        {

            try
            {
                return _nextHandler.Handle(request, previousMessage);

                if (request.Message is { } message)
                {
                    if (IsArtistName(request, previousMessage))
                    {
                        var listOfInfo = MainItem.songDowloaderSefon.GetInfoSong(message.Text, 2);

                        if (listOfInfo.Count() == 0)
                        {
                            _botClient.SendTextMessageAsync(
                                text: "Not Found Artist",
                                chatId: _chatId
                                );
                            return true;
                        }

                        var info = listOfInfo.First();

                        _botClient.SendTextMessageAsync(
                            text: $"Find artist name is {info.artist}",
                            chatId: _chatId
                            );

                        // DowloadSongsArtistToFolder(info.urlArtist);

                        //foreach (var filename in files)
                        //{
                        //    SendFileAsync(filename);
                        //}

                        _botClient.SendTextMessageAsync(
                            text: $"You can see all files on the: {NetworkItem._urlWebStorage}",
                            chatId: _chatId
                            );


                        MainItem.songDowloaderSefon.CopyAllFilesToStorageServer();

                        return true;
                    }
                }
                else if (IsChosenOptionArtist(request))
                {
                    _botClient.SendMessage(
                        chatId: _chatId,
                        text: "Enter the artist name"
                        );

                    return true;
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
            _mainInlineKeyboard = DictionaryKeyboardMarkup.CreateInlineKeyboard([("Song", "song"), ("Artist(not avaliable)", "artist")], groupSongOrArtist);
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
                            chatId: _chatId,
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

                throw ex;

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
