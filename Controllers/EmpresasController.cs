
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaFacturacionUI.Models;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaFacturacionUI.Controllers
{
    public class EmpresasController : Controller
    {
        private readonly AppDbContext _context;

        public EmpresasController(AppDbContext context)
        {
            _context = context;
        }

        private bool EsAdministrador()
        {
            return HttpContext.Session.GetString("rol") == "ADMIN";
        }

        // =====================================================
        // LISTADO DE EMPRESAS
        // =====================================================
        public async Task<IActionResult> Index()
        {
            var usuario = HttpContext.Session.GetString("usuario");

            if (string.IsNullOrEmpty(usuario))
                return RedirectToAction("Index", "Login");

            if (!EsAdministrador())
                return RedirectToAction("Index", "Empleado");


            var empresas = await _context.Empresas
                .AsNoTracking()
                .Include(e => e.Tiendas)
                .OrderBy(e => e.Nombre)
                .ToListAsync();

            // Monedas disponibles
            ViewBag.Monedas = await _context.Monedas
                .AsNoTracking()
                .Where(m => m.Activa)
                .OrderBy(m => m.Codigo)
                .ToListAsync();

            // Catálogo general: incluye monedas activas e inactivas
            ViewBag.CatalogoMonedas = await _context.Monedas
                .AsNoTracking()
                .OrderBy(m => m.Codigo)
                .ToListAsync();

            // Monedas asignadas a cada empresa
            ViewBag.EmpresaMonedas = await _context.EmpresaMonedas
                .AsNoTracking()
                .Include(em => em.Empresa)
                .Include(em => em.Moneda)
                .ToListAsync();

            // Historial de tipos de cambio
            ViewBag.TiposCambio = await _context.TiposCambio
                .AsNoTracking()
                .Include(tc => tc.Empresa)
                .Include(tc => tc.Moneda)
                .OrderByDescending(tc => tc.FechaVigencia)
                .ToListAsync();

            return View(empresas);

        }

        // =====================================================
        // GUARDAR EMPRESA DESDE AJAX
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarEmpresa([FromForm] Empresa empresa)
        {
            if (string.IsNullOrEmpty(
                HttpContext.Session.GetString("usuario")))
            {
                return Unauthorized(new
                {
                    success = false,
                    mensaje = "Tu sesión ha expirado."
                });
            }

            if (!EsAdministrador())
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                var errores = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new
                {
                    success = false,
                    mensaje = "Revisa los datos ingresados.",
                    errores
                });
            }

            empresa.Nombre = empresa.Nombre.Trim();

            empresa.NombreComercial =
                string.IsNullOrWhiteSpace(empresa.NombreComercial)
                    ? null
                    : empresa.NombreComercial.Trim();

            empresa.IdentificacionFiscal =
                string.IsNullOrWhiteSpace(empresa.IdentificacionFiscal)
                    ? null
                    : empresa.IdentificacionFiscal.Trim();

            empresa.Direccion =
                string.IsNullOrWhiteSpace(empresa.Direccion)
                    ? null
                    : empresa.Direccion.Trim();

            empresa.Telefono =
                string.IsNullOrWhiteSpace(empresa.Telefono)
                    ? null
                    : empresa.Telefono.Trim();

            empresa.Correo =
                string.IsNullOrWhiteSpace(empresa.Correo)
                    ? null
                    : empresa.Correo.Trim();

            // El sistema trabajará con método devengado.
            empresa.MetodoContable = "Devengado";

            empresa.FechaCreacion = System.DateTime.Now;

            _context.Empresas.Add(empresa);

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                mensaje = "Empresa registrada correctamente.",
                idEmpresa = empresa.IdEmpresa,
                nombre = empresa.Nombre
            });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarTienda([FromForm] Tienda tienda)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("usuario")))
                return Unauthorized(new
                {
                    success = false,
                    mensaje = "Tu sesión ha expirado."
                });

            if (!EsAdministrador())
                return Forbid();

            if (!ModelState.IsValid)
            {
                var errores = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new
                {
                    success = false,
                    mensaje = "Revisa los datos ingresados.",
                    errores
                });
            }

            var empresa = await _context.Empresas
                .FirstOrDefaultAsync(e =>
                    e.IdEmpresa == tienda.IdEmpresa && e.Activa);

            if (empresa == null)
            {
                return BadRequest(new
                {
                    success = false,
                    mensaje = "La empresa seleccionada no existe o está inactiva."
                });
            }

            tienda.Nombre = tienda.Nombre.Trim();

            tienda.Direccion = string.IsNullOrWhiteSpace(tienda.Direccion)
                ? null : tienda.Direccion.Trim();

            tienda.Telefono = string.IsNullOrWhiteSpace(tienda.Telefono)
                ? null : tienda.Telefono.Trim();

            tienda.Activa = true;

            bool existe = await _context.Tiendas.AnyAsync(t =>
                t.IdEmpresa == tienda.IdEmpresa &&
                t.Nombre.ToLower() == tienda.Nombre.ToLower());

            if (existe)
            {
                return BadRequest(new
                {
                    success = false,
                    mensaje = "Esta empresa ya tiene una tienda con ese nombre."
                });
            }

            _context.Tiendas.Add(tienda);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                mensaje = "Tienda registrada correctamente.",
                idTienda = tienda.IdTienda,
                nombre = tienda.Nombre,
                idEmpresa = tienda.IdEmpresa
            });
        }


        // Obtener información de una tienda para editarla
        [HttpGet]
        public async Task<IActionResult> ObtenerTienda(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("usuario")))
                return Unauthorized();

            if (!EsAdministrador())
                return Forbid();

            var tienda = await _context.Tiendas
                .AsNoTracking()
                .Where(t => t.IdTienda == id)
                .Select(t => new
                {
                    t.IdTienda,
                    t.IdEmpresa,
                    t.Nombre,
                    t.Direccion,
                    t.Telefono,
                    t.Activa
                })
                .FirstOrDefaultAsync();

            if (tienda == null)
                return NotFound(new { success = false, mensaje = "Tienda no encontrada." });

            return Json(tienda);
        }

        // Actualizar información de una tienda
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarTienda([FromForm] Tienda datos)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("usuario")))
                return Unauthorized(new { success = false, mensaje = "Tu sesión ha expirado." });

            if (!EsAdministrador())
                return Forbid();

            if (datos.IdTienda <= 0 ||
                string.IsNullOrWhiteSpace(datos.Nombre) ||
                datos.IdEmpresa <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    mensaje = "Completa los campos obligatorios."
                });
            }

            var tienda = await _context.Tiendas
                .FirstOrDefaultAsync(t => t.IdTienda == datos.IdTienda);

            if (tienda == null)
            {
                return NotFound(new
                {
                    success = false,
                    mensaje = "La tienda no existe."
                });
            }

            var empresa = await _context.Empresas
                .FirstOrDefaultAsync(e => e.IdEmpresa == datos.IdEmpresa && e.Activa);

            if (empresa == null)
            {
                return BadRequest(new
                {
                    success = false,
                    mensaje = "La empresa seleccionada no existe o está inactiva."
                });
            }

            string nombre = datos.Nombre.Trim();

            bool duplicada = await _context.Tiendas.AnyAsync(t =>
                t.IdTienda != datos.IdTienda &&
                t.IdEmpresa == datos.IdEmpresa &&
                t.Nombre.ToLower() == nombre.ToLower());

            if (duplicada)
            {
                return BadRequest(new
                {
                    success = false,
                    mensaje = "Ya existe una tienda con ese nombre en esta empresa."
                });
            }

            tienda.IdEmpresa = datos.IdEmpresa;
            tienda.Nombre = nombre;
            tienda.Direccion = string.IsNullOrWhiteSpace(datos.Direccion)
                ? null : datos.Direccion.Trim();
            tienda.Telefono = string.IsNullOrWhiteSpace(datos.Telefono)
                ? null : datos.Telefono.Trim();

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                mensaje = "Tienda actualizada correctamente."
            });
        }


        // Activar o desactivar una empresa
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstadoEmpresa(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("usuario")))
                return Unauthorized(new { success = false, mensaje = "Tu sesión ha expirado." });

            if (!EsAdministrador())
                return Forbid();

            var empresa = await _context.Empresas
                .FirstOrDefaultAsync(e => e.IdEmpresa == id);

            if (empresa == null)
                return NotFound(new { success = false, mensaje = "Empresa no encontrada." });

            // No permitir desactivar una empresa que tenga tiendas activas
            if (empresa.Activa)
            {
                bool tieneTiendasActivas = await _context.Tiendas
                    .AnyAsync(t => t.IdEmpresa == id && t.Activa);

                if (tieneTiendasActivas)
                {
                    return BadRequest(new
                    {
                        success = false,
                        mensaje = "Primero debes desactivar todas las tiendas de esta empresa."
                    });
                }
            }

            empresa.Activa = !empresa.Activa;

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                activo = empresa.Activa,
                mensaje = empresa.Activa
                    ? "Empresa activada correctamente."
                    : "Empresa desactivada correctamente."
            });
        }

        // Activar o desactivar una tienda
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstadoTienda(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("usuario")))
                return Unauthorized(new { success = false, mensaje = "Tu sesión ha expirado." });

            if (!EsAdministrador())
                return Forbid();

            var tienda = await _context.Tiendas
                .FirstOrDefaultAsync(t => t.IdTienda == id);

            if (tienda == null)
                return NotFound(new { success = false, mensaje = "Tienda no encontrada." });

            if (!tienda.Activa)
            {
                bool empresaActiva = await _context.Empresas
                    .AnyAsync(e => e.IdEmpresa == tienda.IdEmpresa && e.Activa);

                if (!empresaActiva)
                {
                    return BadRequest(new
                    {
                        success = false,
                        mensaje = "No puedes activar una tienda cuya empresa está inactiva."
                    });
                }
            }

            tienda.Activa = !tienda.Activa;

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                activo = tienda.Activa,
                mensaje = tienda.Activa
                    ? "Tienda activada correctamente."
                    : "Tienda desactivada correctamente."
            });
        }



    }
}
