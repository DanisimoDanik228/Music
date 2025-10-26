using System;
using System.Net.Mime;
using Test_1.Models.Dowloaders;
using Test_1.Models;
using DotNetEnv;
using Test_1.Services;

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

            builder.Services.AddRazorPages();

            builder.Services.AddScoped<ProductManager>();

            Env.Load("./Core/.env");
            builder.Configuration.AddEnvironmentVariables();

            var app = builder.Build();
            app.UseStaticFiles();
            app.UseSession();

            app.MapRazorPages();

            app.Run();
        }
    }
}
