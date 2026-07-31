using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SistemaFacturacionUI.Models;
using System;

namespace SistemaFacturacionUI
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            // 🔥 SESSION
            services.AddSession(options =>
            {
                // La sesión expira tras 6 horas SIN actividad
                options.IdleTimeout = TimeSpan.FromHours(8);

                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;

                // Nombre personalizado de la cookie
                options.Cookie.Name = "SistemaFacturacion.Session";

                // Solo HTTPS cuando exista
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

                // Protección CSRF
                options.Cookie.SameSite = SameSiteMode.Lax;
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseSession(); // 🔥 ACTIVA SESIÓN

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Login}/{action=Index}/{id?}");
            });
        }
    }
}