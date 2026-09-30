using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AppAlumnos.Data;
using AppAlumnos.Models;
using System.Security.Claims;

namespace AppAlumnos.Controllers
{
    [Authorize]
    public class InscripcionesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Usuario> _userManager;

        public InscripcionesController(ApplicationDbContext context, UserManager<Usuario> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private bool EsStaff() => User.IsInRole("Docente") || User.IsInRole("Administrador");

        // GET: Inscripciones (Vista Principal)
        public async Task<IActionResult> Index()
        {
            var query = _context.Inscripciones
                .Include(i => i.Alumno)
                .Include(i => i.Materia)
                .AsQueryable();

            // Un alumno solo ve sus propias inscripciones. Docente/Admin ven todas.
            if (!EsStaff())
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                query = query.Where(i => i.AlumnoId == userId);
            }

            ViewBag.EsStaff = EsStaff();
            return View(await query.OrderByDescending(i => i.AnioLectivo).ToListAsync());
        }

        // GET: Inscripciones/ObtenerFormulario/0 (Crear) o /5 (Editar)
        [HttpGet]
        public async Task<IActionResult> ObtenerFormulario(int id = 0)
        {
            await CargarListasAsync();
            ViewBag.EsStaff = EsStaff();

            if (id == 0)
            {
                var nueva = new Inscripcion { AnioLectivo = DateTime.Now.Year, Fecha = DateTime.Today };

                // Si es alumno, se autocompleta y bloquea su propio Id
                if (!EsStaff())
                {
                    nueva.AlumnoId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
                }

                return PartialView("_FormularioInscripcionPartial", nueva);
            }

            var inscripcion = await _context.Inscripciones.FindAsync(id);
            if (inscripcion == null) return NotFound();

            // Un alumno no puede editar inscripciones ajenas, ni siquiera abrir el formulario
            if (!EsStaff() && inscripcion.AlumnoId != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return Forbid();
            }

            return PartialView("_FormularioInscripcionPartial", inscripcion);
        }

        // POST: Inscripciones/Guardar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guardar(Inscripcion inscripcion)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool esStaff = EsStaff();

            // Un alumno jamás puede mandar un AlumnoId distinto al suyo, ni tocar Estado/Nota
            if (!esStaff)
            {
                inscripcion.AlumnoId = userId ?? string.Empty;
                inscripcion.Estado = EstadoInscripcion.Cursando;
                inscripcion.NotaFinal = null;

                // Si además intentaba editar una inscripción ajena vía POST directo, lo bloqueamos
                if (inscripcion.Id != 0)
                {
                    var existenteAjena = await _context.Inscripciones.AsNoTracking()
                        .FirstOrDefaultAsync(i => i.Id == inscripcion.Id);
                    if (existenteAjena == null || existenteAjena.AlumnoId != userId)
                    {
                        return Forbid();
                    }
                }
            }

            // Regla de negocio: no permitir doble inscripción a la misma materia en el mismo año lectivo
            bool yaExiste = await _context.Inscripciones.AnyAsync(i =>
                i.AlumnoId == inscripcion.AlumnoId &&
                i.MateriaId == inscripcion.MateriaId &&
                i.AnioLectivo == inscripcion.AnioLectivo &&
                i.Id != inscripcion.Id);

            if (yaExiste)
            {
                ModelState.AddModelError(string.Empty, "Este alumno ya está inscripto en esa materia para el mismo año lectivo.");
            }

            if (ModelState.IsValid)
            {
                if (inscripcion.Id == 0)
                {
                    _context.Add(inscripcion);
                }
                else
                {
                    _context.Update(inscripcion);
                }
                await _context.SaveChangesAsync();
                return Json(new { success = true, mensaje = "Inscripción guardada correctamente." });
            }

            await CargarListasAsync();
            ViewBag.EsStaff = esStaff;
            return PartialView("_FormularioInscripcionPartial", inscripcion);
        }

        // POST: Inscripciones/EliminarAsync/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Docente,Administrador")]
        public async Task<IActionResult> EliminarAsync(int id)
        {
            var inscripcion = await _context.Inscripciones.FindAsync(id);
            if (inscripcion == null)
            {
                return Json(new { success = false, mensaje = "La inscripción no existe." });
            }

            _context.Inscripciones.Remove(inscripcion);
            await _context.SaveChangesAsync();
            return Json(new { success = true, mensaje = "Inscripción eliminada correctamente." });
        }

        private async Task CargarListasAsync()
        {
            var alumnos = await _userManager.GetUsersInRoleAsync("Alumno");
            ViewBag.Alumnos = new SelectList(
                alumnos.OrderBy(a => a.Apellido).ThenBy(a => a.Nombre),
                "Id", "NombreCompleto");

            ViewBag.Materias = new SelectList(
                await _context.Materias.Include(m => m.Carrera).OrderBy(m => m.Nombre).ToListAsync(),
                "Id", "Nombre");
        }
    }
}