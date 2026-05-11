using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SistemaFacturacionUI.Models;
using System;
using System.Linq;
using System.Collections.Generic;

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
        public IActionResult Index()
        {
            var rol = HttpContext.Session.GetString("rol");

            if (rol != "ADMIN" && rol != "EMPLEADO")
                return RedirectToAction("Index", "Login");

            var facturas = _context.Facturas
                .OrderByDescending(x => x.FechaRegistro)
                .Take(50)
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
        [HttpPost]
        public JsonResult GuardarFactura([FromBody] FacturaDTO data)
        {
            try
            {
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
                // BUSCAR CLIENTE
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
                // SI NO EXISTE -> CREAR
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
                // CREAR FACTURA
                //////////////////////////////////////////////////////

                var factura = new Factura
                {
                    NumeroFactura = "FAC-" + DateTime.Now.Ticks,

                    FechaRegistro = DateTime.Now,

                    FechaEnvio = data.FechaEnvio,

                    IdUsuario = userDb.IdUsuario,

                    IdCliente = cliente.Idcliente,

                    NombreCliente = cliente.Nombre,

                    Telefono = data.Telefono ?? "",

                    Direccion = data.Direccion ?? "",

                    Departamento = data.Departamento ?? "",

                    Municipio = data.Municipio ?? "",

                    Horario = data.Horario ?? "TM",

                    HoraExacta = data.HoraExacta ?? "",

                    MetodoPago = string.IsNullOrEmpty(data.MetodoPago)
                        ? "Efectivo"
                        : data.MetodoPago,

                    Estado = "Completada",

                    Comentario = "",

                    Subtotal = data.Subtotal,

                    Envio = data.Envio,

                    Descuento = data.Descuento,

                    Total = data.Total
                };

                _context.Facturas.Add(factura);

                _context.SaveChanges();

                //////////////////////////////////////////////////////
                // DETALLES
                //////////////////////////////////////////////////////

                foreach (var d in data.Detalles)
                {
                    var producto = _context.Productos
                        .FirstOrDefault(x => x.IdProducto == d.IdProducto);

                    if (producto == null)
                        continue;

                    if (producto.Stock < d.Cantidad)
                    {
                        return Json($"Stock insuficiente para {producto.Nombre}");
                    }

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

        //////////////////////////////////////////////////////
        // DTO FACTURA
        //////////////////////////////////////////////////////

        public class FacturaDTO
    {
        public string NombreCliente { get; set; }

        public string Telefono { get; set; }

        public string Direccion { get; set; }

        public string Departamento { get; set; }

        public string Municipio { get; set; }

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