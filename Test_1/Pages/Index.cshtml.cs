using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.IO.Compression;
using System.Runtime.InteropServices.ComTypes;
using System.Text.Json;
using Test_1.Models;
using Test_1.Models.Dowloaders;
using Test_1.Models.TestData;

namespace Test_1.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public IndexModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public void OnGet()
        {

        }

        [BindProperty]
        [Required(ErrorMessage = "Поле 'Название песни' обязательно для заполнения")]
        public string NameSong { get; set; }
        public async Task<IActionResult> OnPost()
        {
            string zipFile;
            List<InfoSong> songs;

            if (_configuration["DEBUG_USE_LOCAL_DATA"] == "true")
            {
                songs = Fake_SongData.Get(_configuration["DEBUG_PATH_JSON"]);
                zipFile = _configuration["DEBUG_PATH_ZIP"];
            }
            else
            {
                if (!ModelState.IsValid)
                    return Page();

                songs = await DowloadMp3Party.GetInfoSong(NameSong);

                var destinationFolder = @"C:\Users\Werty\Desktop\test\" + Guid.NewGuid().ToString();

                Directory.CreateDirectory(destinationFolder);

                await Dowloader.DowloadFilesAsync(songs, destinationFolder);

                zipFile = Dowloader.CompresToZip(destinationFolder);
            }

            // For LinkPage.cshtml
            TempData["Songs"] = JsonSerializer.Serialize<List<InfoSong>>(songs);

            // For ZipPage.cshtml
            TempData["ZipFile"] = zipFile;

            return RedirectToPage("LinksPage");
        }
    }
}
