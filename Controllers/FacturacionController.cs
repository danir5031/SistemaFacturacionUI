using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SistemaFacturacionUI.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SistemaFacturacionUI.Controllers
{
    public class FacturacionController : Controller
    {
        private readonly AppDbContext _context;

        public FacturacionController(AppDbContext context)
        {
            _context = context;
        }

        //////////////////////////////////////////////////////
        // INDEX
        //////////////////////////////////////////////////////
        public IActionResult Index(
    string buscar,
    DateTime? fechaRegistro,
    DateTime? fechaEnvio)
        {
            var rol = HttpContext.Session.GetString("rol");

            if (rol != "ADMIN" && rol != "EMPLEADO")
                return RedirectToAction("Index", "Login");

            //////////////////////////////////////////////////////
            // QUERY
            //////////////////////////////////////////////////////

            var query = _context.Facturas
                .Include(x => x.Usuario)
                .AsQueryable();

            //////////////////////////////////////////////////////
            // SI NO HAY FILTROS
            // MOSTRAR SOLO HOY
            //////////////////////////////////////////////////////

            //////////////////////////////////////////////////////
            // MOSTRAR SOLO REGISTROS DE HOY
            // PERO SI EXISTE FILTRO DE ENVIO
            // MOSTRAR ESA FECHA AUNQUE SEA ANTIGUA
            //////////////////////////////////////////////////////

            if (!fechaRegistro.HasValue &&
    !fechaEnvio.HasValue &&
    string.IsNullOrWhiteSpace(buscar))
            {
                var hoy = DateTime.Today;

                query = query.Where(x =>
                    x.FechaRegistro.Date == hoy);
            }
            else
            {
                //////////////////////////////////////////////////////
                // FILTRO FECHA REGISTRO
                //////////////////////////////////////////////////////

                if (fechaRegistro.HasValue)
                {
                    var fecha = fechaRegistro.Value.Date;

                    query = query.Where(x =>
                        x.FechaRegistro.Date == fecha);
                }

                //////////////////////////////////////////////////////
                // FILTRO FECHA ENVIO
                //////////////////////////////////////////////////////

                if (fechaEnvio.HasValue)
                {
                    var fecha = fechaEnvio.Value.Date;

                    query = query.Where(x =>
                        x.FechaEnvio.HasValue &&
                        x.FechaEnvio.Value.Date == fecha);
                }
            }

            //////////////////////////////////////////////////////
            // FILTRO FECHA REGISTRO
            //////////////////////////////////////////////////////

            if (fechaRegistro.HasValue)
            {
                var fecha = fechaRegistro.Value.Date;

                query = query.Where(x =>
                    x.FechaRegistro.Date == fecha);
            }

            //////////////////////////////////////////////////////
            // FILTRO FECHA ENVIO
            //////////////////////////////////////////////////////

            if (fechaEnvio.HasValue)
            {
                var fecha = fechaEnvio.Value.Date;

                query = query.Where(x =>
                    x.FechaEnvio.HasValue &&
                    x.FechaEnvio.Value.Date == fecha);
            }

            //////////////////////////////////////////////////////
            // BUSCADOR
            //////////////////////////////////////////////////////

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                buscar = buscar.ToLower();

                query = query.Where(x =>

                    (x.NombreCliente != null &&
                     x.NombreCliente.ToLower().Contains(buscar))

                    ||

                    (x.Telefono != null &&
                     x.Telefono.Contains(buscar))
                );
            }

            //////////////////////////////////////////////////////
            // RESULTADO
            //////////////////////////////////////////////////////

            var facturas = query
                .OrderByDescending(x => x.FechaRegistro)
                .Take(300)
                .ToList();

            return View(facturas);
        }

        //////////////////////////////////////////////////////
        // BUSCAR PRODUCTOS
        //////////////////////////////////////////////////////
        [HttpGet]
        public JsonResult ObtenerProductos(string filtro)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filtro))
                    return Json(new List<object>());

                var productos = _context.Productos
                    .Where(x =>
                        x.Activo &&
                        x.Disponible &&
                        x.Stock > 0 &&
                        (
                            x.Nombre.Contains(filtro)
                        )
                    )
                    .Select(x => new
                    {
                        idProducto = x.IdProducto,
                        nombre = x.Nombre,
                        precioVenta = x.PrecioVenta,
                        imagenUrl = x.ImagenUrl,
                        stock = x.Stock
                    })
                    .Take(10)
                    .ToList();

                return Json(productos);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    error = ex.Message
                });
            }
        }

        //////////////////////////////////////////////////////
        // GUARDAR FACTURA
        //////////////////////////////////////////////////////

        //////////////////////////////////////////////////////
        // GUARDAR FACTURA
        //////////////////////////////////////////////////////

        [HttpPost]
        public JsonResult GuardarFactura([FromBody] FacturaDTO data)
        {
            try
            {
                //////////////////////////////////////////////////////
                // VALIDAR
                //////////////////////////////////////////////////////

                if (data == null)
                    return Json("Datos inválidos");

                if (data.Detalles == null || data.Detalles.Count == 0)
                    return Json("Debe agregar productos");

                //////////////////////////////////////////////////////
                // USUARIO
                //////////////////////////////////////////////////////

                var usuario = HttpContext.Session.GetString("usuario");

                if (string.IsNullOrEmpty(usuario))
                    return Json("Sesión expirada");

                var userDb = _context.Usuarios
                    .FirstOrDefault(x => x.Usuario1 == usuario);

                if (userDb == null)
                    return Json("Usuario no encontrado");

                //////////////////////////////////////////////////////
                // CLIENTE
                //////////////////////////////////////////////////////

                Cliente cliente = null;

                if (!string.IsNullOrEmpty(data.Telefono))
                {
                    cliente = _context.Clientes
                        .FirstOrDefault(x =>
                            x.Telefono == data.Telefono &&
                            x.Activo == true);
                }

                //////////////////////////////////////////////////////
                // CREAR CLIENTE SI NO EXISTE
                //////////////////////////////////////////////////////

                if (cliente == null)
                {
                    cliente = new Cliente
                    {
                        Nombre = string.IsNullOrWhiteSpace(data.NombreCliente)
                            ? "Cliente General"
                            : data.NombreCliente,

                        Telefono = data.Telefono ?? "",

                        Direccion = data.Direccion ?? "",

                        FechaRegistro = DateTime.Now,

                        Activo = true
                    };

                    _context.Clientes.Add(cliente);

                    _context.SaveChanges();
                }

                //////////////////////////////////////////////////////
                // FACTURA
                //////////////////////////////////////////////////////

                var factura = new Factura
                {
                    NumeroFactura =
                        "FAC-" + DateTime.Now.Ticks,

                    FechaRegistro =
                        DateTime.Now,

                    FechaEnvio =
                        data.FechaEnvio,

                    IdUsuario =
                        userDb.IdUsuario,

                    IdCliente =
                        cliente.Idcliente,

                    NombreCliente =
                        cliente.Nombre,

                    Telefono =
                        data.Telefono ?? "",

                    Direccion =
                        data.Direccion ?? "",

                    Departamento =
                        data.Departamento ?? "",

                    Municipio =
                        data.Municipio ?? "",

                    Comentario =
                        data.Comentario ?? "",

                    Horario =
                        data.Horario ?? "TM",

                    HoraExacta =
                        data.HoraExacta ?? "",

                    MetodoPago =
                        string.IsNullOrEmpty(data.MetodoPago)
                        ? "Efectivo"
                        : data.MetodoPago,

                    Estado =
                        "Completada",

                    Subtotal =
                        data.Subtotal,

                    Envio =
                        data.Envio,

                    Descuento =
                        data.Descuento,

                    Total =
                        data.Total,

                    TicketGenerado = false
                };

                _context.Facturas.Add(factura);

                _context.SaveChanges();

                //////////////////////////////////////////////////////
                // DETALLES
                //////////////////////////////////////////////////////

                foreach (var d in data.Detalles)
                {
                    var producto = _context.Productos
                        .FirstOrDefault(x =>
                            x.IdProducto == d.IdProducto);

                    if (producto == null)
                        continue;

                    //////////////////////////////////////////////////////
                    // VALIDAR STOCK
                    //////////////////////////////////////////////////////

                    if (producto.Stock < d.Cantidad)
                    {
                        return Json(
                            $"Stock insuficiente para {producto.Nombre}"
                        );
                    }

                    //////////////////////////////////////////////////////
                    // DETALLE
                    //////////////////////////////////////////////////////

                    var detalle = new FacturaDetalle
                    {
                        IdFactura = factura.IdFactura,

                        IdProducto = d.IdProducto,

                        Cantidad = d.Cantidad,

                        Precio = d.Precio
                    };

                    _context.FacturaDetalle.Add(detalle);

                    //////////////////////////////////////////////////////
                    // DESCONTAR STOCK
                    //////////////////////////////////////////////////////

                    producto.Stock -= d.Cantidad;

                    if (producto.Stock <= 0)
                    {
                        producto.Stock = 0;
                        producto.Disponible = false;
                    }
                }

                //////////////////////////////////////////////////////
                // GUARDAR
                //////////////////////////////////////////////////////

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



        [HttpPost]
        public JsonResult ActualizarFactura([FromBody] FacturaDTO data)
        {
            try
            {
                //////////////////////////////////////////////////////
                // VALIDAR
                //////////////////////////////////////////////////////

                var factura = _context.Facturas
                    .Include(x => x.Detalles)
                    .FirstOrDefault(x => x.IdFactura == data.IdFactura);

                if (factura == null)
                    return Json("Factura no encontrada");

                if (factura.Estado == "Cancelada")
                    return Json("No se puede editar una factura cancelada");

                //////////////////////////////////////////////////////
                // DEVOLVER STOCK ANTERIOR
                //////////////////////////////////////////////////////

                foreach (var d in factura.Detalles)
                {
                    var producto = _context.Productos
                        .FirstOrDefault(x => x.IdProducto == d.IdProducto);

                    if (producto != null)
                    {
                        producto.Stock += d.Cantidad;

                        if (producto.Stock > 0)
                        {
                            producto.Disponible = true;
                        }
                    }
                }

                //////////////////////////////////////////////////////
                // ELIMINAR DETALLES ANTERIORES
                //////////////////////////////////////////////////////

                _context.FacturaDetalle.RemoveRange(factura.Detalles);

                //////////////////////////////////////////////////////
                // ACTUALIZAR DATOS
                //////////////////////////////////////////////////////

                factura.Telefono = data.Telefono ?? "";

                factura.Direccion = data.Direccion ?? "";

                factura.Departamento = data.Departamento ?? "";

                factura.Municipio = data.Municipio ?? "";

                factura.Comentario = data.Comentario ?? "";

                factura.Horario = data.Horario ?? "TM";

                factura.HoraExacta = data.HoraExacta ?? "";

                factura.FechaEnvio = data.FechaEnvio;

                factura.Envio = data.Envio;

                factura.Descuento = data.Descuento;

                factura.Subtotal = data.Subtotal;

                factura.Total = data.Total;

                factura.MetodoPago = data.MetodoPago ?? "Efectivo";

                //////////////////////////////////////////////////////
                // NUEVOS DETALLES
                //////////////////////////////////////////////////////

                foreach (var d in data.Detalles)
                {
                    var producto = _context.Productos
                        .FirstOrDefault(x => x.IdProducto == d.IdProducto);

                    if (producto == null)
                        continue;

                    //////////////////////////////////////////////////////
                    // VALIDAR STOCK
                    //////////////////////////////////////////////////////

                    if (producto.Stock < d.Cantidad)
                    {
                        return Json($"Stock insuficiente para {producto.Nombre}");
                    }

                    //////////////////////////////////////////////////////
                    // AGREGAR DETALLE
                    //////////////////////////////////////////////////////

                    var detalle = new FacturaDetalle
                    {
                        IdFactura = factura.IdFactura,
                        IdProducto = d.IdProducto,
                        Cantidad = d.Cantidad,
                        Precio = d.Precio
                    };

                    _context.FacturaDetalle.Add(detalle);

                    //////////////////////////////////////////////////////
                    // DESCONTAR STOCK
                    //////////////////////////////////////////////////////

                    producto.Stock -= d.Cantidad;

                    if (producto.Stock <= 0)
                    {
                        producto.Stock = 0;
                        producto.Disponible = false;
                    }
                }

                //////////////////////////////////////////////////////
                // GUARDAR
                //////////////////////////////////////////////////////

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
        // CANCELAR FACTURA
        //////////////////////////////////////////////////////

        [HttpPost]
        public JsonResult CancelarFactura(int id)
        {
            try
            {
                var factura = _context.Facturas
                    .FirstOrDefault(x => x.IdFactura == id);

                if (factura == null)
                    return Json("Factura no encontrada");

                //////////////////////////////////////////////////////
                // VALIDAR
                //////////////////////////////////////////////////////

                if (factura.Estado == "Cancelada")
                    return Json("La factura ya está cancelada");

                //////////////////////////////////////////////////////
                // OBTENER DETALLES
                //////////////////////////////////////////////////////

                var detalles = _context.FacturaDetalle
                    .Where(x => x.IdFactura == id)
                    .ToList();

                //////////////////////////////////////////////////////
                // DEVOLVER STOCK
                //////////////////////////////////////////////////////

                foreach (var d in detalles)
                {
                    var producto = _context.Productos
                        .FirstOrDefault(x => x.IdProducto == d.IdProducto);

                    if (producto != null)
                    {
                        producto.Stock += d.Cantidad;

                        //////////////////////////////////////////////////////
                        // REACTIVAR
                        //////////////////////////////////////////////////////

                        if (producto.Stock > 0)
                        {
                            producto.Disponible = true;
                        }
                    }
                }

                //////////////////////////////////////////////////////
                // CANCELAR FACTURA
                //////////////////////////////////////////////////////

                factura.Estado = "Cancelada";

                _context.SaveChanges();

                return Json(true);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        //////////////////////////////////////////////////////
        // OBTENER FACTURA
        //////////////////////////////////////////////////////

        [HttpGet]
        public JsonResult ObtenerFactura(int id)
        {
            try
            {
                var factura = _context.Facturas
                    .Include(x => x.Detalles)
                    .FirstOrDefault(x => x.IdFactura == id);

                if (factura == null)
                    return Json(null);

                //////////////////////////////////////////////////////
                // VALIDAR CANCELADA
                //////////////////////////////////////////////////////

                if (factura.Estado == "Cancelada")
                {
                    return Json("No se puede editar");
                }

                //////////////////////////////////////////////////////
                // PRODUCTOS
                //////////////////////////////////////////////////////

                var detalles = factura.Detalles.Select(d => {

                    var producto = _context.Productos
                        .FirstOrDefault(p => p.IdProducto == d.IdProducto);

                    return new
                    {
                        idProducto = d.IdProducto,
                        nombre = producto != null ? producto.Nombre : "",
                        cantidad = d.Cantidad,
                        precio = d.Precio,
                        imagen = producto != null ? producto.ImagenUrl : "",
                        stock = producto != null ? producto.Stock : 0
                    };

                }).ToList();

                //////////////////////////////////////////////////////
                // RETORNO
                //////////////////////////////////////////////////////

                return Json(new
                {
                    factura.IdFactura,
                    factura.NombreCliente,
                    factura.Telefono,
                    factura.Direccion,
                    factura.Departamento,
                    factura.Municipio,
                    factura.Comentario,
                    factura.Horario,
                    factura.HoraExacta,
                    factura.FechaEnvio,
                    factura.Envio,
                    factura.Descuento,
                    factura.MetodoPago,
                    detalles
                });
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }


        //////////////////////////////////////////////////////
        // TICKET
        //////////////////////////////////////////////////////

        [HttpGet]
        public IActionResult Ticket(int id)
        {
            var factura = _context.Facturas
                .Include(x => x.Detalles)
                .FirstOrDefault(x => x.IdFactura == id);

            if (factura == null)
                return NotFound();

            //////////////////////////////////////////////////////
            // PRODUCTOS
            //////////////////////////////////////////////////////

            foreach (var d in factura.Detalles)
            {
                d.Producto = _context.Productos
                    .FirstOrDefault(x => x.IdProducto == d.IdProducto);
            }

            return View(factura);
        }

        //////////////////////////////////////////////////////
        // EXPORTAR EXCEL
        //////////////////////////////////////////////////////

        [HttpGet]
        public IActionResult ExportarExcel(string ids)
        {
            var listaIds = ids
                .Split(',')
                .Select(int.Parse)
                .ToList();

            var facturas = _context.Facturas
                .Where(x => listaIds.Contains(x.IdFactura))
                .ToList();

            using var workbook = new XLWorkbook();

            var ws = workbook.Worksheets.Add("Envios");

            //////////////////////////////////////////////////////
            // HEADERS
            //////////////////////////////////////////////////////

            ws.Cell(1, 1).Value = "Nombre";
            ws.Cell(1, 2).Value = "Telefono";
            ws.Cell(1, 3).Value = "Email";
            ws.Cell(1, 4).Value = "Direccion";
            ws.Cell(1, 5).Value = "Ciudad";
            ws.Cell(1, 6).Value = "Region";
            ws.Cell(1, 7).Value = "Pais";
            ws.Cell(1, 8).Value = "Descripcion";
            ws.Cell(1, 9).Value = "Peso";
            ws.Cell(1, 10).Value = "Valor Declarado";
            ws.Cell(1, 11).Value = "Observaciones";

            //////////////////////////////////////////////////////
            // DATA
            //////////////////////////////////////////////////////

            int fila = 2;

            foreach (var f in facturas)
            {
                ws.Cell(fila, 1).Value = f.NombreCliente;
                ws.Cell(fila, 2).Value = f.Telefono;
                ws.Cell(fila, 3).Value = "";
                ws.Cell(fila, 4).Value = f.Direccion;
                ws.Cell(fila, 5).Value = f.Municipio;
                ws.Cell(fila, 6).Value = f.Departamento;
                ws.Cell(fila, 7).Value = "El Salvador";
                ws.Cell(fila, 8).Value = "JOYERIA";
                ws.Cell(fila, 9).Value = 1;

                //////////////////////////////////////////////////////
                // COD
                //////////////////////////////////////////////////////

                ws.Cell(fila, 10).Value =
                    f.MetodoPago == "Efectivo"
                    ? f.Total
                    : 0;

                ws.Cell(fila, 11).Value =
                    "Llamar 30 min antes de la entrega";

                //////////////////////////////////////////////////////
                // MARCAR GENERADO
                //////////////////////////////////////////////////////

                f.TicketGenerado = true;

                fila++;
            }

            _context.SaveChanges();

            //////////////////////////////////////////////////////
            // AJUSTAR
            //////////////////////////////////////////////////////

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();

            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "CargaMasiva.xlsx"
            );
        }

        //////////////////////////////////////////////////////
        // GENERAR TICKETS MASIVOS
        //////////////////////////////////////////////////////

        [HttpGet]
        public IActionResult GenerarEtiquetas(string ids)
        {
            var listaIds = ids
                .Split(',')
                .Select(int.Parse)
                .ToList();

            var facturas = _context.Facturas
                .Include(x => x.Detalles)
                .ToList();

            facturas = facturas
                .Where(x => listaIds.Contains(x.IdFactura))
                .ToList();

            //////////////////////////////////////////////////////
            // PRODUCTOS
            //////////////////////////////////////////////////////

            foreach (var factura in facturas)
            {
                foreach (var d in factura.Detalles)
                {
                    d.Producto = _context.Productos
                        .FirstOrDefault(x =>
                            x.IdProducto == d.IdProducto);
                }

                //////////////////////////////////////////////////////
                // MARCAR GENERADO
                //////////////////////////////////////////////////////

                factura.TicketGenerado = true;
            }

            _context.SaveChanges();

            return View("TicketsMasivos", facturas);
        }






    }

        //////////////////////////////////////////////////////
        // DTO FACTURA
        //////////////////////////////////////////////////////

        public class FacturaDTO
    {
        public int IdFactura { get; set; }
        public string NombreCliente { get; set; }

        public string Telefono { get; set; }

        public string Direccion { get; set; }

        public string Departamento { get; set; }

        public string Municipio { get; set; }

        public string Comentario { get; set; }

        public string Horario { get; set; }

        public string HoraExacta { get; set; }

        public DateTime? FechaEnvio { get; set; }

        public decimal Subtotal { get; set; }

        public decimal Envio { get; set; }

        public decimal Descuento { get; set; }

        public decimal Total { get; set; }

        public string MetodoPago { get; set; }

        public string Estado { get; set; }

        public List<DetalleDTO> Detalles { get; set; }
    }

    //////////////////////////////////////////////////////
    // DTO DETALLE
    //////////////////////////////////////////////////////

    public class DetalleDTO
    {
        public int IdProducto { get; set; }

        public int Cantidad { get; set; }

        public decimal Precio { get; set; }
    }


}