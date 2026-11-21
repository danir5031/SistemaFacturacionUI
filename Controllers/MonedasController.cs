
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaFacturacionUI.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaFacturacionUI.Controllers
{
    public class MonedasController : Controller
    {
        private readonly AppDbContext _context;

        public MonedasController(AppDbContext context)
        {
            _context = context;
        }

        private bool EsAdministrador()
        {
            return HttpContext.Session.GetString("rol") == "ADMIN";
        }

        // ==========================================
        // PANTALLA PRINCIPAL
        // ==========================================

        public async Task<IActionResult> Index()
        {
            var usuario = HttpContext.Session.GetString("usuario");

            if (string.IsNullOrEmpty(usuario))
                return RedirectToAction("Index", "Login");

            if (!EsAdministrador())
                return RedirectToAction("Index", "Empleado");

            ViewBag.Empresas = await _context.Empresas
                .AsNoTracking()
                .OrderBy(e => e.Nombre)
                .ToListAsync();

            ViewBag.Monedas = await _context.Monedas
                .AsNoTracking()
                .Where(m => m.Activa)
                .OrderBy(m => m.Codigo)
                .ToListAsync();

            ViewBag.EmpresaMonedas = await _context.EmpresaMonedas
                .AsNoTracking()
                .Include(em => em.Empresa)
                .Include(em => em.Moneda)
                .ToListAsync();

            ViewBag.TiposCambio = await _context.TiposCambio
                .AsNoTracking()
                .Include(tc => tc.Empresa)
                .Include(tc => tc.Moneda)
                .OrderByDescending(tc => tc.FechaVigencia)
                .ToListAsync();

            return View();
        }

        // ==========================================
        // CONFIGURAR MONEDA BASE
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfigurarMonedaBase(
            int idEmpresa, int idMoneda)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("usuario")))
                return Unauthorized(new { success = false, mensaje = "Tu sesión ha expirado." });

            if (!EsAdministrador())
                return Forbid();

            var empresa = await _context.Empresas
                .FirstOrDefaultAsync(e => e.IdEmpresa == idEmpresa && e.Activa);

            var moneda = await _context.Monedas
                .FirstOrDefaultAsync(m => m.IdMoneda == idMoneda && m.Activa);

            if (empresa == null || moneda == null)
            {
                return BadRequest(new
                {
                    success = false,
                    mensaje = "La empresa o moneda seleccionada no está disponible."
                });
            }

            empresa.IdMonedaBase = idMoneda;

            var relacion = await _context.EmpresaMonedas
                .FirstOrDefaultAsync(em =>
                    em.IdEmpresa == idEmpresa &&
                    em.IdMoneda == idMoneda);

            if (relacion == null)
            {
                _context.EmpresaMonedas.Add(new EmpresaMoneda
                {
                    IdEmpresa = idEmpresa,
                    IdMoneda = idMoneda,
                    Activa = true
                });
            }
            else
            {
                relacion.Activa = true;
            }

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                mensaje = "Moneda base configurada correctamente."
            });
        }

        // ==========================================
        // HABILITAR MONEDA PARA UNA EMPRESA
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarEmpresaMoneda(
            int idEmpresa, int idMoneda)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("usuario")))
                return Unauthorized(new { success = false, mensaje = "Tu sesión ha expirado." });

            if (!EsAdministrador())
                return Forbid();

            var empresa = await _context.Empresas
                .FirstOrDefaultAsync(e => e.IdEmpresa == idEmpresa && e.Activa);

            var moneda = await _context.Monedas
                .FirstOrDefaultAsync(m => m.IdMoneda == idMoneda && m.Activa);

            if (empresa == null || moneda == null)
            {
                return BadRequest(new
                {
                    success = false,
                    mensaje = "Selecciona una empresa y moneda activas."
                });
            }

            var relacion = await _context.EmpresaMonedas
                .FirstOrDefaultAsync(em =>
                    em.IdEmpresa == idEmpresa &&
                    em.IdMoneda == idMoneda);

            if (relacion != null)
            {
                if (relacion.Activa)
                {
                    return BadRequest(new
                    {
                        success = false,
                        mensaje = "Esta moneda ya está habilitada para la empresa."
                    });
                }

                relacion.Activa = true;
            }
            else
            {
                _context.EmpresaMonedas.Add(new EmpresaMoneda
                {
                    IdEmpresa = idEmpresa,
                    IdMoneda = idMoneda,
                    Activa = true
                });
            }

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                mensaje = "Moneda habilitada correctamente."
            });
        }

        // ==========================================
        // REGISTRAR TIPO DE CAMBIO
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarTipoCambio(
            int idEmpresa, int idMoneda,
            DateTime fechaVigencia, decimal tasa)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("usuario")))
                return Unauthorized(new { success = false, mensaje = "Tu sesión ha expirado." });

            if (!EsAdministrador())
                return Forbid();

            if (idEmpresa <= 0 || idMoneda <= 0 || tasa <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    mensaje = "Verifica los datos y registra una tasa mayor que cero."
                });
            }

            var empresa = await _context.Empresas
                .FirstOrDefaultAsync(e =>
                    e.IdEmpresa == idEmpresa && e.Activa);

            if (empresa == null)
            {
                return BadRequest(new
                {
                    success = false,
                    mensaje = "La empresa no está disponible."
                });
            }

            if (empresa.IdMonedaBase == idMoneda)
            {
                return BadRequest(new
                {
                    success = false,
                    mensaje = "No necesitas registrar tipo de cambio para la moneda base."
                });
            }

            bool monedaHabilitada = await _context.EmpresaMonedas
                .AnyAsync(em =>
                    em.IdEmpresa == idEmpresa &&
                    em.IdMoneda == idMoneda &&
                    em.Activa);

            if (!monedaHabilitada)
            {
                return BadRequest(new
                {
                    success = false,
                    mensaje = "Primero debes habilitar esta moneda para la empresa."
                });
            }

            var tipoCambio = await _context.TiposCambio
                .FirstOrDefaultAsync(tc =>
                    tc.IdEmpresa == idEmpresa &&
                    tc.IdMoneda == idMoneda &&
                    tc.FechaVigencia == fechaVigencia.Date);

            if (tipoCambio == null)
            {
                _context.TiposCambio.Add(new TipoCambio
                {
                    IdEmpresa = idEmpresa,
                    IdMoneda = idMoneda,
                    FechaVigencia = fechaVigencia.Date,
                    Tasa = tasa,
                    FechaRegistro = DateTime.Now
                });
            }
            else
            {
                tipoCambio.Tasa = tasa;
            }

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                mensaje = "Tipo de cambio guardado correctamente."
            });
        }


        // ==========================================
        // REGISTRAR NUEVA MONEDA
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarMoneda([FromForm] Moneda moneda)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("usuario")))
                return Unauthorized(new { success = false, mensaje = "Tu sesión ha expirado." });

            if (!EsAdministrador())
                return Forbid();

            if (string.IsNullOrWhiteSpace(moneda.Codigo) ||
                string.IsNullOrWhiteSpace(moneda.Nombre) ||
                string.IsNullOrWhiteSpace(moneda.Simbolo))
            {
                return BadRequest(new
                {
                    success = false,
                    mensaje = "Completa el código, nombre y símbolo de la moneda."
                });
            }

            moneda.Codigo = moneda.Codigo.Trim().ToUpper();
            moneda.Nombre = moneda.Nombre.Trim();
            moneda.Simbolo = moneda.Simbolo.Trim();

            if (moneda.Codigo.Length != 3 || moneda.Decimales > 6)
            {
                return BadRequest(new
                {
                    success = false,
                    mensaje = "El código debe tener 3 caracteres y los decimales no pueden superar 6."
                });
            }

            bool existe = await _context.Monedas
                .AnyAsync(m => m.Codigo == moneda.Codigo);

            if (existe)
            {
                return BadRequest(new
                {
                    success = false,
                    mensaje = "Ya existe una moneda con ese código."
                });
            }

            moneda.Activa = true;

            _context.Monedas.Add(moneda);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                mensaje = "Moneda registrada correctamente.",
                idMoneda = moneda.IdMoneda
            });
        }


        // ==========================================
        // OBTENER MONEDA PARA EDITAR
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> ObtenerMoneda(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("usuario")))
                return Unauthorized();

            if (!EsAdministrador())
                return Forbid();

            var moneda = await _context.Monedas
                .AsNoTracking()
                .Where(m => m.IdMoneda == id)
                .Select(m => new
                {
                    m.IdMoneda,
                    m.Codigo,
                    m.Nombre,
                    m.Simbolo,
                    m.Decimales,
                    m.Activa
                })
                .FirstOrDefaultAsync();

            if (moneda == null)
                return NotFound(new
                {
                    success = false,
                    mensaje = "Moneda no encontrada."
                });

            return Json(moneda);
        }


        // ==========================================
        // ACTUALIZAR MONEDA
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarMoneda([FromForm] Moneda datos)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("usuario")))
                return Unauthorized(new { success = false, mensaje = "Tu sesión ha expirado." });

            if (!EsAdministrador())
                return Forbid();

            if (datos.IdMoneda <= 0 ||
                string.IsNullOrWhiteSpace(datos.Codigo) ||
                string.IsNullOrWhiteSpace(datos.Nombre) ||
                string.IsNullOrWhiteSpace(datos.Simbolo))
            {
                return BadRequest(new
                {
                    success = false,
                    mensaje = "Completa todos los campos obligatorios."
                });
            }

            string codigo = datos.Codigo.Trim().ToUpper();

            if (codigo.Length != 3 || datos.Decimales > 6)
            {
                return BadRequest(new
                {
                    success = false,
                    mensaje = "El código debe tener 3 caracteres y los decimales no pueden superar 6."
                });
            }

            var moneda = await _context.Monedas
                .FirstOrDefaultAsync(m => m.IdMoneda == datos.IdMoneda);

            if (moneda == null)
                return NotFound(new { success = false, mensaje = "Moneda no encontrada." });

            bool codigoDuplicado = await _context.Monedas.AnyAsync(m =>
                m.IdMoneda != datos.IdMoneda &&
                m.Codigo == codigo);

            if (codigoDuplicado)
            {
                return BadRequest(new
                {
                    success = false,
                    mensaje = "Ese código ya pertenece a otra moneda."
                });
            }

            moneda.Codigo = codigo;
            moneda.Nombre = datos.Nombre.Trim();
            moneda.Simbolo = datos.Simbolo.Trim();
            moneda.Decimales = datos.Decimales;

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                mensaje = "Moneda actualizada correctamente."
            });
        }


        // ==========================================
        // ACTIVAR O DESACTIVAR MONEDA
        // ==========================================

        


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstadoMoneda(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("usuario")))
                return Unauthorized(new { success = false, mensaje = "Tu sesión ha expirado." });

            if (!EsAdministrador())
                return Forbid();

            var moneda = await _context.Monedas
                .FirstOrDefaultAsync(m => m.IdMoneda == id);

            if (moneda == null)
                return NotFound(new { success = false, mensaje = "Moneda no encontrada." });

            moneda.Activa = !moneda.Activa;

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                activo = moneda.Activa,
                mensaje = moneda.Activa
                    ? "Moneda activada correctamente."
                    : "Moneda desactivada correctamente."
            });
        }


    }
}
