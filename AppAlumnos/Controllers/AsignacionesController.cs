using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AppAlumnos.Data;
using AppAlumnos.Models;

namespace AppAlumnos.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AsignacionesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Usuario> _userManager;

        public AsignacionesController(ApplicationDbContext context, UserManager<Usuario> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Asignaciones
        public async Task<IActionResult> Index()
        {
            var asignaciones = await _context.DocentesMaterias
                .Include(dm => dm.Docente)
                .Include(dm => dm.Materia)
                .OrderBy(dm => dm.Docente!.Apellido)
                .ToListAsync();

            return View(asignaciones);
        }

        // GET: Asignaciones/ObtenerFormulario/0
        [HttpGet]
        public async Task<IActionResult> ObtenerFormulario(int id = 0)
        {
            await CargarListasAsync();

            if (id == 0)
            {
                return PartialView("_FormularioAsignacionPartial", new DocenteMateria());
            }

            var asignacion = await _context.DocentesMaterias.FindAsync(id);
            if (asignacion == null) return NotFound();

            return PartialView("_FormularioAsignacionPartial", asignacion);
        }

        // POST: Asignaciones/Guardar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guardar(DocenteMateria asignacion)
        {
            bool yaExiste = await _context.DocentesMaterias.AnyAsync(dm =>
                dm.DocenteId == asignacion.DocenteId &&
                dm.MateriaId == asignacion.MateriaId &&
                dm.Id != asignacion.Id);

            if (yaExiste)
            {
                ModelState.AddModelError(string.Empty, "Ese docente ya está asignado a esa materia.");
            }

            if (ModelState.IsValid)
            {
                if (asignacion.Id == 0)
                {
                    _context.Add(asignacion);
                }
                else
                {
                    _context.Update(asignacion);
                }
                await _context.SaveChangesAsync();
                return Json(new { success = true, mensaje = "Asignación guardada correctamente." });
            }

            await CargarListasAsync();
            return PartialView("_FormularioAsignacionPartial", asignacion);
        }

        // POST: Asignaciones/EliminarAsync/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarAsync(int id)
        {
            var asignacion = await _context.DocentesMaterias.FindAsync(id);
            if (asignacion == null)
            {
                return Json(new { success = false, mensaje = "La asignación no existe." });
            }

            _context.DocentesMaterias.Remove(asignacion);
            await _context.SaveChangesAsync();
            return Json(new { success = true, mensaje = "Asignación eliminada correctamente." });
        }

        private async Task CargarListasAsync()
        {
            var docentes = await _userManager.GetUsersInRoleAsync("Docente");
            ViewBag.Docentes = new SelectList(
                docentes.OrderBy(d => d.Apellido).ThenBy(d => d.Nombre),
                "Id", "NombreCompleto");

            ViewBag.Materias = new SelectList(
                await _context.Materias.Include(m => m.Carrera).OrderBy(m => m.Nombre).ToListAsync(),
                "Id", "Nombre");
        }
    }
}