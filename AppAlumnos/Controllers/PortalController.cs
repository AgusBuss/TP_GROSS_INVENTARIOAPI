using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AppAlumnos.Data;
using AppAlumnos.Models;
using System.Security.Claims;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AppAlumnos.Controllers
{
    [Authorize(Roles = "Alumno,Docente,Administrador")]
    public class PortalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Usuario> _userManager;

        public PortalController(ApplicationDbContext context, UserManager<Usuario> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Portal (landing con accesos según rol)
        public IActionResult Index()
        {
            return View();
        }

        private bool EsStaff() => User.IsInRole("Docente") || User.IsInRole("Administrador");

        // GET: Portal/HistoriaAcademica
        [Authorize(Roles = "Alumno,Docente,Administrador")]
        public async Task<IActionResult> HistoriaAcademica(string? alumnoId)
        {
            string? idAConsultar;

            if (User.IsInRole("Alumno") && !EsStaff())
            {
                idAConsultar = User.FindFirstValue(ClaimTypes.NameIdentifier);
            }
            else
            {
                idAConsultar = alumnoId;
            }

            if (string.IsNullOrEmpty(idAConsultar))
            {
                var alumnos = await _userManager.GetUsersInRoleAsync("Alumno");
                ViewBag.Alumnos = new SelectList(
                    alumnos.OrderBy(a => a.Apellido).ThenBy(a => a.Nombre),
                    "Id", "NombreCompleto");
                ViewBag.MostrarSelector = true;
                return View(new List<Inscripcion>());
            }

            var alumno = await _userManager.FindByIdAsync(idAConsultar);
            if (alumno == null) return NotFound();

            var historia = await _context.Inscripciones
                .Include(i => i.Materia)
                .Where(i => i.AlumnoId == idAConsultar)
                .OrderBy(i => i.AnioLectivo).ThenBy(i => i.Materia!.Nombre)
                .ToListAsync();

            ViewBag.MostrarSelector = false;
            ViewBag.NombreAlumno = alumno.NombreCompleto;
            return View(historia);
        }

        // GET: Portal/MisMaterias
        [Authorize(Roles = "Docente,Administrador")]
        public async Task<IActionResult> MisMaterias(string? docenteId)
        {
            string? idDocente;

            if (User.IsInRole("Docente") && !User.IsInRole("Administrador"))
            {
                idDocente = User.FindFirstValue(ClaimTypes.NameIdentifier);
            }
            else
            {
                idDocente = docenteId;

                if (string.IsNullOrEmpty(idDocente))
                {
                    var docentes = await _userManager.GetUsersInRoleAsync("Docente");
                    ViewBag.Docentes = new SelectList(
                        docentes.OrderBy(d => d.Apellido).ThenBy(d => d.Nombre),
                        "Id", "NombreCompleto");
                    ViewBag.MostrarSelector = true;
                    return View(new List<Materia>());
                }
            }

            var materias = await _context.DocentesMaterias
                .Include(dm => dm.Materia)
                .Where(dm => dm.DocenteId == idDocente)
                .Select(dm => dm.Materia!)
                .ToListAsync();

            var conteos = await _context.Inscripciones
                .Where(i => materias.Select(m => m.Id).Contains(i.MateriaId) && i.Estado == EstadoInscripcion.Cursando)
                .GroupBy(i => i.MateriaId)
                .Select(g => new { MateriaId = g.Key, Cantidad = g.Count() })
                .ToDictionaryAsync(x => x.MateriaId, x => x.Cantidad);

            ViewBag.Conteos = conteos;
            ViewBag.MostrarSelector = false;
            return View(materias);
        }

        // GET: Portal/DetalleMateria/5
        [Authorize(Roles = "Docente,Administrador")]
        public async Task<IActionResult> DetalleMateria(int id)
        {
            var materia = await _context.Materias.FindAsync(id);
            if (materia == null) return NotFound();

            if (User.IsInRole("Docente") && !User.IsInRole("Administrador"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                bool asignada = await _context.DocentesMaterias.AnyAsync(dm => dm.MateriaId == id && dm.DocenteId == userId);
                if (!asignada) return Forbid();
            }

            var inscriptos = await _context.Inscripciones
                .Include(i => i.Alumno)
                .Where(i => i.MateriaId == id)
                .OrderBy(i => i.Alumno!.Apellido)
                .ToListAsync();

            ViewBag.Materia = materia;
            return View(inscriptos);
        }

        // POST: Portal/CerrarCursada/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Docente,Administrador")]
        public async Task<IActionResult> CerrarCursada(int materiaId)
        {
            if (User.IsInRole("Docente") && !User.IsInRole("Administrador"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                bool asignada = await _context.DocentesMaterias.AnyAsync(dm => dm.MateriaId == materiaId && dm.DocenteId == userId);
                if (!asignada) return Forbid();
            }

            var pendientes = await _context.Inscripciones
                .Where(i => i.MateriaId == materiaId && i.Estado == EstadoInscripcion.Cursando)
                .ToListAsync();

            foreach (var inscripcion in pendientes)
            {
                inscripcion.Estado = EstadoInscripcion.Libre;
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, mensaje = $"Cursada cerrada. {pendientes.Count} alumno(s) pasaron a estado Libre." });
        }

        // GET: Portal/Certificado
        [Authorize(Roles = "Alumno,Docente,Administrador")]
        public async Task<IActionResult> Certificado(string? alumnoId)
        {
            string? idAConsultar;

            if (User.IsInRole("Alumno") && !EsStaff())
            {
                idAConsultar = User.FindFirstValue(ClaimTypes.NameIdentifier);
            }
            else
            {
                idAConsultar = alumnoId;
            }

            if (string.IsNullOrEmpty(idAConsultar))
            {
                var alumnos = await _userManager.GetUsersInRoleAsync("Alumno");
                ViewBag.Alumnos = new SelectList(
                    alumnos.OrderBy(a => a.Apellido).ThenBy(a => a.Nombre),
                    "Id", "NombreCompleto");
                ViewBag.MostrarSelectorAlumno = true;
                return View();
            }

            var alumno = await _userManager.FindByIdAsync(idAConsultar);
            if (alumno == null) return NotFound();

            ViewBag.MostrarSelectorAlumno = false;
            ViewBag.AlumnoId = idAConsultar;
            ViewBag.NombreAlumno = alumno.NombreCompleto;

            if (TempData["ErrorCertificado"] is string error)
            {
                ViewBag.Error = error;
            }

            return View();
        }

        // GET: Portal/GenerarCertificado
        [Authorize(Roles = "Alumno,Docente,Administrador")]
        public async Task<IActionResult> GenerarCertificado(string alumnoId, string tipo)
        {
            // Un alumno solo puede pedir su propio certificado, sin importar qué le manden por query string
            if (User.IsInRole("Alumno") && !EsStaff())
            {
                alumnoId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            }

            var alumno = await _userManager.FindByIdAsync(alumnoId);
            if (alumno == null) return NotFound();

            var inscripciones = await _context.Inscripciones
                .Include(i => i.Materia)
                .Where(i => i.AlumnoId == alumnoId)
                .OrderBy(i => i.AnioLectivo).ThenBy(i => i.Materia!.Nombre)
                .ToListAsync();

            byte[] pdfBytes;
            string nombreArchivo;

            if (tipo == "Regular")
            {
                int anioActual = DateTime.Now.Year;
                bool esRegular = inscripciones.Any(i => i.AnioLectivo == anioActual && i.Estado == EstadoInscripcion.Cursando);

                if (!esRegular)
                {
                    TempData["ErrorCertificado"] = "No se puede emitir el certificado de alumno regular: no hay materias en curso registradas para el año actual.";
                    return RedirectToAction(nameof(Certificado), new { alumnoId });
                }

                var materiasEnCurso = inscripciones.Where(i => i.AnioLectivo == anioActual && i.Estado == EstadoInscripcion.Cursando).ToList();
                pdfBytes = GenerarPdfRegular(alumno, materiasEnCurso, anioActual);
                nombreArchivo = $"CertificadoRegular_{alumno.Apellido}_{alumno.Nombre}.pdf";
            }
            else
            {
                var materiasAprobadas = inscripciones.Where(i => i.Estado == EstadoInscripcion.Aprobada).ToList();
                pdfBytes = GenerarPdfMateriasAprobadas(alumno, materiasAprobadas);
                nombreArchivo = $"CertificadoMateriasAprobadas_{alumno.Apellido}_{alumno.Nombre}.pdf";
            }

            return File(pdfBytes, "application/pdf", nombreArchivo);
        }

        private byte[] GenerarPdfRegular(Usuario alumno, List<Inscripcion> materiasEnCurso, int anioActual)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header()
                        .Text("Certificado de Alumno Regular")
                        .SemiBold().FontSize(20).AlignCenter();

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            column.Spacing(10);

                            column.Item().Text($"Se certifica que {alumno.Apellido}, {alumno.Nombre} (DNI {alumno.Dni}) " +
                                $"se encuentra registrado/a como alumno/a regular durante el año lectivo {anioActual}, " +
                                $"cursando las siguientes materias:");

                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Text("Materia").SemiBold();
                                    header.Cell().Text("Año").SemiBold();
                                    header.Cell().ColumnSpan(2).PaddingTop(5).BorderBottom(1);
                                });

                                foreach (var item in materiasEnCurso)
                                {
                                    table.Cell().Text(item.Materia?.Nombre ?? "");
                                    table.Cell().Text(item.Materia?.Anio.ToString() ?? "");
                                }
                            });

                            column.Item().PaddingTop(20).Text($"Se extiende el presente certificado a solicitud del interesado, a los {DateTime.Now:dd 'de' MMMM 'de' yyyy}.");
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("AppAlumnos - Sistema de Gestión Académica").FontSize(9).FontColor(Colors.Grey.Medium);
                        });
                });
            }).GeneratePdf();
        }

        private byte[] GenerarPdfMateriasAprobadas(Usuario alumno, List<Inscripcion> materiasAprobadas)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header()
                        .Text("Certificado de Materias Aprobadas")
                        .SemiBold().FontSize(20).AlignCenter();

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            column.Spacing(10);

                            column.Item().Text($"Se certifica que {alumno.Apellido}, {alumno.Nombre} (DNI {alumno.Dni}) " +
                                $"ha aprobado las siguientes materias:");

                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Text("Materia").SemiBold();
                                    header.Cell().Text("Año Lectivo").SemiBold();
                                    header.Cell().Text("Nota").SemiBold();
                                    header.Cell().ColumnSpan(3).PaddingTop(5).BorderBottom(1);
                                });

                                if (materiasAprobadas.Count == 0)
                                {
                                    table.Cell().ColumnSpan(3).Text("No hay materias aprobadas registradas.").Italic();
                                }
                                else
                                {
                                    foreach (var item in materiasAprobadas)
                                    {
                                        table.Cell().Text(item.Materia?.Nombre ?? "");
                                        table.Cell().Text(item.AnioLectivo.ToString());
                                        table.Cell().Text(item.NotaFinal?.ToString("0.##") ?? "-");
                                    }
                                }
                            });

                            column.Item().PaddingTop(20).Text($"Se extiende el presente certificado a solicitud del interesado, a los {DateTime.Now:dd 'de' MMMM 'de' yyyy}.");
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("AppAlumnos - Sistema de Gestión Académica").FontSize(9).FontColor(Colors.Grey.Medium);
                        });
                });
            }).GeneratePdf();
        }
    }
}