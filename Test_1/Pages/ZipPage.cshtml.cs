using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Sprache;
using System.IO;

namespace Test_1.Pages
{
    public class ZipPageModel : PageModel
    {
        public ZipPageModel()
        {
        }
        public IActionResult OnGet()
        {
            if (TempData["ZipFile"] != null)
            { 
                string zipPath = TempData["ZipFile"] as string ?? ""; ;
                string fileName = "your cool.zip";

                Response.Headers["Content-Disposition"] = $"attachment; filename=\"{fileName}\"";

                var stream = System.IO.File.OpenRead(zipPath);
                var result = File(stream, "application/zip");

                return result;
            }

            return Page();
        }
    }
}
