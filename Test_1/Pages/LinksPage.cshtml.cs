using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using Test_1.Models;

namespace Test_1.Pages
{
    public class LinksPageModel : PageModel
    {
        public List<ResultInfoSong> Songs { get; set; }
        public void OnGet()
        {
            if (TempData["Songs"] != null)
            {
                string jsonData = TempData["Songs"] as string ?? "";
                Songs = JsonSerializer.Deserialize<List<ResultInfoSong>>(jsonData) ?? new();
            }
        }
    }
}
