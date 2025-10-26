using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.IO.Compression;
using System.Runtime.InteropServices.ComTypes;
using System.Text.Json;
using Test_1.Models;
using Test_1.Models.Dowloaders;
using Test_1.Models.SiteParse;
using Test_1.Models.TestData;
using Test_1.Services;

namespace Test_1.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly ProductManager _productManager;

        public IndexModel(IConfiguration configuration, ProductManager productManager)
        {
            _configuration = configuration;
            _productManager = productManager;
        }
        public void OnGet()
        {

        }

        [BindProperty]
        [Required(ErrorMessage = "Поле 'Название песни' обязательно для заполнения")]
        public string NameSong { get; set; }
        public async Task<IActionResult> OnPost()
        {
            if (!ModelState.IsValid)
                return Page();

            (string zipFile, List<InfoSong> songs) = await _productManager.FindMusic(NameSong,_configuration);

            // For LinkPage.cshtml
            var json = JsonSerializer.Serialize<List<InfoSong>>(songs);
            TempData["Songs"] = json;

            // For ZipPage.cshtml
            TempData["ZipFile"] = zipFile;

            return RedirectToPage("LinksPage");
        }
    }
}
