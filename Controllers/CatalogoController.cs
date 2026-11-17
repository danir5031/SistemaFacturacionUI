using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaFacturacionUI.Models;
using System.Linq;

namespace SistemaFacturacionUI.Controllers
{
    [AllowAnonymous]
    public class CatalogoController : Controller
    {
        private readonly AppDbContext _context;

        public CatalogoController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index(
            string categoria = "",
            string buscar = "")
        {
            var query = _context.Productos
                .Include(x => x.Variantes)
                .Where(x =>
                    x.Activo &&
                    x.Disponible &&
                    x.Stock > 0 &&
                    x.MostrarEnCatalogo
                )
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                buscar = buscar.Trim().ToLower();

                query = query.Where(x =>
                    x.Nombre.ToLower().Contains(buscar) ||
                    x.Categoria.ToLower().Contains(buscar)
                );
            }

            if (!string.IsNullOrWhiteSpace(categoria))
            {
                query = query.Where(x =>
                    x.Categoria == categoria
                );
            }

            var productos = query
                .OrderByDescending(x => x.Stock)
                .ThenByDescending(x => x.FechaRegistro)
                .ToList();

            var categorias = _context.Productos
                .Where(x =>
                    x.Activo &&
                    x.Disponible &&
                    x.Stock > 0 &&
                    x.MostrarEnCatalogo &&
                    x.Categoria != null &&
                    x.Categoria != ""
                )
                .Select(x => x.Categoria)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            ViewBag.Categorias = categorias;
            ViewBag.CategoriaSeleccionada = categoria;
            ViewBag.Buscar = buscar;

            return View(productos);
        }

        [HttpGet]
        public IActionResult VerImagen(int id)
        {
            var producto = _context.Productos
                .FirstOrDefault(x => x.IdProducto == id);

            if (producto?.Imagen != null)
            {
                return File(
                    producto.Imagen,
                    producto.ImagenMime
                );
            }

            return NotFound();
        }

        // ==========================================
        // DETALLE PÚBLICO DEL PRODUCTO
        // ==========================================

        [HttpGet]
        public IActionResult ObtenerProducto(int id)
        {
            var producto = _context.Productos
                .Include(x => x.Variantes)
                .FirstOrDefault(x =>
                    x.IdProducto == id &&
                    x.Activo &&
                    x.Disponible &&
                    x.Stock > 0 &&
                    x.MostrarEnCatalogo
                );

            if (producto == null)
                return NotFound();

            return Json(new
            {
                idProducto = producto.IdProducto,
                nombre = producto.Nombre,
                categoria = producto.Categoria,
                descripcion = producto.Descripcion,
                precioVenta = producto.PrecioVenta,

                variantes = producto.Variantes
                    .Where(x =>
                        x.Activo &&
                        x.Stock > 0
                    )
                    .Select(x => new
                    {
                        nombreVariante = x.NombreVariante
                    })
                    .ToList()
            });
        }
    }
}