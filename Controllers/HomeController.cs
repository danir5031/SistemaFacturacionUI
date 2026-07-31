using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SistemaFacturacionUI.Models;
using System;
using System.Diagnostics;
using System.Linq;

namespace SistemaFacturacionUI.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;

        public HomeController(
            ILogger<HomeController> logger,
            AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public  IActionResult Index(DateTime? fechaInicio, DateTime? fechaFin)
        {
            var usuario = HttpContext.Session.GetString("usuario");
            var rol = HttpContext.Session.GetString("rol");

            if (usuario == null)
                return RedirectToAction("Index", "Login");

            if (rol != "ADMIN")
                return RedirectToAction("Index", "Empleado");

            DateTime inicio = fechaInicio ?? DateTime.Today;
            DateTime fin = (fechaFin ?? DateTime.Today)
                            .Date
                            .AddDays(1)
                            .AddSeconds(-1);

            bool filtroActivo =
                fechaInicio.HasValue &&
                fechaFin.HasValue;

            //////////////////////////////////////////////////////
            // CARGAR TODO UNA SOLA VEZ
            //////////////////////////////////////////////////////

            var facturas = _context.Facturas
                .AsNoTracking()
                .Where(x => x.Estado != "Cancelada")
                .ToList();

            var facturasFiltro = filtroActivo
                ? facturas
                    .Where(x =>
                        x.FechaRegistro >= inicio &&
                        x.FechaRegistro <= fin)
                    .ToList()
                : facturas
                    .Where(x => x.FechaRegistro.Date == DateTime.Today)
                    .ToList();

            var detalles = _context.FacturaDetalle
                .AsNoTracking()
                .ToList();

            var productos = _context.Productos
                .AsNoTracking()
                .ToDictionary(x => x.IdProducto);

            var clientes = _context.Clientes
                .AsNoTracking()
                .ToList();

            var usuarios = _context.Usuarios
                .AsNoTracking()
                .ToList();

            //////////////////////////////////////////////////////
            // DICCIONARIO FACTURA -> DETALLES
            //////////////////////////////////////////////////////

            var detallesPorFactura = detalles
                .GroupBy(x => x.IdFactura)
                .ToDictionary(
                    g => g.Key,
                    g => g.ToList());

            //////////////////////////////////////////////////////
            // DICCIONARIO PRODUCTO -> NOMBRE
            //////////////////////////////////////////////////////

            var nombresProductos = productos.Values
                .ToDictionary(
                    x => x.IdProducto,
                    x => x.Nombre);

            //////////////////////////////////////////////////////
            // DICCIONARIO USUARIO
            //////////////////////////////////////////////////////

            var usuariosPorId = usuarios
                .ToDictionary(
                    x => x.IdUsuario,
                    x => x.Usuario1);

            //////////////////////////////////////////////////////
            // COSTO PRODUCTOS
            //////////////////////////////////////////////////////

            decimal costoProductos = 0;

            foreach (var factura in facturasFiltro)
            {
                if (!detallesPorFactura.ContainsKey(factura.IdFactura))
                    continue;

                foreach (var d in detallesPorFactura[factura.IdFactura])
                {
                    if (productos.TryGetValue(d.IdProducto, out var producto))
                    {
                        costoProductos +=
                            producto.Costo * d.Cantidad;
                    }
                }
            }

            //////////////////////////////////////////////////////
            // RESUMEN GENERAL
            //////////////////////////////////////////////////////

            decimal ventasTotales =
                facturasFiltro.Sum(x => x.Total);

            decimal gastosEnvio =
                facturasFiltro.Sum(x => x.Envio);

            decimal gananciaNeta =
                ventasTotales
                - gastosEnvio
                - costoProductos;

            int clientesActivos =
                clientes.Count(x => x.Activo);

            int productosActivos =
                productos.Values.Count(x => x.Activo);

            int usuariosActivos =
                usuarios.Count(x => x.Activo);

            int pendientes =
                facturas.Count(x => x.Estado == "Pendiente");

            int productosBajos =
                productos.Values.Count(x =>
                    x.Activo &&
                    x.Stock <= 3);

            int facturasPeriodo =
                facturasFiltro.Count();

            decimal ventasHoyMonto =
                facturasFiltro.Sum(x => x.Total);

            decimal enviosHoyMonto =
                facturasFiltro.Sum(x => x.Envio);

            int cantidadVentasHoy =
                facturasFiltro.Count();

            var hoy = DateTime.Today;

            //////////////////////////////////////////////////////
            // FACTURAS DEL PERIODO
            //////////////////////////////////////////////////////

            var facturasPeriodoLista = filtroActivo
                ? facturasFiltro
                : facturas
                    .Where(x => x.FechaRegistro.Date == hoy)
                    .ToList();

            //////////////////////////////////////////////////////
            // HASHSET FACTURAS
            //////////////////////////////////////////////////////

            var facturasPeriodoIds =

                facturasPeriodoLista

                    .Select(x => x.IdFactura)

                    .ToHashSet();


            cantidadVentasHoy = facturasFiltro.Count();
            //////////////////////////////////////////////////////
            // VENTAS POR USUARIO
            //////////////////////////////////////////////////////

            var ventasUsuariosHoy = facturasPeriodoLista

                .Where(x => x.IdUsuario != null)

                .GroupBy(x => usuariosPorId.ContainsKey(x.IdUsuario)
                                ? usuariosPorId[x.IdUsuario]
                                : "SIN USUARIO")

                .Select(g => new
                {
                    Usuario = g.Key,
                    Ventas = g.Count(),
                    Total = g.Sum(x => x.Total)
                })

                .OrderByDescending(x => x.Ventas)

                .ToList();

            //////////////////////////////////////////////////////
            // VENTAS TOTALES POR USUARIO
            //////////////////////////////////////////////////////

            var ventasUsuariosTotal = facturasFiltro

                .Where(x => x.IdUsuario != null)

                .GroupBy(x => usuariosPorId.ContainsKey(x.IdUsuario)
                                ? usuariosPorId[x.IdUsuario]
                                : "SIN USUARIO")

                .Select(g => new
                {
                    Usuario = g.Key,
                    Ventas = g.Count(),
                    Total = g.Sum(x => x.Total)
                })

                .OrderByDescending(x => x.Total)

                .ToList();

            ViewBag.VentasUsuariosHoy = ventasUsuariosHoy;
            ViewBag.VentasUsuariosTotal = ventasUsuariosTotal;

            //////////////////////////////////////////////////////
            // PRODUCTOS MAS VENDIDOS
            //////////////////////////////////////////////////////

            var productosMasVendidos =

                detalles

.Where(d =>

    facturasPeriodoIds

        .Contains(d.IdFactura))

                .GroupBy(d => d.IdProducto)

                .Select(g => new
                {
                    IdProducto = g.Key,

                    CantidadVendida = g.Sum(x => x.Cantidad),

                    NombreProducto =
                        nombresProductos.ContainsKey(g.Key)
                            ? nombresProductos[g.Key]
                            : "SIN NOMBRE"
                })

                .OrderByDescending(x => x.CantidadVendida)

                .Take(10)

                .ToList();

            ViewBag.ProductosMasVendidos = productosMasVendidos;

            ViewBag.PastelProductosLabels =
                productosMasVendidos
                    .Select(x => x.NombreProducto)
                    .ToList();

            ViewBag.PastelProductosValores =
                productosMasVendidos
                    .Select(x => x.CantidadVendida)
                    .ToList();

            //////////////////////////////////////////////////////
            // PASTEL TIENDAS
            //////////////////////////////////////////////////////

            var ventasTiendas =

                facturasPeriodoLista

                .Where(x =>
                    !string.IsNullOrWhiteSpace(x.Tienda))

                .GroupBy(x => x.Tienda.Trim().ToUpper())

                .Select(g => new
                {
                    Tienda = g.First().Tienda.Trim(),
                    Total = g.Sum(x => x.Total)
                })

                .OrderByDescending(x => x.Total)

                .ToList();

            ViewBag.PastelTiendasLabels =
                ventasTiendas
                    .Select(x => x.Tienda)
                    .ToList();

            ViewBag.PastelTiendasValores =
                ventasTiendas
                    .Select(x => x.Total)
                    .ToList();

            //////////////////////////////////////////////////////
            // PASTEL USUARIOS
            //////////////////////////////////////////////////////

            var ventasPastelUsuarios =

                facturasPeriodoLista

                .Where(x => x.IdUsuario != null)

                .GroupBy(x => usuariosPorId.ContainsKey(x.IdUsuario)
                                ? usuariosPorId[x.IdUsuario]
                                : "SIN USUARIO")

                .Select(g => new
                {
                    Usuario = g.Key,
                    Total = g.Sum(x => x.Total)
                })

                .OrderByDescending(x => x.Total)

                .ToList();

            ViewBag.PastelUsuariosLabels =
                ventasPastelUsuarios
                    .Select(x => x.Usuario)
                    .ToList();

            ViewBag.PastelUsuariosValores =
                ventasPastelUsuarios
                    .Select(x => x.Total)
                    .ToList();
            //////////////////////////////////////////////////////
            // VIEWBAG
            //////////////////////////////////////////////////////

            ViewBag.VentasTotales = ventasTotales;
            ViewBag.GastosEnvio = gastosEnvio;
            ViewBag.CostoProductos = costoProductos;
            ViewBag.GananciaNeta = gananciaNeta;

            ViewBag.Clientes = clientesActivos;
            ViewBag.Productos = productosActivos;

            ViewBag.FacturasHoy = facturasPeriodo;
            ViewBag.Pendientes = pendientes;
            ViewBag.ProductosBajos = productosBajos;
            ViewBag.Usuarios = usuariosActivos;

            //////////////////////////////////////////////////////
            // NUEVOS VIEWBAG
            //////////////////////////////////////////////////////

            ViewBag.VentasHoyMonto = ventasHoyMonto;

            ViewBag.EnviosHoyMonto = enviosHoyMonto;

            ViewBag.CantidadVentasHoy = cantidadVentasHoy;

            //////////////////////////////////////////////////////
            // COSTO POR FACTURA (PRECALCULADO)
            //////////////////////////////////////////////////////

            var costoFactura = detalles

                .GroupBy(d => d.IdFactura)

                .ToDictionary(

                    g => g.Key,

                    g => g.Sum(d =>

                        productos.TryGetValue(d.IdProducto, out var p)

                            ? p.Costo * d.Cantidad

                            : 0

                    )

                );

            //////////////////////////////////////////////////////
            // GRAFICA
            //////////////////////////////////////////////////////

            var ultimos7Dias = Enumerable.Range(0, 7)
                .Select(i => DateTime.Today.AddDays(-i))
                .OrderBy(x => x)
                .ToList();

            ViewBag.Labels = ultimos7Dias
                .Select(x => x.ToString("dd/MM"))
                .ToList();

            var ventasPorFecha =

    facturas

    .GroupBy(x => x.FechaRegistro.Date)

    .ToDictionary(

        g => g.Key,

        g => g.Sum(x => x.Total)

    );

            ViewBag.VentasSemana =

    ultimos7Dias

        .Select(fecha =>

            ventasPorFecha.ContainsKey(fecha.Date)

                ? ventasPorFecha[fecha.Date]

                : 0

        )

        .ToList();

            ViewBag.GananciasSemana =

     ultimos7Dias

     .Select(fecha =>
     {
         var ventasDia = facturas

             .Where(x => x.FechaRegistro.Date == fecha.Date)

             .ToList();

         decimal venta =
             ventasDia.Sum(x => x.Total);

         decimal envio =
             ventasDia.Sum(x => x.Envio);

         decimal costo =

             ventasDia.Sum(f =>

                 costoFactura.ContainsKey(f.IdFactura)

                     ? costoFactura[f.IdFactura]

                     : 0

             );

         return venta - envio - costo;

     })

     .ToList();

            ViewBag.User = usuario;
            ViewBag.FechaInicio =
    inicio.ToString("yyyy-MM-dd");

            DateTime fechaFinVista =
    fechaFin ?? DateTime.Today;

            ViewBag.FechaFin =
    fechaFinVista.ToString("yyyy-MM-dd");

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]

        public IActionResult Error()
        {
            return View(
                new ErrorViewModel
                {
                    RequestId =
                    Activity.Current?.Id ??
                    HttpContext.TraceIdentifier
                }
            );
        }
    }
}