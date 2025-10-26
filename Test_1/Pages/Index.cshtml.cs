using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.IO.Compression;
using System.Runtime.InteropServices.ComTypes;
using System.Text.Json;
using Test_1.Dowloaders;
using Test_1.Models;
using Test_1.TestData;

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
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Название песни не может быть пустым")]
        public string NameSong { get; set; }
        public async Task<IActionResult> OnPost()
        {
            string jsonFile;
            string zipFile;

            if (_configuration["DEBUG_USE_LOCAL_DATA"] == "true")
            {
                var songs = Fake_SongData.Get();

                jsonFile = System.IO.File.ReadAllText(_configuration["DEBUG_PATH_JSON"]);
                zipFile = _configuration["DEBUG_PATH_ZIP"];
            }
            else
            {
                if (!ModelState.IsValid)
                    return Page();

                var songs = await DowloadMp3Party.GetInfoSong(NameSong);

                jsonFile = JsonSerializer.Serialize(songs);

                var destinationFolder = @"C:\Users\Werty\Desktop\test\" + Guid.NewGuid().ToString();

                Directory.CreateDirectory(destinationFolder);

                await Dowloader.DowloadFilesAsync(songs, destinationFolder);

                zipFile = Dowloader.CompresToZip(destinationFolder);
            }

                
            TempData["Songs"] = jsonFile;
            TempData["ZipFile"] = zipFile;

            return RedirectToPage("LinksPage");
        }
    }
}
