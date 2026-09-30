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

        // Nota mínima para poder aprobar una materia
        private const double NotaMinimaAprobacion = 6;

        public InscripcionesController(ApplicationDbContext context, UserManager<Usuario> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private bool EsStaff() => User.IsInRole("Docente") || User.IsInRole("Administrador");

        // Docente "puro": es docente y no es administrador (el admin puede todo)
        private bool EsSoloDocente() => User.IsInRole("Docente") && !User.IsInRole("Administrador");

        // ¿Esta materia está asignada a este docente?
        private Task<bool> DocenteTieneMateriaAsync(string? docenteId, int materiaId) =>
            _context.DocentesMaterias.AnyAsync(dm => dm.DocenteId == docenteId && dm.MateriaId == materiaId);

        // GET: Inscripciones (Vista Principal)
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var query = _context.Inscripciones
                .Include(i => i.Alumno)
                .Include(i => i.Materia)
                .AsQueryable();

            if (!EsStaff())
            {
                // Un alumno solo ve sus propias inscripciones
                query = query.Where(i => i.AlumnoId == userId);
            }
            else if (EsSoloDocente())
            {
                // Un docente solo ve las inscripciones de las materias que tiene asignadas
                query = query.Where(i => _context.DocentesMaterias
                    .Any(dm => dm.DocenteId == userId && dm.MateriaId == i.MateriaId));
            }

            ViewBag.EsStaff = EsStaff();
            return View(await query.OrderByDescending(i => i.AnioLectivo).ToListAsync());
        }

        // GET: Inscripciones/ObtenerFormulario/0 (Crear) o /5 (Editar)
        [HttpGet]
        public async Task<IActionResult> ObtenerFormulario(int id = 0)
        {
            ViewBag.EsStaff = EsStaff();

            if (id == 0)
            {
                await CargarListasAsync();

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

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Un alumno no puede editar inscripciones ajenas, ni siquiera abrir el formulario
            if (!EsStaff() && inscripcion.AlumnoId != userId)
            {
                return Forbid();
            }

            // Un docente no puede abrir inscripciones de materias que no tiene asignadas
            if (EsSoloDocente() && !await DocenteTieneMateriaAsync(userId, inscripcion.MateriaId))
            {
                return Forbid();
            }

            // Se pasa la materia actual para que siga apareciendo seleccionada al editar
            await CargarListasAsync(inscripcion.MateriaId);

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

            // Un docente solo puede gestionar inscripciones de materias que tiene asignadas
            if (EsSoloDocente())
            {
                // La materia elegida en el formulario debe ser una de las suyas
                if (!await DocenteTieneMateriaAsync(userId, inscripcion.MateriaId))
                {
                    ModelState.AddModelError(nameof(inscripcion.MateriaId),
                        "No podés gestionar inscripciones de una materia que no tenés asignada.");
                }

                // Y si edita una existente, la materia original también debe ser suya
                // (evita cambiar el MateriaId en el POST para saltear el control)
                if (inscripcion.Id != 0)
                {
                    var original = await _context.Inscripciones.AsNoTracking()
                        .FirstOrDefaultAsync(i => i.Id == inscripcion.Id);

                    if (original == null || !await DocenteTieneMateriaAsync(userId, original.MateriaId))
                    {
                        return Forbid();
                    }
                }
            }

            // Regla de negocio: solo se puede aprobar con la nota mínima
            if (inscripcion.Estado == EstadoInscripcion.Aprobada)
            {
                if (inscripcion.NotaFinal == null)
                {
                    ModelState.AddModelError(nameof(inscripcion.NotaFinal),
                        "Para aprobar la materia hay que cargar la nota final.");
                }
                else if (inscripcion.NotaFinal < NotaMinimaAprobacion)
                {
                    ModelState.AddModelError(nameof(inscripcion.NotaFinal),
                        $"No se puede aprobar con una nota menor a {NotaMinimaAprobacion}.");
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

            await CargarListasAsync(inscripcion.MateriaId);
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

            // Un docente solo puede eliminar inscripciones de sus materias
            if (EsSoloDocente())
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!await DocenteTieneMateriaAsync(userId, inscripcion.MateriaId))
                {
                    return Json(new { success = false, mensaje = "No tenés asignada la materia de esta inscripción." });
                }
            }

            _context.Inscripciones.Remove(inscripcion);
            await _context.SaveChangesAsync();
            return Json(new { success = true, mensaje = "Inscripción eliminada correctamente." });
        }

        private async Task CargarListasAsync(int? materiaActualId = null)
        {
            var alumnos = await _userManager.GetUsersInRoleAsync("Alumno");
            ViewBag.Alumnos = new SelectList(
                alumnos.OrderBy(a => a.Apellido).ThenBy(a => a.Nombre),
                "Id", "NombreCompleto");

            var materiasQuery = _context.Materias.Include(m => m.Carrera).AsQueryable();

            if (EsSoloDocente())
            {
                // Un docente solo puede elegir entre las materias que tiene asignadas
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                materiasQuery = materiasQuery.Where(m => _context.DocentesMaterias
                    .Any(dm => dm.DocenteId == userId && dm.MateriaId == m.Id));
            }
            else if (!EsStaff())
            {
                // Un alumno solo ve las materias donde todavía no está inscripto en el año actual
                // (más la materia que está editando, para que no desaparezca del desplegable)
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                int anioActual = DateTime.Now.Year;
                materiasQuery = materiasQuery.Where(m =>
                    m.Id == materiaActualId ||
                    !_context.Inscripciones.Any(i =>
                        i.AlumnoId == userId && i.MateriaId == m.Id && i.AnioLectivo == anioActual));
            }

            ViewBag.Materias = new SelectList(
                await materiasQuery.OrderBy(m => m.Nombre).ToListAsync(),
                "Id", "Nombre");
        }
    }
}