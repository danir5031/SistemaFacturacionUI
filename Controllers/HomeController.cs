using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        public IActionResult Index()
        {
            var usuario = HttpContext.Session.GetString("usuario");
            var rol = HttpContext.Session.GetString("rol");

            //////////////////////////////////////////////////////
            // LOGIN
            //////////////////////////////////////////////////////

            if (usuario == null)
            {
                return RedirectToAction("Index", "Login");
            }

            //////////////////////////////////////////////////////
            // SOLO ADMIN
            //////////////////////////////////////////////////////

            if (rol != "ADMIN")
            {
                return RedirectToAction("Index", "Empleado");
            }

            //////////////////////////////////////////////////////
            // FACTURAS VALIDAS
            //////////////////////////////////////////////////////

            var facturas = _context.Facturas
                .Where(x => x.Estado != "Cancelada")
                .ToList();

            //////////////////////////////////////////////////////
            // VENTAS
            //////////////////////////////////////////////////////

            decimal ventasTotales =
                facturas.Sum(x => x.Total);

            //////////////////////////////////////////////////////
            // GASTOS ENVIO
            //////////////////////////////////////////////////////

            decimal gastosEnvio =
                facturas.Sum(x => x.Envio);

            //////////////////////////////////////////////////////
            // COSTOS PRODUCTOS
            //////////////////////////////////////////////////////

            decimal costoProductos = 0;

            var detalles = _context.FacturaDetalle.ToList();

            foreach (var d in detalles)
            {
                var producto = _context.Productos
                    .FirstOrDefault(x =>
                        x.IdProducto == d.IdProducto);

                if (producto != null)
                {
                    costoProductos +=
                        producto.Costo * d.Cantidad;
                }
            }

            //////////////////////////////////////////////////////
            // GANANCIA
            //////////////////////////////////////////////////////

            decimal gananciaNeta =
                ventasTotales
                - gastosEnvio
                - costoProductos;

            //////////////////////////////////////////////////////
            // CLIENTES
            //////////////////////////////////////////////////////

            int clientes =
                _context.Clientes
                .Count(x => x.Activo == true);

            //////////////////////////////////////////////////////
            // PRODUCTOS
            //////////////////////////////////////////////////////

            int productos =
                _context.Productos
                .Count(x => x.Activo == true);

            //////////////////////////////////////////////////////
            // FACTURAS HOY
            //////////////////////////////////////////////////////

            int facturasHoy =
                _context.Facturas
                .Count(x =>
                    x.FechaRegistro.Date == DateTime.Today
                    &&
                    x.Estado != "Cancelada");

            //////////////////////////////////////////////////////
            // PENDIENTES
            //////////////////////////////////////////////////////

            int pendientes =
                _context.Facturas
                .Count(x =>
                    x.Estado == "Pendiente");

            //////////////////////////////////////////////////////
            // STOCK BAJO
            //////////////////////////////////////////////////////

            int productosBajos =
                _context.Productos
                .Count(x =>
                    x.Stock <= 3
                    &&
                    x.Activo == true);

            //////////////////////////////////////////////////////
            // USUARIOS
            //////////////////////////////////////////////////////

            int usuarios =
                _context.Usuarios
                .Count(x => x.Activo == true);

            //////////////////////////////////////////////////////
            // VIEWBAG
            //////////////////////////////////////////////////////

            ViewBag.VentasTotales = ventasTotales;
            ViewBag.GastosEnvio = gastosEnvio;
            ViewBag.CostoProductos = costoProductos;
            ViewBag.GananciaNeta = gananciaNeta;

            ViewBag.Clientes = clientes;
            ViewBag.Productos = productos;

            ViewBag.FacturasHoy = facturasHoy;
            ViewBag.Pendientes = pendientes;
            ViewBag.ProductosBajos = productosBajos;
            ViewBag.Usuarios = usuarios;

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

            ViewBag.VentasSemana = ultimos7Dias
                .Select(fecha =>
                    facturas
                    .Where(x => x.FechaRegistro.Date == fecha.Date)
                    .Sum(x => x.Total))
                .ToList();

            ViewBag.GananciasSemana = ultimos7Dias
    .Select(fecha =>
    {
        //////////////////////////////////////////////////////
        // FACTURAS DEL DIA
        //////////////////////////////////////////////////////

        var ventasDia = facturas
            .Where(x => x.FechaRegistro.Date == fecha.Date)
            .ToList();

        //////////////////////////////////////////////////////
        // TOTAL VENTAS
        //////////////////////////////////////////////////////

        decimal venta = ventasDia.Sum(x => x.Total);

        //////////////////////////////////////////////////////
        // TOTAL ENVIO
        //////////////////////////////////////////////////////

        decimal envio = ventasDia.Sum(x => x.Envio);

        //////////////////////////////////////////////////////
        // COSTO PRODUCTOS
        //////////////////////////////////////////////////////

        decimal costoProductosDia = 0;

        foreach (var factura in ventasDia)
        {
            var detallesFactura = _context.FacturaDetalle
                .Where(x => x.IdFactura == factura.IdFactura)
                .ToList();

            foreach (var d in detallesFactura)
            {
                var producto = _context.Productos
                    .FirstOrDefault(x =>
                        x.IdProducto == d.IdProducto);

                if (producto != null)
                {
                    costoProductosDia +=
                        producto.Costo * d.Cantidad;
                }
            }
        }

        //////////////////////////////////////////////////////
        // GANANCIA REAL
        //////////////////////////////////////////////////////

        decimal gananciaReal =
            venta
            - envio
            - costoProductosDia;

        return gananciaReal;
    })
    .ToList();

            ViewBag.User = usuario;

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