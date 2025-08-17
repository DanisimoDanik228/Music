using Microsoft.Extensions.FileProviders;
using Music.MainFunction;
using Music.PostgresSQL;
using OpenQA.Selenium.DevTools.V136.WebAuthn;
using OpenQA.Selenium.Interactions;
using System;
using System.Collections.Generic;
using System.IO.Compression;
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
        IEnumerable<ResponseHandler> Handle(Update request, List<Update> previousMessage);
    }

    public abstract class AbstractHandler : IHandler
    {
        public static Dictionary<string, string> chosenReplyMarkup = new();

        public const string groupNameSong = "song_names";
        public const string groupSongOrArtist = "song_artist";

        public IHandler _nextHandler { get; private set; }

        public IHandler SetNext(IHandler handler)
        {
            _nextHandler = handler;
            return handler;
        }

        public abstract IEnumerable<ResponseHandler> Handle(Update request, List<Update> previousMessage);

        public AbstractHandler()
        {
        }
    }

    public class ChosenSongName : AbstractHandler
    {


        // list of find songs
        private static bool IsChosenSongName(Update currentMessage)
        {
            if (currentMessage.CallbackQuery == null)
                return false;

            int numItem = DictionaryKeyboardMarkup.GetNumItem(currentMessage);

            return DictionaryKeyboardMarkup.GetNameGroup(numItem) == groupNameSong;
        }
        public override IEnumerable<ResponseHandler> Handle(Update request, List<Update> previousMessage)
        {
            if (IsChosenSongName(request))
            {
                ResponseHandler responseFiles = new();
                responseFiles.files = new List<string>();

                var numItem = DictionaryKeyboardMarkup.GetNumItem(request);

                string data = DictionaryKeyboardMarkup.GetData(numItem);

                var _chatId = request.CallbackQuery.Id;

                var dowloadFolder = Path.Combine(MainItem.directoryDowload, _chatId.ToString(), MainItem.CurrentTime());
                if (!Directory.Exists(dowloadFolder))
                    Directory.CreateDirectory(dowloadFolder);

                if (data == "_dowload_all_")
                {

                    var nameGroup = DictionaryKeyboardMarkup.GetNameGroup(numItem);
                    var range = DictionaryKeyboardMarkup.GetRangeGroup(nameGroup);

                    var massiveFilename = new List<string>();


                    Parallel.For(range.left, range.right, i =>
                    {
                        var xmlData = DictionaryKeyboardMarkup.GetData(i);
                        InfoSong info = MainItem.DeSerialize(xmlData);
                        var filename = MainItem.DowloadMusicFromAllSource(info, dowloadFolder);

                        responseFiles.files.Add(filename);

                        var item = new TextSample();
                        item.SongName = info.songName;
                        item.UrlDownload = info.songUrl;
                        item.FingerPrint = MainItem.MakeFingerprint(filename);
                    });

                    var zipFile = MainItem.GetUniqueFileName(Path.Combine(MainItem.webStorage, previousMessage.Last().Message.Text + ".zip"));
                    ZipFile.CreateFromDirectory(dowloadFolder, zipFile);
                }
                else
                {
                    InfoSong info = MainItem.DeSerialize(data);

                    var filename = MainItem.DowloadMusicFromAllSource(info, dowloadFolder);

                    responseFiles.files.Add(filename);
                }


                ResponseHandler responseSite = new();
                responseSite.text = $"You can see all files on the: {NetworkItem._urlWebStorage}";

                MainItem.CopyAllFilesToStorageServerFromAllSource();

                return [responseFiles,responseSite];
            }

            return _nextHandler.Handle(request, previousMessage);
        }
    }

    public class ChosenOptionFind : AbstractHandler
    {


        // chose "song" from["song","artist"]
        private static bool IsChosenOptionFind(Update currentMessage)
        {
            if (currentMessage.CallbackQuery == null)
                return false;

            int numItem = DictionaryKeyboardMarkup.GetNumItem(currentMessage);

            return DictionaryKeyboardMarkup.GetNameGroup(numItem) == groupSongOrArtist && DictionaryKeyboardMarkup.GetData(numItem) == "song";
        }

        public override IEnumerable<ResponseHandler> Handle(Update request, List<Update> previousMessage)
        {
            if (IsChosenOptionFind(request))
            {
                return [new ResponseHandler("Enter the song name")];
            }

            return _nextHandler.Handle(request, previousMessage);
        }
    }

    public class SongNameHandler : AbstractHandler
    {
        // text message of song name for search
        private static bool IsSongName(Update current,List<Update> previousMessage)
        {
            if (!previousMessage.Any() || current.Message == null || previousMessage.Last().CallbackQuery== null)
                return false;

            int numItem = DictionaryKeyboardMarkup.GetNumItem(previousMessage.Last());

            return DictionaryKeyboardMarkup.GetNameGroup(numItem) == groupSongOrArtist && DictionaryKeyboardMarkup.GetData(numItem) == "song";
        }

        public override IEnumerable<ResponseHandler> Handle(Update request, List<Update> previousMessage)
        { 
            if (IsSongName(request, previousMessage))
            {
                var infoSongs = MainItem.GetInfoSongFromAllSource(request.Message.Text, 3);

                if (infoSongs.Count() == 0)
                {
                    return [new ResponseHandler("No Found Song")];
                }

                List<(string, string)> tempList = new();

                foreach (var item in infoSongs)
                {
                    string sogInfoInString = MainItem.Serialize(item);
                    tempList.Add(($"{item.artist} - {item.songName}", sogInfoInString));
                }

                tempList.Add(($"[Dowload ALL]", "_dowload_all_"));


                var response = new ResponseHandler();

                response.text = "Find songs";
                response.markup = DictionaryKeyboardMarkup.CreateInlineKeyboard(tempList, groupNameSong);

                return [response];
            }

            return _nextHandler.Handle(request, previousMessage);
        }
    }
    
    public class InitHandler : AbstractHandler
    {
        private InlineKeyboardMarkup _mainInlineKeyboard;

        public InitHandler() 
        {
            _mainInlineKeyboard = DictionaryKeyboardMarkup.CreateInlineKeyboard([("Song", "song"), ("Artist(not avaliable)", "artist")], groupSongOrArtist);
        }
        public override IEnumerable<ResponseHandler> Handle(Update request, List<Update> previousMessage)
        {
            if (request.Message is { } message)
            {
                if (message.Text == "/start")
                { 
                    var response = new ResponseHandler();

                    response.text = "Choose what do you want to find";
                    response.markup = _mainInlineKeyboard;

                    return [response];
                }
            }

            return _nextHandler.Handle(request,previousMessage);
        }
    }
    
    public class LastHandler : AbstractHandler
    {

        public override IEnumerable<ResponseHandler> Handle(Update request, List<Update> previousMessage) => null;
    }

    public class ResponseHandler
    {
        public string text = null;

        public List<string> files = null;

        public InlineKeyboardMarkup markup = null;

        public ResponseHandler(string text)
        {
            this.text = text;
        }

        public ResponseHandler()
        {
            
        }

        public override string ToString()
        {
            StringBuilder str = new();

            if (text != null)
            {
                str.AppendLine("text: " + text);
            }

            if (files != null)
            {
                str.Append("files: ");

                foreach (var item in files)
                    str.Append($"{Path.GetFileName(item)},");

                str.AppendLine();
            }

            if (markup != null)
            {
                str.AppendLine($"markup: exist in {markup.InlineKeyboard.Count()}-th count");
            }

            return str.ToString();
        }
    }
}
