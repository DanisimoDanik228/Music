using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Sprache;
using System.IO;

namespace Test_1.Pages
{
    public class ZipPageModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public ZipPageModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public IActionResult OnGet()
        {
            string? zipPath = TempData["ZipFile"] as string;
            string fileName = "your cool.zip";

            Response.Headers["Content-Disposition"] = $"attachment; filename=\"{fileName}\"";

            var stream = System.IO.File.OpenRead(zipPath);
            var result = File(stream, "application/zip");

            TempData.Remove("ZipFile");

            return result;
        }
    }
}
