using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SistemaFacturacionUI.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaFacturacionUI.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public IActionResult Index()
        {
            var usuario = HttpContext.Session.GetString("usuario");
            var rol = HttpContext.Session.GetString("rol");

            // 🔒 NO LOGIN
            if (usuario == null)
            {
                return RedirectToAction("Index", "Login");
            }

            // 🔒 SOLO ADMIN PUEDE VER DASHBOARD
            if (rol != "ADMIN")
            {
                return RedirectToAction("Index", "Empleado");
            }

            ViewBag.User = usuario;
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
