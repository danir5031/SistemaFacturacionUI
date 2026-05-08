using Microsoft.AspNetCore.Mvc;
using SistemaFacturacionUI.Models;
using System;
using System.Linq;

namespace SistemaFacturacionUI.Controllers
{
    public class ClienteController : Controller
    {
        private readonly AppDbContext _context;

        public ClienteController(AppDbContext context)
        {
            _context = context;
        }

        // 🔥 CLIENTES ACTIVOS
        public IActionResult Index()
        {
            var clientes = _context.Clientes
                .Where(x => x.Activo == true)
                .OrderByDescending(x => x.FechaRegistro)
                .ToList();

            return View(clientes);
        }

        // 🔥 CLIENTES ELIMINADOS
        public IActionResult Eliminados()
        {
            var clientes = _context.Clientes
                .Where(x => x.Activo == false)
                .OrderByDescending(x => x.FechaRegistro)
                .ToList();

            return View(clientes);
        }

        // 🔥 CREAR
        [HttpPost]
        public JsonResult Crear([FromBody] Cliente cliente)
        {
            if (cliente == null) return Json(null);

            cliente.FechaRegistro = DateTime.Now;
            cliente.Activo = true;

            _context.Clientes.Add(cliente);
            _context.SaveChanges();

            return Json(cliente);
        }

        // 🔥 EDITAR
        [HttpPost]
        public JsonResult Editar([FromBody] Cliente cliente)
        {
            var db = _context.Clientes.Find(cliente.Idcliente);

            if (db != null)
            {
                db.Nombre = cliente.Nombre;
                db.Telefono = cliente.Telefono;
                db.Direccion = cliente.Direccion;

                _context.SaveChanges();
            }

            return Json(db);
        }

        // 🔥 ELIMINAR (SOFT DELETE)
        [HttpPost]
        public JsonResult Eliminar(int id)
        {
            var cliente = _context.Clientes.Find(id);

            if (cliente != null)
            {
                cliente.Activo = false;
                _context.SaveChanges();
            }

            return Json(true);
        }

        // 🔥 ELIMINAR MÚLTIPLE
        [HttpPost]
        public JsonResult EliminarMultiple([FromBody] int[] ids)
        {
            var clientes = _context.Clientes.Where(x => ids.Contains(x.Idcliente)).ToList();

            foreach (var c in clientes)
            {
                c.Activo = false;
            }

            _context.SaveChanges();

            return Json(true);
        }

        // 🔥 RESTAURAR
        [HttpPost]
        public JsonResult Restaurar(int id)
        {
            var cliente = _context.Clientes.Find(id);

            if (cliente != null)
            {
                cliente.Activo = true;
                _context.SaveChanges();
            }

            return Json(true);
        }

        // 🔥 ELIMINAR DEFINITIVO
        [HttpPost]
        public JsonResult EliminarDefinitivo(int id)
        {
            var cliente = _context.Clientes.Find(id);

            if (cliente != null)
            {
                _context.Clientes.Remove(cliente);
                _context.SaveChanges();
            }

            return Json(true);
        }
    }
}