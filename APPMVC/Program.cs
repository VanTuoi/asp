using APPMVC.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;

namespace APPMVC
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Maximum 100MB (Global)
            builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 104857600);
            builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 104857600);

            // Add services to the container.
            builder.Services.AddRouting(options => options.LowercaseUrls = true);
            builder.Services.AddControllersWithViews();

            builder.Services.AddScoped<UserRepositories>();
            builder.Services.AddScoped<DocumentRepositories>();

            // SESSION:
            // builder.Services.AddSession(o => o.IdleTimeout = TimeSpan.FromMinutes(20));

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/auth/login";
                    options.LogoutPath = "/auth/logout";
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
                });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/home/error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            // BẬT DÒNG NÀY NẾU ĐỀ YÊU CẦU DÙNG SESSION:
            // app.UseSession();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=home}/{action=index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
