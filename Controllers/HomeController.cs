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
        public IActionResult Index(DateTime? fechaInicio,
    DateTime? fechaFin)
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
            // FILTRO FECHAS
            //////////////////////////////////////////////////////

            DateTime inicio =
                fechaInicio ?? DateTime.Today;

            DateTime fin =
                fechaFin ?? DateTime.Today;


            fin = fin.Date
                .AddDays(1)
                .AddSeconds(-1);


            //////////////////////////////////////////////////////
            // FILTRO ACTIVO
            //////////////////////////////////////////////////////

            bool filtroActivo =
                fechaInicio.HasValue &&
                fechaFin.HasValue;

            //////////////////////////////////////////////////////
            // FACTURAS FILTRADAS
            //////////////////////////////////////////////////////

            var facturasFiltro = _context.Facturas

                .Where(x =>
                    x.Estado != "Cancelada")

                .ToList();

            if (filtroActivo)
            {
                facturasFiltro =
                    facturasFiltro

                    .Where(x =>
                        x.FechaRegistro >= inicio
                        &&
                        x.FechaRegistro <= fin)

                    .ToList();
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
    facturasFiltro.Sum(x => x.Total);

            //////////////////////////////////////////////////////
            // GASTOS ENVIO
            //////////////////////////////////////////////////////

            decimal gastosEnvio =
    facturasFiltro.Sum(x => x.Envio);

            //////////////////////////////////////////////////////
            // COSTOS PRODUCTOS
            //////////////////////////////////////////////////////

            decimal costoProductos = 0;

            var detalles = _context.FacturaDetalle.ToList();

            foreach (var factura in facturasFiltro)
            {
                var detallesFactura =
                    _context.FacturaDetalle

                    .Where(x =>
                        x.IdFactura == factura.IdFactura)

                    .ToList();

                foreach (var d in detallesFactura)
                {
                    var producto =
                        _context.Productos

                        .FirstOrDefault(x =>
                            x.IdProducto == d.IdProducto);

                    if (producto != null)
                    {
                        costoProductos +=
                            producto.Costo *
                            d.Cantidad;
                    }
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

            int facturasPeriodo;

            if (filtroActivo)
            {
                facturasPeriodo =
                    facturasFiltro.Count();
            }
            else
            {
                facturasPeriodo =
                    _context.Facturas

                    .Count(x =>
                        x.Estado != "Cancelada"
                        &&
                        x.FechaRegistro.Date == DateTime.Today);
            }

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
            // HOY
            //////////////////////////////////////////////////////

            var hoy = DateTime.Today;

            //////////////////////////////////////////////////////
            // VENTAS HOY $
            //////////////////////////////////////////////////////

            decimal ventasHoyMonto =
    _context.Facturas
    .Where(x =>
        x.Estado != "Cancelada"
        &&
        x.FechaRegistro >= inicio
        &&
        x.FechaRegistro <= fin)
    .Sum(x => (decimal?)x.Total) ?? 0;

            //////////////////////////////////////////////////////
            // ENVIOS HOY $
            //////////////////////////////////////////////////////

            decimal enviosHoyMonto =
    _context.Facturas
    .Where(x =>
        x.Estado != "Cancelada"
        &&
        x.FechaRegistro >= inicio
        &&
        x.FechaRegistro <= fin)
    .Sum(x => (decimal?)x.Envio) ?? 0;

            //////////////////////////////////////////////////////
            // CANTIDAD VENTAS HOY
            //////////////////////////////////////////////////////

            int cantidadVentasHoy;

            if (filtroActivo)
            {
                cantidadVentasHoy =
                    facturasFiltro.Count();
            }
            else
            {
                cantidadVentasHoy =
                    _context.Facturas

                    .Count(x =>
                        x.Estado != "Cancelada"
                        &&
                        x.FechaRegistro.Date == DateTime.Today);
            }

            //////////////////////////////////////////////////////
            // VENTAS EMPLEADOS
            //////////////////////////////////////////////////////

            //////////////////////////////////////////////////////
            // VENTAS EMPLEADOS HOY
            //////////////////////////////////////////////////////

            var ventasUsuariosHoy = _context.Facturas

    .Where(x =>
        x.Estado != "Cancelada"
        &&
        x.FechaRegistro >= inicio
        &&
        x.FechaRegistro <= fin
        &&
        x.Usuario != null)

    .GroupBy(x => x.Usuario.Usuario1)

    .Select(g => new
    {
        Usuario = g.Key,

        Ventas = g.Count(),

        Total = g.Sum(x => x.Total)
    })

    .OrderByDescending(x => x.Ventas)

    .ToList();

            //////////////////////////////////////////////////////
            // VENTAS TOTALES EMPLEADOS
            //////////////////////////////////////////////////////

            var ventasUsuariosTotal = _context.Facturas

                .Where(x =>
    x.Estado != "Cancelada"
    &&
    x.Usuario != null
    &&
    (
        !filtroActivo

        ||

        (x.FechaRegistro >= inicio
         &&
         x.FechaRegistro <= fin)
    )
)

                .GroupBy(x => x.Usuario.Usuario1)

                .Select(g => new
                {
                    Usuario = g.Key,

                    Ventas = g.Count(),

                    Total = g.Sum(x => x.Total)
                })

                .OrderByDescending(x => x.Total)

                .ToList();

            //////////////////////////////////////////////////////
            // PRODUCTOS MAS VENDIDOS
            //////////////////////////////////////////////////////

            //////////////////////////////////////////////////////
            // PRODUCTOS MAS VENDIDOS
            //////////////////////////////////////////////////////

      

            var productosMasVendidos =

                (from d in _context.FacturaDetalle

                 join f in _context.Facturas
                 on d.IdFactura equals f.IdFactura

                 where f.Estado != "Cancelada"

                 &&

                 (
                     filtroActivo

                     ?

                     (f.FechaRegistro >= inicio
                      &&
                      f.FechaRegistro <= fin)

                     :

                     f.FechaRegistro.Date == hoy
                 )

                 group d by d.IdProducto into g

                 select new
                 {
                     IdProducto = g.Key,

                     CantidadVendida =
                         g.Sum(x => x.Cantidad),

                     NombreProducto =
                         _context.Productos
                         .Where(p => p.IdProducto == g.Key)
                         .Select(p => p.Nombre)
                         .FirstOrDefault()
                 })

                 .OrderByDescending(x => x.CantidadVendida)

                 .Take(10)

                 .ToList();

            ViewBag.ProductosMasVendidos =
                productosMasVendidos;

            ViewBag.PastelProductosLabels =
    productosMasVendidos
    .Select(x => x.NombreProducto)
    .ToList();

            ViewBag.PastelProductosValores =
                productosMasVendidos
                .Select(x => x.CantidadVendida)
                .ToList();

            ViewBag.VentasUsuariosTotal = ventasUsuariosTotal;

            ViewBag.VentasUsuariosHoy = ventasUsuariosHoy;



            //////////////////////////////////////////////////////
            // PASTEL VENTAS POR TIENDA
            //////////////////////////////////////////////////////

            var ventasTiendas =

            _context.Facturas

            .Where(f =>

                f.Estado != "Cancelada"

                &&

                !string.IsNullOrWhiteSpace(f.Tienda)

                &&

                (

                    filtroActivo

                    ?

                    (f.FechaRegistro >= inicio &&
                     f.FechaRegistro <= fin)

                    :

                    f.FechaRegistro.Date == hoy

                )

            )

            .AsEnumerable()

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
            // GRAFICO PASTEL USUARIOS
            //////////////////////////////////////////////////////

            var ventasPastelUsuarios =

            _context.Facturas

            .Where(x =>

                x.Estado != "Cancelada"

                &&

                x.Usuario != null

                &&

                (

                    filtroActivo

                    ?

                    (x.FechaRegistro >= inicio &&
                     x.FechaRegistro <= fin)

                    :

                    x.FechaRegistro.Date == hoy

                )

            )

            .GroupBy(x => x.Usuario.Usuario1)

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

            ViewBag.Clientes = clientes;
            ViewBag.Productos = productos;

            ViewBag.FacturasHoy = facturasPeriodo;
            ViewBag.Pendientes = pendientes;
            ViewBag.ProductosBajos = productosBajos;
            ViewBag.Usuarios = usuarios;

            //////////////////////////////////////////////////////
            // NUEVOS VIEWBAG
            //////////////////////////////////////////////////////

            ViewBag.VentasHoyMonto = ventasHoyMonto;

            ViewBag.EnviosHoyMonto = enviosHoyMonto;

            ViewBag.CantidadVentasHoy = cantidadVentasHoy;

            

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
            ViewBag.FechaInicio =
    inicio.ToString("yyyy-MM-dd");

            DateTime fechaFinVista =
    fechaFin ?? DateTime.Today;

            ViewBag.FechaFin =
                fin.ToString("yyyy-MM-dd");

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