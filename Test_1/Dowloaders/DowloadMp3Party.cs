using HtmlAgilityPack;
using System;
using System.Buffers.Text;
using System.Net.Http;
using System.Text;
using Test_1.Models;

namespace Test_1.Dowloaders
{
    public class DowloadMp3Party
    {
        private const string urlSite = "https://mp3party.net";

        private async static Task<InfoSong> GetAddintionalInfo(string urlMusic, HttpClient httpClient)
        {
            InfoSong res = new InfoSong();

            // happy link ->  href="/yabanner?url=https%3A%2F%2Fdl2.mp3party.net%2Fdownload%2F8952738
            // %3A  this :
            // %2F  this / 

            var htmlContent = await httpClient.GetStringAsync(urlMusic);
            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);

            var downloadTag = doc.DocumentNode.SelectNodes("//a[contains(@class, 'c-button c-button_download js-dw-btn')]")[0];
            var downloadLink = downloadTag.GetAttributeValue("href", "");
            string decodedDownloadUrl = Uri.UnescapeDataString(downloadLink.Substring(downloadLink.IndexOf("https")));

            res.dowloadLink = decodedDownloadUrl;
            res.songUrl = urlMusic;



            var mainSection = doc.DocumentNode.SelectNodes("//div[contains(@class, 'breadcrumbs')]");

            var items = mainSection[0].SelectNodes("//span[contains(@itemprop, 'name')]");

            //items[1] - artist
            //items[2] - song
            
            res.artistUrl = items[1].ParentNode.GetAttributeValue("href", "");
            res.artist = items[1].InnerText.Trim();
            res.songName = items[2].InnerText.Trim();

            return res;
        }
        public async static Task<List<InfoSong>> GetInfoSong(string inputName)
        {
            string searchUrl  = CreateUrlForSearch(inputName);

            List<InfoSong> res = new();

            HttpClient httpClient = new HttpClient();
            var htmlContent = await httpClient.GetStringAsync(searchUrl);
            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);

            var mainSection = doc.DocumentNode.SelectNodes("//div[contains(@class, 'playlist')]");

            var songPages = mainSection[0].SelectNodes(".//a[contains(@href, '/music/')]");


            if (songPages != null)
            {
                for (int i = 0; i < songPages.Count; i += 2)
                {
                    var urlMusic = songPages[i].GetAttributeValue("href", "");

                    string musicUrl = urlSite + urlMusic;

                    var t = await GetAddintionalInfo(musicUrl, httpClient);


                    res.Add(t);
                }
            }


            return res;
        }

        public static string CreateUrlForSearch(string inputName)
        {
            return $"https://mp3party.net/search?q={inputName.Replace(" ", "%20")}";
        }

    }
}
