using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        private void RegistrarHistorial(
    int idFactura,
    int idUsuario,
    string tipoAccion,
    string descripcion,
    string usuarioActual,
    bool permitirSuperAdmin = false)
        {
            // El superadmin no genera historial de creación ni edición
            if (usuarioActual == "admin" && !permitirSuperAdmin)
                return;

            _context.FacturaHistorial.Add(new FacturaHistorial
            {
                IdFactura = idFactura,
                IdUsuario = idUsuario,
                Fecha = DateTime.Now,
                TipoAccion = tipoAccion,
                Descripcion = descripcion
            });
        }

        [HttpGet]
        public JsonResult ObtenerHistorialFactura(int id)
        {
            var historial = _context.FacturaHistorial
                .Where(x => x.IdFactura == id)
                .Include(x => x.Usuario)
                .OrderByDescending(x => x.Fecha)
                .Select(x => new
                {
                    idHistorial = x.IdHistorial,
                    fecha = x.Fecha.ToString("dd/MM/yyyy HH:mm"),
                    usuario = x.Usuario.Usuario1,
                    tipoAccion = x.TipoAccion,
                    descripcion = x.Descripcion
                })
                .ToList();

            return Json(historial);
        }

        //////////////////////////////////////////////////////
        // INDEX
        //////////////////////////////////////////////////////
        public IActionResult Index(
    string buscar,
    DateTime? fechaInicioRegistro,
    DateTime? fechaFinRegistro,
    DateTime? fechaRegistro,
    DateTime? fechaEnvio,
    int? idEmpresa,
    int? idTienda,
    bool mostrarTodas = false,
    int pagina = 1)
        {
            // Solo los administradores pueden filtrar por empresa y tienda
            var rol = HttpContext.Session.GetString("rol");

            if (rol != "ADMIN")
            {
                idEmpresa = null;
                idTienda = null;
            }

            if (rol != "ADMIN" && rol != "EMPLEADO")
                return RedirectToAction("Index", "Login");

            //////////////////////////////////////////////////////
            // QUERY
            //////////////////////////////////////////////////////

            var query = _context.Facturas
    .Include(x => x.Usuario)
    .Include(x => x.Detalles)
        .ThenInclude(d => d.Producto)
    .AsQueryable();

            // FILTRO POR EMPRESA
            // FILTRO POR EMPRESA
            if (idEmpresa.HasValue)
            {
                query = query.Where(x =>
                    (
                        x.IdTienda.HasValue &&
                        _context.Tiendas.Any(t =>
                            t.IdTienda == x.IdTienda.Value &&
                            t.IdEmpresa == idEmpresa.Value
                        )
                    )
                    ||
                    (
                        !x.IdTienda.HasValue &&
                        _context.Tiendas.Any(t =>
                            t.IdEmpresa == idEmpresa.Value &&
                            t.Nombre == x.Tienda
                        )
                    )
                );
            }

            // FILTRO POR TIENDA
            if (idTienda.HasValue)
            {
                query = query.Where(x =>
                    x.IdTienda == idTienda.Value
                    ||
                    (
                        !x.IdTienda.HasValue &&
                        _context.Tiendas.Any(t =>
                            t.IdTienda == idTienda.Value &&
                            t.Nombre == x.Tienda
                        )
                    )
                );
            }

            //////////////////////////////////////////////////////
            // SI NO HAY FILTROS
            // MOSTRAR SOLO HOY
            //////////////////////////////////////////////////////

            //////////////////////////////////////////////////////
            // MOSTRAR SOLO REGISTROS DE HOY
            // PERO SI EXISTE FILTRO DE ENVIO
            // MOSTRAR ESA FECHA AUNQUE SEA ANTIGUA
            //////////////////////////////////////////////////////

            if (!fechaInicioRegistro.HasValue &&
    !fechaFinRegistro.HasValue &&
    !fechaEnvio.HasValue &&
    !idEmpresa.HasValue &&
    !idTienda.HasValue &&
    string.IsNullOrWhiteSpace(buscar))
            {
                var hoy = DateTime.Today;

                query = query.Where(x =>
                    x.FechaRegistro.Date == hoy);
            }


            //////////////////////////////////////////////////////
            // SOLO HOY SI NO HAY FILTROS
            //////////////////////////////////////////////////////

            if (!fechaInicioRegistro.HasValue &&
                !fechaFinRegistro.HasValue &&
                !fechaEnvio.HasValue &&
                string.IsNullOrWhiteSpace(buscar))
            {
                var hoy = DateTime.Today;

                query = query.Where(x =>
                    x.FechaRegistro.Date == hoy);
            }

            //////////////////////////////////////////////////////
            // FILTRO FECHA REGISTRO
            //////////////////////////////////////////////////////

            if (fechaInicioRegistro.HasValue &&
                fechaFinRegistro.HasValue)
            {
                var inicio =
                    fechaInicioRegistro.Value.Date;

                var fin =
                    fechaFinRegistro.Value.Date.AddDays(1);

                query = query.Where(x =>

                    x.FechaRegistro >= inicio &&

                    x.FechaRegistro < fin

                );
            }

            //////////////////////////////////////////////////////
            // FILTRO FECHA ENVIO
            //////////////////////////////////////////////////////

            if (fechaEnvio.HasValue)
            {
                var inicio =
                    fechaEnvio.Value.Date;

                var fin =
                    inicio.AddDays(1);

                query = query.Where(x =>

                    x.FechaEnvio.HasValue &&

                    x.FechaEnvio.Value >= inicio &&

                    x.FechaEnvio.Value < fin

                );
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

    ||

    (x.NumeroFactura != null &&
     x.NumeroFactura.ToLower().Contains(buscar))

    ||

    x.Detalles.Any(d =>
        d.Producto != null &&
        d.Producto.Nombre.ToLower().Contains(buscar))

);
            }

            //////////////////////////////////////////////////////
            // RESULTADO
            //////////////////////////////////////////////////////

            var facturas = query
    .OrderBy(x =>
        x.Estado == "Cancelada"
            ? 2
            : x.TicketGenerado
                ? 1
                : 0)
    .ThenByDescending(x => x.FechaRegistro)
    .Take(300)
    .ToList();

            ViewBag.TiendasActivas = _context.Tiendas
    .Include(t => t.Empresa)
    .Where(t => t.Activa && t.Empresa.Activa)
    .OrderBy(t => t.Empresa.Nombre)
    .ThenBy(t => t.Nombre)
    .ToList();


            ViewBag.EmpresasActivas = _context.Empresas
                .Where(e => e.Activa)
                .OrderBy(e => e.Nombre)
                .ToList();

            ViewBag.IdEmpresaSeleccionada = idEmpresa;
            ViewBag.IdTiendaSeleccionada = idTienda;



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

                //////////////////////////////////////////////////////
                // PRODUCTOS
                //////////////////////////////////////////////////////

                var productos = _context.Productos
                    .Include(x => x.Variantes)
                    .Where(x =>
                        x.Activo &&
                        x.Disponible &&
                        x.Stock > 0 &&
                        x.Nombre.Contains(filtro)
                    )
                    .ToList();

                //////////////////////////////////////////////////////
                // RESULTADO
                //////////////////////////////////////////////////////

                var resultado = productos.SelectMany(p =>

                    //////////////////////////////////////////////////////
                    // SI TIENE VARIANTES
                    //////////////////////////////////////////////////////

                    p.Variantes != null && p.Variantes.Any()

                    ?

                    p.Variantes
                    .Where(v => v.Activo && v.Stock > 0)
                    .Select(v => new
                    {
                        idProducto = p.IdProducto,

                        nombre =
                            p.Nombre + " - " + v.NombreVariante,

                        precioVenta = p.PrecioVenta,

                        imagenUrl = p.Imagen != null
    ? "/Producto/VerImagen/" + p.IdProducto
    : p.ImagenUrl,

                        stock = v.Stock,

                        idVariante = v.IdVariante,

                        esVariante = true
                    })

                    :

                    //////////////////////////////////////////////////////
                    // PRODUCTO NORMAL
                    //////////////////////////////////////////////////////

                    new[]
                    {
                new
                {
                    idProducto = p.IdProducto,

                    nombre = p.Nombre,

                    precioVenta = p.PrecioVenta,

                    imagenUrl = p.Imagen != null
        ? "/Producto/VerImagen/" + p.IdProducto
        : p.ImagenUrl,

                    stock = p.Stock,

                    idVariante = 0,

                    esVariante = false
                }
                    }

                )
                .Take(10)
                .ToList();

                return Json(resultado);
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
        // VERIFICAR HISTORIAL DEL CLIENTE
        //////////////////////////////////////////////////////

        [HttpGet]
        public JsonResult VerificarCliente(string telefono)
        {
            if (string.IsNullOrWhiteSpace(telefono))
                return Json(new
                {
                    riesgo = "NINGUNO"
                });

            var historial = _context.Facturas
                .Where(x => x.Telefono == telefono)
                .ToList();

            var canceladas = historial.Count(x => x.Estado == "Cancelada");
            var completadas = historial.Count(x => x.Estado != "Cancelada");

            string riesgo = "NINGUNO";
            string color = "success";
            string mensaje = "";

            if (canceladas >= 4)
            {
                riesgo = "ALTO";
                color = "danger";
                mensaje = "Este cliente posee varias facturas canceladas.";
            }
            else if (canceladas >= 2)
            {
                riesgo = "MEDIO";
                color = "warning";
                mensaje = "Este cliente tiene múltiples cancelaciones.";
            }
            else if (canceladas >= 1)
            {
                riesgo = "BAJO";
                color = "secondary";
                mensaje = "Este cliente tiene una factura cancelada.";
            }

            var total = historial.Count();

            double porcentaje = total == 0
                ? 0
                : (double)canceladas * 100 / total;

            var ultimaCancelacion = historial
                .Where(x => x.Estado == "Cancelada")
                .OrderByDescending(x => x.FechaRegistro)
                .FirstOrDefault();

            return Json(new
            {
                mostrar = canceladas > 0,

                riesgo,

                color,

                mensaje,

                canceladas,

                completadas,

                total,

                porcentaje = Math.Round(porcentaje, 1),

                ultimaCancelacion =
                    ultimaCancelacion?.FechaRegistro.ToString("dd/MM/yyyy")
            });
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
                // EVITAR FACTURA DUPLICADA
                //////////////////////////////////////////////////////

                if (!data.IdSolicitud.HasValue || data.IdSolicitud == Guid.Empty)
                    return Json("Solicitud inválida");

                var facturaExistente = _context.Facturas
                    .FirstOrDefault(x => x.IdSolicitud == data.IdSolicitud.Value);

                if (facturaExistente != null)
                {
                    // La factura ya fue creada anteriormente.
                    // Esto ocurre cuando el usuario vuelve a presionar
                    // Guardar porque la respuesta tardó en llegar.
                    return Json(true);
                }

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

                        FechaRegistro = DateTime.UtcNow.AddHours(-6),

                        Activo = true
                    };

                    _context.Clientes.Add(cliente);

                    _context.SaveChanges();
                }

                //////////////////////////////////////////////////////
                // VALIDAR FECHA DE ENVÍO
                //////////////////////////////////////////////////////

                if (data.FechaEnvio.HasValue)
                {
                    var fechaEnvio = data.FechaEnvio.Value.Date;
                    var hoy = DateTime.Now.Date;

                    if (fechaEnvio < hoy)
                    {
                        return Json("No se permiten fechas anteriores al día de hoy.");
                    }
                }

                // VALIDAR TIENDA SELECCIONADA
                var tienda = _context.Tiendas
                    .Include(t => t.Empresa)
                    .FirstOrDefault(t =>
                        t.IdTienda == data.IdTienda &&
                        t.Activa &&
                        t.Empresa.Activa);

                if (tienda == null)
                {
                    return Json("Selecciona una tienda activa de una empresa activa.");
                }

                //////////////////////////////////////////////////////
                // FACTURA
                //////////////////////////////////////////////////////

                var factura = new Factura
                {

                    IdSolicitud = data.IdSolicitud.Value,
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

                    Referencia = data.Referencia ?? "",

                    Departamento = data.Departamento ?? "",

                    Municipio = data.Municipio ?? "",

                    // TipoEnvio = data.TipoEnvio ?? "",

                    IdTienda = tienda.IdTienda,
                    Tienda = tienda.Nombre,

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
                        
                    }

                    //////////////////////////////////////////////////////
                    // DETALLE
                    //////////////////////////////////////////////////////

                    var detalle = new FacturaDetalle
                    {
                        IdFactura = factura.IdFactura,

                        IdProducto = d.IdProducto,

                        IdVariante = d.IdVariante,

                        Variante = d.Variante,

                        Cantidad = d.Cantidad,

                        Precio = d.Precio
                    };

                    _context.FacturaDetalle.Add(detalle);

                    

                    //////////////////////////////////////////////////////
                    // DESCONTAR TOTAL PRODUCTO
                    //////////////////////////////////////////////////////

                    //////////////////////////////////////////////////////
                    // DESCONTAR STOCK
                    //////////////////////////////////////////////////////

                    if (d.IdVariante.HasValue &&
                        d.IdVariante > 0)
                    {
                        var variante = _context.ProductoVariantes
                            .FirstOrDefault(x =>
                                x.IdVariante == d.IdVariante);

                        if (variante != null)
                        {
                            //////////////////////////////////////////////////////
                            // VALIDAR STOCK
                            //////////////////////////////////////////////////////

                            if (variante.Stock < d.Cantidad)
                            {
                                return Json(
                                    $"Stock insuficiente para {variante.NombreVariante}"
                                );
                            }

                            //////////////////////////////////////////////////////
                            // DESCONTAR VARIANTE
                            //////////////////////////////////////////////////////

                            variante.Stock -= d.Cantidad;

                            if (variante.Stock < 0)
                                variante.Stock = 0;

                            //////////////////////////////////////////////////////
                            // RECALCULAR STOCK TOTAL
                            //////////////////////////////////////////////////////

                            producto.Stock = _context.ProductoVariantes
    .AsEnumerable()
    .Where(x =>
        x.IdProducto == producto.IdProducto &&
        x.Activo)
    .Sum(x => x.Stock);

                            //////////////////////////////////////////////////////
                            // DISPONIBLE
                            //////////////////////////////////////////////////////

                            producto.Disponible =
                                producto.Stock > 0;
                        }
                    }
                    else
                    {
                        //////////////////////////////////////////////////////
                        // PRODUCTO NORMAL
                        //////////////////////////////////////////////////////

                        if (producto.Stock < d.Cantidad)
                        {
                            return Json(
                                $"Stock insuficiente para {producto.Nombre}"
                            );
                        }

                        producto.Stock -= d.Cantidad;

                        if (producto.Stock < 0)
                            producto.Stock = 0;

                        producto.Disponible =
                            producto.Stock > 0;
                    }

                    if (producto.Stock <= 0)
                    {
                        producto.Stock = 0;

                        producto.Disponible = false;
                    }
                }



                RegistrarHistorial(
    factura.IdFactura,
    userDb.IdUsuario,
    "CREACION",
    "Factura creada",
    usuario
);
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

                // VALIDAR TIENDA SELECCIONADA
                var tiendaSeleccionada = _context.Tiendas
                    .Include(t => t.Empresa)
                    .FirstOrDefault(t =>
                        t.IdTienda == data.IdTienda &&
                        t.Activa &&
                        t.Empresa.Activa);

                if (tiendaSeleccionada == null)
                {
                    return Json("Selecciona una tienda activa de una empresa activa.");
                }

                //////////////////////////////////////////////////////
                // USUARIO ACTUAL
                //////////////////////////////////////////////////////

                var usuario =
                    HttpContext.Session.GetString("usuario");

                var userDb =
                    _context.Usuarios
                    .FirstOrDefault(x => x.Usuario1 == usuario);

                var cambios = new List<string>();

                // Guardar valores anteriores
                var nombreAnterior = factura.NombreCliente;
                var telefonoAnterior = factura.Telefono;
                var direccionAnterior = factura.Direccion;
                var referenciaAnterior = factura.Referencia;
                var departamentoAnterior = factura.Departamento;
                var municipioAnterior = factura.Municipio;
                var tiendaAnterior = factura.Tienda;
                var tiendaAnteriorId = factura.IdTienda;
                var comentarioAnterior = factura.Comentario;
                var horarioAnterior = factura.Horario;
                var horaExactaAnterior = factura.HoraExacta;
                var fechaEnvioAnterior = factura.FechaEnvio;
                var envioAnterior = factura.Envio;
                var descuentoAnterior = factura.Descuento;
                var subtotalAnterior = factura.Subtotal;
                var totalAnterior = factura.Total;
                var metodoPagoAnterior = factura.MetodoPago;


                //////////////////////////////////////////////////////
                // DETECTAR CAMBIOS
                //////////////////////////////////////////////////////

                if (nombreAnterior != (data.NombreCliente ?? ""))
                {
                    cambios.Add($"Cliente: {nombreAnterior} → {data.NombreCliente}");
                }

                if (telefonoAnterior != (data.Telefono ?? ""))
                {
                    cambios.Add($"Teléfono: {telefonoAnterior} → {data.Telefono}");
                }

                if (direccionAnterior != (data.Direccion ?? ""))
                {
                    cambios.Add($"Dirección: {direccionAnterior} → {data.Direccion}");
                }

                if (referenciaAnterior != (data.Referencia ?? ""))
                {
                    cambios.Add($"Referencia: {referenciaAnterior} → {data.Referencia}");
                }

                if (departamentoAnterior != (data.Departamento ?? ""))
                {
                    cambios.Add($"Departamento: {departamentoAnterior} → {data.Departamento}");
                }

                if (municipioAnterior != (data.Municipio ?? ""))
                {
                    cambios.Add($"Municipio: {municipioAnterior} → {data.Municipio}");
                }

                if (tiendaAnteriorId != tiendaSeleccionada.IdTienda)
                {
                    cambios.Add(
                        $"Tienda: {tiendaAnterior ?? "Sin tienda"} → {tiendaSeleccionada.Nombre}"
                    );
                }

                if (comentarioAnterior != (data.Comentario ?? ""))
                {
                    cambios.Add($"Comentario: {comentarioAnterior} → {data.Comentario}");
                }

                if (horarioAnterior != (data.Horario ?? "TM"))
                {
                    cambios.Add($"Horario: {horarioAnterior} → {data.Horario}");
                }

                if (horaExactaAnterior != (data.HoraExacta ?? ""))
                {
                    cambios.Add($"Hora: {horaExactaAnterior} → {data.HoraExacta}");
                }

                if (fechaEnvioAnterior?.Date != data.FechaEnvio?.Date)
                {
                    cambios.Add(
                        $"Fecha envío: " +
                        $"{fechaEnvioAnterior?.ToString("dd/MM/yyyy") ?? "Sin fecha"} → " +
                        $"{data.FechaEnvio?.ToString("dd/MM/yyyy") ?? "Sin fecha"}"
                    );
                }

                if (envioAnterior != data.Envio)
                {
                    cambios.Add($"Envío: ${envioAnterior:0.00} → ${data.Envio:0.00}");
                }

                if (descuentoAnterior != data.Descuento)
                {
                    cambios.Add($"Descuento: ${descuentoAnterior:0.00} → ${data.Descuento:0.00}");
                }

                if (subtotalAnterior != data.Subtotal)
                {
                    cambios.Add($"Subtotal: ${subtotalAnterior:0.00} → ${data.Subtotal:0.00}");
                }

                if (totalAnterior != data.Total)
                {
                    cambios.Add($"Total: ${totalAnterior:0.00} → ${data.Total:0.00}");
                }

                if (metodoPagoAnterior != (data.MetodoPago ?? "Efectivo"))
                {
                    cambios.Add(
                        $"Pago: {metodoPagoAnterior} → {data.MetodoPago ?? "Efectivo"}"
                    );
                }

                //////////////////////////////////////////////////////
                // DETECTAR CAMBIOS EN PRODUCTOS
                //////////////////////////////////////////////////////

                var detallesAnteriores = factura.Detalles
                    .Select(d => new
                    {
                        d.IdProducto,
                        d.IdVariante,
                        d.Variante,
                        d.Cantidad
                    })
                    .ToList();

                var detallesNuevos = data.Detalles
                    .Select(d => new
                    {
                        d.IdProducto,
                        d.IdVariante,
                        d.Variante,
                        d.Cantidad
                    })
                    .ToList();


                // PRODUCTOS AGREGADOS
                foreach (var nuevo in detallesNuevos)
                {
                    var anterior = detallesAnteriores.FirstOrDefault(x =>
                        x.IdProducto == nuevo.IdProducto &&
                        x.IdVariante == nuevo.IdVariante
                    );

                    if (anterior == null)
                    {
                        var producto = _context.Productos
                            .FirstOrDefault(x => x.IdProducto == nuevo.IdProducto);

                        string nombreProducto = producto?.Nombre ?? "Producto";

                        if (!string.IsNullOrWhiteSpace(nuevo.Variante))
                        {
                            nombreProducto += $" ({nuevo.Variante})";
                        }

                        cambios.Add(
                            $"Agregado: {nombreProducto} x{nuevo.Cantidad}"
                        );
                    }
                }


                // PRODUCTOS ELIMINADOS
                foreach (var anterior in detallesAnteriores)
                {
                    var nuevo = detallesNuevos.FirstOrDefault(x =>
                        x.IdProducto == anterior.IdProducto &&
                        x.IdVariante == anterior.IdVariante
                    );

                    if (nuevo == null)
                    {
                        var producto = _context.Productos
                            .FirstOrDefault(x => x.IdProducto == anterior.IdProducto);

                        string nombreProducto = producto?.Nombre ?? "Producto";

                        if (!string.IsNullOrWhiteSpace(anterior.Variante))
                        {
                            nombreProducto += $" ({anterior.Variante})";
                        }

                        cambios.Add(
                            $"Eliminado: {nombreProducto} x{anterior.Cantidad}"
                        );
                    }
                }


                // CANTIDADES MODIFICADAS
                foreach (var nuevo in detallesNuevos)
                {
                    var anterior = detallesAnteriores.FirstOrDefault(x =>
                        x.IdProducto == nuevo.IdProducto &&
                        x.IdVariante == nuevo.IdVariante
                    );

                    if (anterior != null &&
                        anterior.Cantidad != nuevo.Cantidad)
                    {
                        var producto = _context.Productos
                            .FirstOrDefault(x => x.IdProducto == nuevo.IdProducto);

                        string nombreProducto = producto?.Nombre ?? "Producto";

                        if (!string.IsNullOrWhiteSpace(nuevo.Variante))
                        {
                            nombreProducto += $" ({nuevo.Variante})";
                        }

                        cambios.Add(
                            $"Cantidad: {nombreProducto} {anterior.Cantidad} → {nuevo.Cantidad}"
                        );
                    }
                }


                // VARIANTES MODIFICADAS
                foreach (var nuevo in detallesNuevos)
                {
                    var anterior = detallesAnteriores.FirstOrDefault(x =>
                        x.IdProducto == nuevo.IdProducto
                    );

                    if (anterior != null &&
                        anterior.IdVariante != nuevo.IdVariante)
                    {
                        var producto = _context.Productos
                            .FirstOrDefault(x => x.IdProducto == nuevo.IdProducto);

                        string nombreProducto = producto?.Nombre ?? "Producto";

                        string varianteAnterior = string.IsNullOrWhiteSpace(anterior.Variante)
                            ? "Sin variante"
                            : anterior.Variante;

                        string varianteNueva = string.IsNullOrWhiteSpace(nuevo.Variante)
                            ? "Sin variante"
                            : nuevo.Variante;

                        cambios.Add(
                            $"Variante: {nombreProducto} {varianteAnterior} → {varianteNueva}"
                        );
                    }
                }

                //////////////////////////////////////////////////////
                // DEVOLVER STOCK ANTERIOR
                //////////////////////////////////////////////////////

                foreach (var d in factura.Detalles)
                {
                    var producto = _context.Productos
                        .FirstOrDefault(x => x.IdProducto == d.IdProducto);

                    //////////////////////////////////////////////////////
                    // SI ES VARIANTE
                    //////////////////////////////////////////////////////

                    if (d.IdVariante.HasValue &&
                        d.IdVariante > 0)
                    {
                        var variante = _context.ProductoVariantes
                            .FirstOrDefault(x =>
                                x.IdVariante == d.IdVariante);

                        if (variante != null)
                        {
                            //////////////////////////////////////////////////////
                            // DEVOLVER STOCK VARIANTE
                            //////////////////////////////////////////////////////

                            variante.Stock += d.Cantidad;

                            //////////////////////////////////////////////////////
                            // RECALCULAR STOCK TOTAL
                            //////////////////////////////////////////////////////

                            if (producto != null)
                            {
                                producto.Stock = _context.ProductoVariantes
    .AsEnumerable()
    .Where(x =>
        x.IdProducto == producto.IdProducto &&
        x.Activo)
    .Sum(x => x.Stock);

                                producto.Disponible =
                                    producto.Stock > 0;
                            }
                        }
                    }
                    else
                    {
                        //////////////////////////////////////////////////////
                        // PRODUCTO NORMAL
                        //////////////////////////////////////////////////////

                        if (producto != null)
                        {
                            if (d.IdVariante.HasValue &&
    d.IdVariante > 0)
                            {
                                var variante = _context.ProductoVariantes
                                    .FirstOrDefault(x =>
                                        x.IdVariante == d.IdVariante);

                                if (variante != null)
                                {
                                    variante.Stock += d.Cantidad;
                                }

                                //////////////////////////////////////////////////////
                                // RECALCULAR STOCK TOTAL
                                //////////////////////////////////////////////////////

                                producto.Stock = _context.ProductoVariantes
    .AsEnumerable()
    .Where(x =>
        x.IdProducto == producto.IdProducto &&
        x.Activo)
    .Sum(x => x.Stock);
                            }
                            else
                            {
                                producto.Stock += d.Cantidad;
                            }

                            producto.Disponible =
                                producto.Stock > 0;
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

                factura.NombreCliente = data.NombreCliente ?? "";

                factura.Telefono = data.Telefono ?? "";

                factura.Direccion = data.Direccion ?? "";


                factura.Referencia = data.Referencia ?? "";

                //factura.TipoEnvio = data.TipoEnvio ?? "";

                factura.Departamento = data.Departamento ?? "";

                factura.Municipio = data.Municipio ?? "";

                // factura.Tienda = data.Tienda ?? "";
                factura.IdTienda = tiendaSeleccionada.IdTienda;
                factura.Tienda = tiendaSeleccionada.Nombre;

                factura.Comentario = data.Comentario ?? "";

                factura.Horario = data.Horario ?? "TM";

                factura.HoraExacta = data.HoraExacta ?? "";

                if (data.FechaEnvio.HasValue)
                {
                    var fechaEnvio = data.FechaEnvio.Value.Date;
                    var hoy = DateTime.Now.Date;

                    if (fechaEnvio < hoy)
                    {
                        return Json("No se permiten fechas anteriores al día de hoy.");
                    }
                }

                factura.FechaEnvio = data.FechaEnvio;

                factura.Envio = data.Envio;

                factura.Descuento = data.Descuento;

                factura.Subtotal = data.Subtotal;

                factura.Total = data.Total;

                factura.MetodoPago = data.MetodoPago ?? "Efectivo";
                factura.TicketGenerado = false;

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

                        IdVariante = d.IdVariante,

                        Variante = d.Variante,

                        Cantidad = d.Cantidad,

                        Precio = d.Precio
                    };

                    _context.FacturaDetalle.Add(detalle);

                    //////////////////////////////////////////////////////
                    // DESCONTAR STOCK
                    //////////////////////////////////////////////////////

                    //////////////////////////////////////////////////////
                    // DESCONTAR STOCK
                    //////////////////////////////////////////////////////

                    if (d.IdVariante.HasValue &&
                        d.IdVariante > 0)
                    {
                        var variante = _context.ProductoVariantes
                            .FirstOrDefault(x =>
                                x.IdVariante == d.IdVariante);

                        if (variante != null)
                        {
                            //////////////////////////////////////////////////////
                            // VALIDAR STOCK
                            //////////////////////////////////////////////////////

                            if (variante.Stock < d.Cantidad)
                            {
                                return Json(
                                    $"Stock insuficiente para {variante.NombreVariante}"
                                );
                            }

                            //////////////////////////////////////////////////////
                            // DESCONTAR VARIANTE
                            //////////////////////////////////////////////////////

                            variante.Stock -= d.Cantidad;

                            if (variante.Stock < 0)
                                variante.Stock = 0;

                            //////////////////////////////////////////////////////
                            // RECALCULAR STOCK TOTAL
                            //////////////////////////////////////////////////////

                            producto.Stock = _context.ProductoVariantes
    .AsEnumerable()
    .Where(x =>
        x.IdProducto == producto.IdProducto &&
        x.Activo)
    .Sum(x => x.Stock);

                            producto.Disponible =
                                producto.Stock > 0;
                        }
                    }
                    else
                    {
                        //////////////////////////////////////////////////////
                        // PRODUCTO NORMAL
                        //////////////////////////////////////////////////////

                        producto.Stock -= d.Cantidad;

                        if (producto.Stock < 0)
                            producto.Stock = 0;

                        producto.Disponible =
                            producto.Stock > 0;
                    }
                }

                //////////////////////////////////////////////////////
                // GUARDAR HISTORIAL
                //////////////////////////////////////////////////////
                if (cambios.Count > 0)
                {
                    var descripcionCambios = string.Join(
                        "\n",
                        cambios.Select(x => "• " + x)
                    );

                    RegistrarHistorial(
    factura.IdFactura,
    userDb.IdUsuario,
    "EDICION",
    descripcionCambios,
    usuario
);
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
        public JsonResult CancelarFactura(int id, string motivo)
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
                if (string.IsNullOrWhiteSpace(motivo))
                    return Json("Debes indicar el motivo de cancelación.");

                var usuario = HttpContext.Session.GetString("usuario");

                var userDb = _context.Usuarios
                    .FirstOrDefault(x => x.Usuario1 == usuario);

                if (userDb == null)
                    return Json("Usuario no encontrado");

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

                    //////////////////////////////////////////////////////
                    // DEVOLVER VARIANTE
                    //////////////////////////////////////////////////////

                    if (d.IdVariante.HasValue)
                    {
                        var variante = _context.ProductoVariantes
                            .FirstOrDefault(x =>
                                x.IdVariante == d.IdVariante);

                        if (variante != null)
                        {
                            variante.Stock += d.Cantidad;
                        }
                    }

                    if (producto != null)
                    {
                        if (d.IdVariante.HasValue &&
                            d.IdVariante > 0)
                        {
                            producto.Stock = _context.ProductoVariantes
                                .AsEnumerable()
                                .Where(x =>
                                    x.IdProducto == producto.IdProducto &&
                                    x.Activo)
                                .Sum(x => x.Stock);
                        }
                        else
                        {
                            producto.Stock += d.Cantidad;
                        }

                        producto.Disponible =
                            producto.Stock > 0;
                    }
                }

                //////////////////////////////////////////////////////
                // CANCELAR FACTURA
                //////////////////////////////////////////////////////

                factura.Estado = "Cancelada";

                RegistrarHistorial(
    factura.IdFactura,
    userDb.IdUsuario,
    "CANCELACION",
    motivo.Trim(),
    usuario,
    true
);

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
                    .Include(x => x.Usuario)
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

                        idVariante = d.IdVariante,

                        variante = d.Variante,

                        nombre = producto != null
    ? (
        !string.IsNullOrEmpty(d.Variante)
        ? producto.Nombre + " - " + d.Variante
        : producto.Nombre
      )
    : "",

                        cantidad = d.Cantidad,

                        precio = d.Precio,

                        imagen = producto != null
                            ? producto.ImagenUrl
                            : "",

                        stock =
    d.IdVariante.HasValue && d.IdVariante > 0

    ?

    _context.ProductoVariantes
        .Where(x => x.IdVariante == d.IdVariante)
        .Select(x => x.Stock)
        .FirstOrDefault()

    :

    (producto != null
        ? producto.Stock
        : 0)
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

                    factura.Referencia,

                    factura.Departamento,
                    factura.Municipio,

                    //factura.TipoEnvio,

                    factura.IdTienda,
                    factura.Tienda,

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
        // ELIMINAR FACTURA DEFINITIVA
        //////////////////////////////////////////////////////

        [HttpPost]
        public JsonResult EliminarFactura(int id)
        {
            try
            {
                //////////////////////////////////////////////////////
                // SOLO ADMIN PRINCIPAL
                //////////////////////////////////////////////////////

                var usuario =
                    HttpContext.Session.GetString("usuario");

                if (usuario != "admin")
                {
                    return Json("No autorizado");
                }



                //////////////////////////////////////////////////////
                // FACTURA
                //////////////////////////////////////////////////////

                var factura = _context.Facturas
                    .Include(x => x.Detalles)
                    .FirstOrDefault(x => x.IdFactura == id);

                if (factura == null)
                {
                    return Json("Factura no encontrada");
                }

                //////////////////////////////////////////////////////
                // DEVOLVER STOCK
                //////////////////////////////////////////////////////

                foreach (var d in factura.Detalles)
                {
                    var producto = _context.Productos
                        .FirstOrDefault(x =>
                            x.IdProducto == d.IdProducto);

                    if (producto != null)
                    {
                        if (d.IdVariante.HasValue &&
    d.IdVariante > 0)
                        {
                            var variante = _context.ProductoVariantes
                                .FirstOrDefault(x =>
                                    x.IdVariante == d.IdVariante);

                            if (variante != null)
                            {
                                variante.Stock += d.Cantidad;
                            }

                            producto.Stock = _context.ProductoVariantes
                                .AsEnumerable()
                                .Where(x =>
                                    x.IdProducto == producto.IdProducto &&
                                    x.Activo)
                                .Sum(x => x.Stock);
                        }
                        else
                        {
                            producto.Stock += d.Cantidad;
                        }

                        //////////////////////////////////////////////////////
                        // REACTIVAR PRODUCTO
                        //////////////////////////////////////////////////////

                        if (producto.Stock > 0)
                        {
                            producto.Disponible = true;
                        }
                    }
                }

                //////////////////////////////////////////////////////
                // ELIMINAR HISTORIAL DE LA FACTURA
                //////////////////////////////////////////////////////

                var historialFactura = _context.FacturaHistorial
                    .Where(x => x.IdFactura == id)
                    .ToList();

                _context.FacturaHistorial.RemoveRange(historialFactura);

                //////////////////////////////////////////////////////
                // ELIMINAR DETALLES
                //////////////////////////////////////////////////////

                _context.FacturaDetalle
                    .RemoveRange(factura.Detalles);

                //////////////////////////////////////////////////////
                // ELIMINAR FACTURA
                //////////////////////////////////////////////////////

                _context.Facturas.Remove(factura);

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
        // TICKET
        //////////////////////////////////////////////////////

        [HttpGet]
        public IActionResult Ticket(int id)
        {
            var factura = _context.Facturas
                .Include(x => x.Detalles)
                .Include(x => x.Usuario)
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
    .Include(x => x.Detalles)
    .ToList()              // primero trae las facturas
    .Where(x => listaIds.Contains(x.IdFactura))
    .ToList();

            using var workbook = new XLWorkbook();

            var ws = workbook.Worksheets.Add("Envios");

            //////////////////////////////////////////////////////
            // HEADERS
            //////////////////////////////////////////////////////

            ws.Cell(1, 1).Value = "nombre*";
            ws.Cell(1, 2).Value = "telefono*";
            ws.Cell(1, 3).Value = "email";
            ws.Cell(1, 4).Value = "direccion*";
            ws.Cell(1, 5).Value = "referencia";
            ws.Cell(1, 6).Value = "departamento*";
            ws.Cell(1, 7).Value = "municipio*";
            ws.Cell(1, 8).Value = "contenido*";
            ws.Cell(1, 9).Value = "cantidad_bultos";
            ws.Cell(1, 10).Value = "valor_declarado*";
            ws.Cell(1, 11).Value = "observaciones";
            ws.Cell(1, 12).Value = "monto_cod";
            ws.Cell(1, 13).Value = "id_comercio";
            ws.Cell(1, 14).Value = "tipo_envio";
            ws.Cell(1, 15).Value = "agencia_origen";
            ws.Cell(1, 16).Value = "agencia_destino";
            ws.Cell(1, 17).Value = "lugar_encuentro";

            //////////////////////////////////////////////////////
            // ESTILO HEADER
            //////////////////////////////////////////////////////

            var headerRange =
                ws.Range(1, 1, 1, 17);

            headerRange.Style.Font.Bold = true;

            headerRange.Style.Fill.BackgroundColor =
                XLColor.Yellow;

            headerRange.Style.Font.FontColor =
                XLColor.Black;

            headerRange.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            headerRange.Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Center;

            headerRange.Style.Border.OutsideBorder =
                XLBorderStyleValues.Thin;

            headerRange.Style.Border.InsideBorder =
                XLBorderStyleValues.Thin;

            //////////////////////////////////////////////////////
            // DATA
            //////////////////////////////////////////////////////

            int fila = 2;

            foreach (var f in facturas)
            {
                ws.Cell(fila, 1).Value =
                    f.NombreCliente;

                ws.Cell(fila, 2).Value =
                    f.Telefono;

                //////////////////////////////////////////////////////
                // EMAIL AUTOMATICO
                //////////////////////////////////////////////////////

                ws.Cell(fila, 3).Value =
                "taosv503@gmail.com";

                //ws.Cell(fila, 3).Value =
                //"kigoelsalvador@gmail.com";

                //ws.Cell(fila, 3).Value =
                   // "watchmaker100@outlook.com";

                ws.Cell(fila, 4).Value =
                    f.Direccion;

                // 5. REFERENCIA
                ws.Cell(fila, 5).Value =
    f.Referencia ?? "";

                ws.Cell(fila, 6).Value =
                    f.Departamento;

                ws.Cell(fila, 7).Value =
                    f.Municipio;

                //ws.Cell(fila, 8).Value =
                // "JOYERIA";

                //////////////////////////////////////////////////////
                // DESCRIPCION PRODUCTOS
                //////////////////////////////////////////////////////

                //////////////////////////////////////////////////////
                // CONTENIDO - PRODUCTO + CANTIDAD + VARIANTE
                //////////////////////////////////////////////////////

                string descripcion = "";

                foreach (var d in f.Detalles)
                {
                    var producto = _context.Productos
                        .FirstOrDefault(x =>
                            x.IdProducto == d.IdProducto);

                    if (producto != null)
                    {
                        if (descripcion != "")
                        {
                            descripcion += ", ";
                        }

                        string lineaProducto =
                            $"{d.Cantidad}x {producto.Nombre}";

                        if (!string.IsNullOrWhiteSpace(d.Variante))
                        {
                            lineaProducto +=
                                $" {d.Variante}";
                        }

                        descripcion += lineaProducto;
                    }
                }

                ws.Cell(fila, 8).Value =
                    descripcion;

                ws.Cell(fila, 9).Value =
                    1;

                //////////////////////////////////////////////////////
                // VALOR DECLARADO
                //////////////////////////////////////////////////////

                ws.Cell(fila, 10).Value =
                    f.MetodoPago == "Efectivo"
                    ? f.Total
                    : 0;

                //////////////////////////////////////////////////////
                // OBSERVACIONES + COMENTARIO
                //////////////////////////////////////////////////////

                // 11. OBSERVACIONES
                string observacion =
                    "Llamar 30 min antes de la entrega";

                if (!string.IsNullOrWhiteSpace(f.Comentario))
                {
                    observacion +=
                        " | " + f.Comentario;
                }

                ws.Cell(fila, 11).Value =
                    observacion;

                ws.Cell(fila, 12).Value =
                   f.MetodoPago == "Efectivo"
                   ? f.Total
                   : 0;

                ws.Cell(fila, 13).Value =
    "";
                ws.Cell(fila, 14).Value =
    "Puerta a puerta";

                // 15. AGENCIA ORIGEN
                ws.Cell(fila, 15).Value =
                    "";

                // 16. AGENCIA DESTINO
                ws.Cell(fila, 16).Value =
                    "";

                // 17. LUGAR ENCUENTRO
                ws.Cell(fila, 17).Value =
                    "";

                //////////////////////////////////////////////////////
                // ESTILO FILAS
                //////////////////////////////////////////////////////

                var dataRange =
                    ws.Range(fila, 1, fila, 17);

                dataRange.Style.Border.OutsideBorder =
                    XLBorderStyleValues.Thin;

                dataRange.Style.Border.InsideBorder =
                    XLBorderStyleValues.Thin;

                //////////////////////////////////////////////////////
                // MARCAR GENERADO
                //////////////////////////////////////////////////////

                f.TicketGenerado = true;

                fila++;
            }

            _context.SaveChanges();

            //////////////////////////////////////////////////////
            // AJUSTAR COLUMNAS
            //////////////////////////////////////////////////////

            ws.Columns().AdjustToContents();

            //////////////////////////////////////////////////////
            // ALTO HEADER
            //////////////////////////////////////////////////////

            ws.Row(1).Height = 25;

            //////////////////////////////////////////////////////
            // CONGELAR HEADER
            //////////////////////////////////////////////////////

            ws.SheetView.FreezeRows(1);

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
                .Include(x => x.Usuario)
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



        //////////////////////////////////////////////////////
        // BUSCAR CLIENTES
        //////////////////////////////////////////////////////

        //////////////////////////////////////////////////////
        // BUSCAR CLIENTES
        //////////////////////////////////////////////////////

        [HttpGet]
        public JsonResult BuscarClientes(string filtro)
        {
            if (string.IsNullOrEmpty(filtro))
            {
                return Json(new List<object>());
            }

            var clientes = _context.Clientes

                //////////////////////////////////////////////////////
                // SOLO ACTIVOS
                //////////////////////////////////////////////////////

                .Where(x =>

                    x.Activo == true &&

                    (

                        x.Nombre.Contains(filtro) ||

                        x.Telefono.Contains(filtro)

                    )
                )

                .Select(x => new
                {
                    x.Idcliente,
                    x.Nombre,
                    x.Telefono,
                    x.Direccion
                })

                .Take(10)

                .ToList();

            return Json(clientes);
        }

        




    }

        //////////////////////////////////////////////////////
        // DTO FACTURA
        //////////////////////////////////////////////////////

        


}