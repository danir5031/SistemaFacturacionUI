using Microsoft.AspNetCore.Mvc;
using SistemaFacturacionUI.Models;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System;

namespace SistemaFacturacionUI.Controllers
{
    public class LoginController : Controller
    {
        private readonly AppDbContext _context;

        public LoginController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Index(string user, string password)
        {
            // 🔥 BUSCAR EN BASE DE DATOS
            var usuario = _context.Usuarios
                .FirstOrDefault(u => u.Usuario1 == user && u.Password == password);

            if (usuario != null)
            {
                var rol = usuario.Rol.Trim().ToUpper();

                HttpContext.Session.SetString("usuario", usuario.Usuario1);
                HttpContext.Session.SetString("nombre", usuario.Nombre);
                HttpContext.Session.SetString("rol", rol);

                if (rol == "ADMIN")
                {
                    return RedirectToAction("Index", "Home");
                }
                else if (rol == "EMPLEADO")
                {
                    return RedirectToAction("Index", "Empleado");
                }
            }

            ViewBag.Error = "Usuario o contraseña incorrectos";
            return View();
        }

        [HttpPost]
        public IActionResult KeepAlive()
        {
            // Si existe sesión la renovamos
            if (HttpContext.Session.GetString("usuario") != null)
            {
                HttpContext.Session.SetString(
                    "ultimaActividad",
                    DateTime.Now.ToString()
                );

                return Ok();
            }

            return Unauthorized();
        }

        // 🔥 LOGOUT
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Login");
        }
    }
}