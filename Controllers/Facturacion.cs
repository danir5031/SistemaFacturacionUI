using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaFacturacionUI.Controllers
{
    public class Facturacion : Controller
    {
        // GET: Facturacion
        public ActionResult Index()
        {
            var rol = HttpContext.Session.GetString("rol");

            if (rol != "ADMIN" && rol != "EMPLEADO")
            {
                return RedirectToAction("Index", "Login");
            }
            return View();
        }

        // GET: Facturacion/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: Facturacion/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Facturacion/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: Facturacion/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: Facturacion/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: Facturacion/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: Facturacion/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
