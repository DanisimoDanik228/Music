
using HtmlAgilityPack;

namespace Test_1.Models.SiteParse
{
    public class ParseMuzofond : IParseSite
    {

        private const string urlSite = "https://muzofond.fm";
        public static async Task<List<InfoSong>> GetInfoSong(string inputName)
        {
            string searchUrl = CreateUrlForSearch(inputName);

            List<InfoSong> res = new();

            HttpClient httpClient = new HttpClient();
            var htmlContent = await httpClient.GetStringAsync(searchUrl);
            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);

            var songSection = doc.DocumentNode.SelectNodes("//li[contains(@class, 'item')]");


            if (songSection != null)
            {
                for (int i = 0; i < int.Min(songSection.Count, IParseSite._maxCountSong); i ++)
                {
                    var dowloadLink = songSection[i].SelectNodes(".//li[contains(@class, 'play')]")[0].GetAttributeValue("data-url", "");

                    //var titleSection = songSection[i].SelectNodes("//h3");

                    var songName = songSection[i].SelectNodes(".//span[contains(@class, 'artist')]")[0].InnerText.Trim();
                    var artistSection = songSection[i].SelectNodes(".//span[contains(@class, 'track')]")[0];
                    var artistName = artistSection.InnerText.Trim();

                    var artistUrl = artistSection.ParentNode.GetAttributeValue("href","");

                    string songUrl = "undef";



                    InfoSong t = new InfoSong(Enum.SourseSite.Muzofond) {
                        artist = artistName,
                        songName = songName.ToString(),
                        artistUrl = artistUrl,
                        dowloadLink = dowloadLink.ToString(),
                        songUrl = songUrl
                    };


                    res.Add(t);
                }
            }


            return res;
        }

        private static string CreateUrlForSearch(string inputName)
        {
            return $"https://muzofond.fm/search/{inputName.Replace(" ", "%20")}";
        }
    }
}
