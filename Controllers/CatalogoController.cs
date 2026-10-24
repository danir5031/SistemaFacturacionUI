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
                    x.Stock > 0
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
    }
}