using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using AppAlumnos.Models;
using AppAlumnos.Data;

namespace AppAlumnos.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class CarrerasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CarrerasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Carreras (Vista Principal)
        public async Task<IActionResult> Index()
        {
            return View(await _context.Carreras.ToListAsync());
        }

        // GET: Carreras/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var carrera = await _context.Carreras.Include(c => c.Materias).FirstOrDefaultAsync(m => m.Id == id);
            if (carrera == null) return NotFound();

            return View(carrera);
        }

        // GET: Carreras/ObtenerFormulario/0 (Crear) o /5 (Editar)
        [HttpGet]
        public async Task<IActionResult> ObtenerFormulario(int id = 0)
        {
            if (id == 0)
            {
                return PartialView("_FormularioCarreraPartial", new Carrera());
            }

            var carrera = await _context.Carreras.FindAsync(id);
            if (carrera == null) return NotFound();

            return PartialView("_FormularioCarreraPartial", carrera);
        }

        // POST: Carreras/Guardar (Procesa Crear y Editar vía AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guardar([Bind("Id,Nombre,DuracionAnios,Descripcion")] Carrera carrera)
        {
            if (ModelState.IsValid)
            {
                if (carrera.Id == 0)
                {
                    _context.Add(carrera);
                }
                else
                {
                    _context.Update(carrera);
                }
                await _context.SaveChangesAsync();
                return Json(new { success = true, mensaje = "Carrera guardada correctamente." });
            }

            return PartialView("_FormularioCarreraPartial", carrera);
        }

        // POST: Carreras/EliminarAsync/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarAsync(int id)
        {
            var carrera = await _context.Carreras.FindAsync(id);
            if (carrera == null)
            {
                return Json(new { success = false, mensaje = "La carrera no existe." });
            }

            try
            {
                _context.Carreras.Remove(carrera);
                await _context.SaveChangesAsync();
                return Json(new { success = true, mensaje = "Carrera eliminada correctamente." });
            }
            catch (DbUpdateException)
            {
                return Json(new { success = false, mensaje = "No se puede eliminar: la carrera tiene materias asociadas." });
            }
        }
    }
}