using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        public IActionResult Index(

    int pagina = 1,

    string buscar = "",

    string categoria = "",

    string stock = "",

    string estado = "",

    DateTime? fecha = null
)
        {
            //////////////////////////////////////////////////////
            // LOGIN
            //////////////////////////////////////////////////////

            if (HttpContext.Session.GetString("usuario") == null)
                return RedirectToAction("Index", "Login");

            //////////////////////////////////////////////////////
            // PAGINACION
            //////////////////////////////////////////////////////

            int cantidadPorPagina = 10;

            //////////////////////////////////////////////////////
            // QUERY
            //////////////////////////////////////////////////////

            var query = _context.Productos

    .Include(x => x.Variantes)

    .Where(x => x.Activo)

    .AsQueryable();

            //////////////////////////////////////////////////////
            // BUSCADOR
            //////////////////////////////////////////////////////

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                buscar = buscar.ToLower();

                query = query.Where(x =>

                    x.Nombre.ToLower().Contains(buscar)

                    ||

                    x.Categoria.ToLower().Contains(buscar)
                );
            }

            //////////////////////////////////////////////////////
            // CATEGORIA
            //////////////////////////////////////////////////////

            if (!string.IsNullOrWhiteSpace(categoria))
            {
                query = query.Where(x =>
                    x.Categoria == categoria);
            }

            //////////////////////////////////////////////////////
            // ESTADO
            //////////////////////////////////////////////////////

            if (!string.IsNullOrWhiteSpace(estado))
            {
                bool disponible = estado == "true";

                query = query.Where(x =>
                    x.Disponible == disponible);
            }

            //////////////////////////////////////////////////////
            // STOCK
            //////////////////////////////////////////////////////

            if (!string.IsNullOrWhiteSpace(stock))
            {
                if (stock == "bajo")
                {
                    query = query.Where(x => x.Stock < 5);
                }

                if (stock == "medio")
                {
                    query = query.Where(x =>
                        x.Stock >= 5 &&
                        x.Stock <= 20);
                }

                if (stock == "alto")
                {
                    query = query.Where(x =>
                        x.Stock > 20);
                }
            }

            //////////////////////////////////////////////////////
            // FECHA
            //////////////////////////////////////////////////////

            if (fecha.HasValue)
            {
                var fechaFiltro = fecha.Value.Date;

                query = query.Where(x =>
                    x.FechaRegistro.Date == fechaFiltro);
            }

            //////////////////////////////////////////////////////
            // ORDER
            //////////////////////////////////////////////////////

            query = query
                .OrderByDescending(x => x.FechaRegistro);

            //////////////////////////////////////////////////////
            // TOTAL
            //////////////////////////////////////////////////////

            int totalProductos = query.Count();

            int totalPaginas = (int)Math.Ceiling(
                (double)totalProductos / cantidadPorPagina
            );

            //////////////////////////////////////////////////////
            // PAGINACION
            //////////////////////////////////////////////////////

            var productos = query

                .Skip((pagina - 1) * cantidadPorPagina)

                .Take(cantidadPorPagina)

                .ToList();

            //////////////////////////////////////////////////////
            // VIEWBAG
            //////////////////////////////////////////////////////

            ViewBag.PaginaActual = pagina;
            ViewBag.TotalPaginas = totalPaginas;

            ViewBag.Buscar = buscar;
            ViewBag.Categoria = categoria;
            ViewBag.Stock = stock;
            ViewBag.Estado = estado;
            ViewBag.Fecha = fecha?.ToString("yyyy-MM-dd");

            ViewBag.Categorias = _context.Productos
    .Where(x => x.Activo)
    .Select(x => x.Categoria)
    .Distinct()
    .OrderBy(x => x)
    .ToList();
            //////////////////////////////////////////////////////
            // RETORNO
            //////////////////////////////////////////////////////

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

                    //////////////////////////////////////////////////////
                    // LIMPIAR URL
                    //////////////////////////////////////////////////////

                    producto.ImagenUrl = null;
                }
            }

            if (producto.IdProducto == 0)
            {
                producto.FechaRegistro = DateTime.Now;
                producto.Activo = true;

                if (producto.Variantes != null
     && producto.Variantes.Any())
                {
                    //////////////////////////////////////////////////////
                    // STOCK TOTAL
                    //////////////////////////////////////////////////////

                    producto.Stock =
                        producto.Variantes.Sum(x => x.Stock);

                    //////////////////////////////////////////////////////
                    // CONFIG VARIANTES
                    //////////////////////////////////////////////////////

                    foreach (var v in producto.Variantes)
                    {
                        v.Activo = true;

                        v.FechaRegistro = DateTime.Now;
                    }
                }

                //////////////////////////////////////////////////////
                // GUARDAR PRODUCTO
                //////////////////////////////////////////////////////

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

                    db.ImagenUrl = producto.ImagenUrl;

                    //////////////////////////////////////////////////////
                    // VARIANTES
                    //////////////////////////////////////////////////////

                    var variantesDb = _context.ProductoVariantes
                        .Where(x => x.IdProducto == db.IdProducto)
                        .ToList();

                    //////////////////////////////////////////////////////
                    // DESACTIVAR ANTERIORES
                    //////////////////////////////////////////////////////

                    foreach (var v in variantesDb)
                    {
                        v.Activo = false;
                    }

                    //////////////////////////////////////////////////////
                    // NUEVAS VARIANTES
                    //////////////////////////////////////////////////////

                    if (producto.Variantes != null &&
                        producto.Variantes.Any())
                    {
                        foreach (var v in producto.Variantes)
                        {
                            //////////////////////////////////////////////////////
                            // BUSCAR EXISTE
                            //////////////////////////////////////////////////////

                            var existeVariante = variantesDb
                                .FirstOrDefault(x =>
                                    x.IdVariante == v.IdVariante);

                            //////////////////////////////////////////////////////
                            // EDITAR
                            //////////////////////////////////////////////////////

                            if (existeVariante != null)
                            {
                                existeVariante.NombreVariante =
                                    v.NombreVariante;

                                existeVariante.Stock =
                                    v.Stock;

                                existeVariante.Activo = true;
                            }
                            else
                            {
                                //////////////////////////////////////////////////////
                                // NUEVA
                                //////////////////////////////////////////////////////

                                _context.ProductoVariantes.Add(
                                    new ProductoVariante
                                    {
                                        IdProducto = db.IdProducto,

                                        NombreVariante = v.NombreVariante,

                                        Stock = v.Stock,

                                        Activo = true,

                                        FechaRegistro = DateTime.Now
                                    });
                            }
                        }

                        //////////////////////////////////////////////////////
                        // STOCK TOTAL AUTOMÁTICO
                        //////////////////////////////////////////////////////

                        db.Stock = producto.Variantes.Sum(x => x.Stock);
                    }
                    else
                    {
                        //////////////////////////////////////////////////////
                        // PRODUCTO NORMAL
                        //////////////////////////////////////////////////////

                        db.Stock = producto.Stock;
                    }

                    //////////////////////////////////////////////////////
                    // DISPONIBLE
                    //////////////////////////////////////////////////////

                    db.Disponible = db.Stock > 0;

                    if (producto.Imagen != null)
                    {
                        db.Imagen = producto.Imagen;

                        db.ImagenMime = producto.ImagenMime;

                        //////////////////////////////////////////////////////
                        // LIMPIAR URL
                        //////////////////////////////////////////////////////

                        db.ImagenUrl = null;
                    }
                }
            }

            _context.SaveChanges();
            return Json(true);
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

        [HttpGet]
        public JsonResult ObtenerProducto(int id)
        {
            var producto = _context.Productos
                .Include(x => x.Variantes)
                .FirstOrDefault(x => x.IdProducto == id);

            if (producto == null)
                return Json(null);

            return Json(new
            {
                producto.IdProducto,
                producto.Nombre,
                producto.Categoria,
                producto.Descripcion,
                producto.PrecioVenta,
                producto.Costo,
                producto.Stock,
                ImagenUrl = producto.Imagen != null
    ? "/Producto/VerImagen/" + producto.IdProducto
    : producto.ImagenUrl,
                producto.Disponible,

                Variantes = producto.Variantes
                    .Where(x => x.Activo)
                    .Select(x => new
                    {
                        x.IdVariante,
                        x.NombreVariante,
                        x.Stock
                    })
            });
        }
    }
}