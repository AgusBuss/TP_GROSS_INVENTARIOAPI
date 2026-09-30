using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using AppAlumnos.Data;
using AppAlumnos.Models;

namespace AppAlumnos.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class MateriasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MateriasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Materias (Vista Principal)
        public async Task<IActionResult> Index()
        {
            ViewBag.CarrerasFiltro = new SelectList(await _context.Carreras.OrderBy(c => c.Nombre).ToListAsync(), "Id", "Nombre");
            return View(await _context.Materias.Include(m => m.Carrera).ToListAsync());
        }

        // GET: Materias/FiltrarPorCarrera?carreraId=3
        [HttpGet]
        public async Task<IActionResult> FiltrarPorCarrera(int? carreraId)
        {
            var query = _context.Materias.Include(m => m.Carrera).AsQueryable();

            if (carreraId.HasValue)
            {
                query = query.Where(m => m.CarreraId == carreraId.Value);
            }

            var materias = await query.OrderBy(m => m.Nombre).ToListAsync();
            return PartialView("_TablaMateriasPartial", materias);
        }

        // GET: Materias/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var materia = await _context.Materias.Include(m => m.Carrera).FirstOrDefaultAsync(m => m.Id == id);
            if (materia == null) return NotFound();

            return View(materia);
        }

        // GET: Materias/ObtenerFormulario/0 (Crear) o /5 (Editar)
        [HttpGet]
        public async Task<IActionResult> ObtenerFormulario(int id = 0)
        {
            ViewBag.Carreras = new SelectList(await _context.Carreras.OrderBy(c => c.Nombre).ToListAsync(), "Id", "Nombre");

            if (id == 0)
            {
                return PartialView("_FormularioMateriaPartial", new Materia());
            }

            var materia = await _context.Materias.FindAsync(id);
            if (materia == null) return NotFound();

            return PartialView("_FormularioMateriaPartial", materia);
        }

        // POST: Materias/Guardar (Procesa Crear y Editar vía AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guardar(Materia materia)
        {
            if (ModelState.IsValid)
            {
                if (materia.Id == 0)
                {
                    _context.Add(materia);
                }
                else
                {
                    _context.Update(materia);
                }
                await _context.SaveChangesAsync();
                return Json(new { success = true, mensaje = "Materia guardada correctamente." });
            }

            ViewBag.Carreras = new SelectList(await _context.Carreras.OrderBy(c => c.Nombre).ToListAsync(), "Id", "Nombre", materia.CarreraId);
            return PartialView("_FormularioMateriaPartial", materia);
        }

        // POST: Materias/EliminarAsync/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarAsync(int id)
        {
            var materia = await _context.Materias.FindAsync(id);
            if (materia == null)
            {
                return Json(new { success = false, mensaje = "La materia no existe." });
            }

            try
            {
                _context.Materias.Remove(materia);
                await _context.SaveChangesAsync();
                return Json(new { success = true, mensaje = "Materia eliminada correctamente." });
            }
            catch (DbUpdateException)
            {
                return Json(new { success = false, mensaje = "No se puede eliminar: la materia tiene inscripciones asociadas." });
            }
        }
    }
}