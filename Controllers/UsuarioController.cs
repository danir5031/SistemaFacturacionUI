using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SistemaFacturacionUI.Models;
using System;
using System.Linq;

namespace SistemaFacturacionUI.Controllers
{
    public class UsuarioController : Controller
    {
        private readonly AppDbContext _context;

        public UsuarioController(AppDbContext context)
        {
            _context = context;
        }

        //////////////////////////////////////////////////////
        // VALIDAR SUPER ADMIN
        //////////////////////////////////////////////////////

        private bool EsSuperAdmin()
        {
            var usuario = HttpContext.Session.GetString("usuario");

            return usuario == "admin";
        }

        //////////////////////////////////////////////////////
        // INDEX
        //////////////////////////////////////////////////////

        public IActionResult Index()
        {
            //////////////////////////////////////////////////////
            // SOLO SUPER ADMIN
            //////////////////////////////////////////////////////

            if (!EsSuperAdmin())
                return RedirectToAction("Index", "Home");

            //////////////////////////////////////////////////////
            // NO MOSTRAR SUPER ADMIN
            //////////////////////////////////////////////////////

            var usuarios = _context.Usuarios
                .Where(x => x.Usuario1 != "admin")
                .OrderByDescending(x => x.IdUsuario)
                .ToList();

            return View(usuarios);
        }

        //////////////////////////////////////////////////////
        // CREAR
        //////////////////////////////////////////////////////

        [HttpPost]
        public JsonResult Crear([FromBody] Usuario usuario)
        {
            try
            {
                //////////////////////////////////////////////////////
                // SOLO SUPER ADMIN
                //////////////////////////////////////////////////////

                if (!EsSuperAdmin())
                    return Json("No autorizado");

                //////////////////////////////////////////////////////
                // VALIDAR
                //////////////////////////////////////////////////////

                if (string.IsNullOrWhiteSpace(usuario.Usuario1))
                    return Json("Ingrese usuario");

                if (string.IsNullOrWhiteSpace(usuario.Password))
                    return Json("Ingrese password");

                //////////////////////////////////////////////////////
                // VALIDAR EXISTE
                //////////////////////////////////////////////////////

                bool existe = _context.Usuarios
                    .Any(x => x.Usuario1 == usuario.Usuario1);

                if (existe)
                    return Json("El usuario ya existe");

                //////////////////////////////////////////////////////
                // CREAR
                //////////////////////////////////////////////////////

                usuario.Activo = true;

                usuario.FechaRegistro = DateTime.Now;

                _context.Usuarios.Add(usuario);

                _context.SaveChanges();

                return Json(true);
            }
            catch (Exception ex)
            {
                return Json(
                    ex.InnerException?.Message ??
                    ex.Message
                );
            }
        }

        //////////////////////////////////////////////////////
        // EDITAR
        //////////////////////////////////////////////////////

        [HttpPost]
        public JsonResult Editar([FromBody] Usuario usuario)
        {
            try
            {
                //////////////////////////////////////////////////////
                // SOLO SUPER ADMIN
                //////////////////////////////////////////////////////

                if (!EsSuperAdmin())
                    return Json("No autorizado");

                //////////////////////////////////////////////////////
                // BUSCAR
                //////////////////////////////////////////////////////

                var db = _context.Usuarios
                    .FirstOrDefault(x =>
                        x.IdUsuario == usuario.IdUsuario);

                if (db == null)
                    return Json("Usuario no encontrado");

                //////////////////////////////////////////////////////
                // BLOQUEAR SUPER ADMIN
                //////////////////////////////////////////////////////

                if (db.Usuario1 == "admin")
                    return Json("No permitido");

                //////////////////////////////////////////////////////
                // VALIDAR DUPLICADO
                //////////////////////////////////////////////////////

                bool existe = _context.Usuarios
                    .Any(x =>
                        x.Usuario1 == usuario.Usuario1
                        &&
                        x.IdUsuario != usuario.IdUsuario);

                if (existe)
                    return Json("El usuario ya existe");

                //////////////////////////////////////////////////////
                // ACTUALIZAR
                //////////////////////////////////////////////////////

                db.Nombre = usuario.Nombre;
                db.Usuario1 = usuario.Usuario1;
                db.Password = usuario.Password;
                db.Rol = usuario.Rol;

                _context.SaveChanges();

                return Json(true);
            }
            catch (Exception ex)
            {
                return Json(
                    ex.InnerException?.Message ??
                    ex.Message
                );
            }
        }

        //////////////////////////////////////////////////////
        // ELIMINAR
        //////////////////////////////////////////////////////

        [HttpPost]
        public JsonResult Eliminar(int id)
        {
            try
            {
                //////////////////////////////////////////////////////
                // SOLO SUPER ADMIN
                //////////////////////////////////////////////////////

                if (!EsSuperAdmin())
                    return Json("No autorizado");

                //////////////////////////////////////////////////////
                // BUSCAR
                //////////////////////////////////////////////////////

                var usuario = _context.Usuarios
                    .FirstOrDefault(x => x.IdUsuario == id);

                if (usuario == null)
                    return Json("Usuario no encontrado");

                //////////////////////////////////////////////////////
                // BLOQUEAR SUPER ADMIN
                //////////////////////////////////////////////////////

                if (usuario.Usuario1 == "admin")
                    return Json("No permitido");

                //////////////////////////////////////////////////////
                // ELIMINAR
                //////////////////////////////////////////////////////

                _context.Usuarios.Remove(usuario);

                _context.SaveChanges();

                return Json(true);
            }
            catch (Exception ex)
            {
                return Json(
                    ex.InnerException?.Message ??
                    ex.Message
                );
            }
        }
    }
}