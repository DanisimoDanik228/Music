using System;
using System.Net.Mime;
using Test_1.Dowloaders;

namespace Test_1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            var app = builder.Build();
            app.UseSession();

            app.MapGet("/", async (HttpContext context) => {
                context.Response.Headers.ContentType = "text/html; charset=utf-8";

                await context.Response.SendFileAsync("Html\\Index.html");
            });

            app.MapPost("/",async (HttpContext context) => {
                
                var form = await context.Request.ReadFormAsync();
                var nameSong = form["nameSong"];


                var songs = await DowloadMp3Party.GetInfoSong(nameSong);

                string destinationFolder = @"C:\Users\Werty\Desktop\test\" + Guid.NewGuid().ToString();

                Directory.CreateDirectory(destinationFolder);

               await Dowloader.DowloadFilesAsync(songs, destinationFolder);

                var zipFile = Dowloader.CompresToZip(destinationFolder);

                string resultHtml = $@"
                    <h3>Найденные ссылки:</h3>
                    {string.Join("<br>", songs.Select(l => $"<a href='{l.dowloadLink}' target='_blank'>{(l.artist + " - " + l.songName)}</a>"))}
                    <br><br><a href='/'>Назад</a>

                    <form method='post' action='/dowload'>
                        <button type='submit'>Скачать zip</button>
                    </form>
                ";

                context.Session.SetString("ZipFile", zipFile);

                context.Response.Headers.ContentType = "text/html; charset=utf-8";
                await context.Response.WriteAsync(resultHtml);
            });

            app.MapPost("/dowload", async (HttpContext context) => {
                var zipFile = context.Session.GetString("ZipFile");

                if (zipFile == null || !File.Exists(zipFile))
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync(@"
                            <html>
                                <body>
                                    <h3>Файл не найден или сессия устарела</h3>
                                    <a href='/'>Вернуться на главную</a>
                                </body>
                            </html>
                        ");

                    return;
                }
                else
                {
                    var fileName = Path.GetFileName(zipFile);

                    context.Response.Headers.ContentType = "application/zip";
                    context.Response.Headers.ContentDisposition = $"attachment; filename=\"{fileName}\"";

                    await context.Response.SendFileAsync(zipFile);

                    //File.Delete(zipFile);
                    context.Session.Remove("ZipFile");
                }
            });

            app.Run();
        }
    }
}
