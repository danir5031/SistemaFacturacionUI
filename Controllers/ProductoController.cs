using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SistemaFacturacionUI.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaFacturacionUI.Controllers
{
    public class ProductoController : Controller
    {
        private readonly AppDbContext _context;

        public ProductoController(AppDbContext context)
        {
            _context = context;
        }

        // 🔥 SOLO ACTIVOS
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("usuario") == null)
                return RedirectToAction("Index", "Login");

            var productos = _context.Productos
                .Where(x => x.Activo)
                .OrderByDescending(x => x.FechaRegistro)
                .ToList();

            return View(productos);
        }

        // 🔥 GUARDAR (CREAR / EDITAR)
        [HttpPost]
        public async Task<JsonResult> Guardar([FromForm] Producto producto, IFormFile imagenFile)
        {
            var rol = (HttpContext.Session.GetString("rol") ?? "").ToUpper();

            if (rol != "ADMIN")
                return Json(null);

            // 🔥 VALIDAR DUPLICADO
            var existe = _context.Productos
                .Any(x => x.Nombre == producto.Nombre &&
                          x.IdProducto != producto.IdProducto &&
                          x.Activo);

            if (existe)
                return Json("duplicado");

            // 🔥 IMAGEN
            if (imagenFile != null && imagenFile.Length > 0)
            {
                using (var ms = new MemoryStream())
                {
                    await imagenFile.CopyToAsync(ms);
                    producto.Imagen = ms.ToArray();
                    producto.ImagenMime = imagenFile.ContentType;
                }
            }

            if (producto.IdProducto == 0)
            {
                producto.FechaRegistro = DateTime.Now;
                producto.Activo = true;

                _context.Productos.Add(producto);
            }
            else
            {
                var db = _context.Productos.Find(producto.IdProducto);

                if (db != null)
                {
                    db.Nombre = producto.Nombre;
                    db.Categoria = producto.Categoria;
                    db.Descripcion = producto.Descripcion;
                    db.PrecioVenta = producto.PrecioVenta;
                    db.Costo = producto.Costo;
                    db.Stock = producto.Stock;
                    db.ImagenUrl = producto.ImagenUrl;
                    db.Disponible = producto.Disponible;

                    if (producto.Imagen != null)
                    {
                        db.Imagen = producto.Imagen;
                        db.ImagenMime = producto.ImagenMime;
                    }
                }
            }

            _context.SaveChanges();
            return Json(producto);
        }

        // 🔥 VER IMAGEN
        public IActionResult VerImagen(int id)
        {
            var producto = _context.Productos.Find(id);

            if (producto?.Imagen != null)
                return File(producto.Imagen, producto.ImagenMime);

            return NotFound();
        }

        // 🔥 ENVIAR A PAPELERA
        [HttpPost]
        public JsonResult Eliminar(int id)
        {
            var rol = (HttpContext.Session.GetString("rol") ?? "").ToUpper();

            if (rol != "ADMIN")
                return Json(false);

            var producto = _context.Productos.Find(id);

            if (producto != null)
            {
                producto.Activo = false;
                _context.SaveChanges();
            }

            return Json(true);
        }

        // 🔥 VER ELIMINADOS
        public IActionResult Eliminados()
        {
            var lista = _context.Productos
                .Where(x => !x.Activo)
                .OrderByDescending(x => x.FechaRegistro)
                .ToList();

            return View(lista);
        }

        // 🔥 RESTAURAR
        [HttpPost]
        public JsonResult Restaurar(int id)
        {
            var p = _context.Productos.Find(id);

            if (p != null)
            {
                p.Activo = true;
                _context.SaveChanges();
            }

            return Json(true);
        }

        // 🔥 ELIMINAR DEFINITIVO
        [HttpPost]
        public JsonResult EliminarDefinitivo(int id)
        {
            var p = _context.Productos.Find(id);

            if (p != null)
            {
                _context.Productos.Remove(p);
                _context.SaveChanges();
            }

            return Json(true);
        }
    }
}