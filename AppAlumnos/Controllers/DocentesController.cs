using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AppAlumnos.Models;

namespace AppAlumnos.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class DocentesController : Controller
    {
        private readonly UserManager<Usuario> _userManager;

        public DocentesController(UserManager<Usuario> userManager)
        {
            _userManager = userManager;
        }

        // GET: Docentes
        public async Task<IActionResult> Index()
        {
            var docentes = await _userManager.GetUsersInRoleAsync("Docente");
            return View(docentes.OrderBy(d => d.Apellido).ThenBy(d => d.Nombre).ToList());
        }

        // GET: Docentes/ObtenerFormulario
        [HttpGet]
        public IActionResult ObtenerFormulario()
        {
            return PartialView("_FormularioDocentePartial", new CrearDocenteViewModel());
        }

        // POST: Docentes/Guardar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guardar(CrearDocenteViewModel modelo)
        {
            // Reglas que dependen de la base de datos
            if (await _userManager.FindByEmailAsync(modelo.Email) != null)
            {
                ModelState.AddModelError(nameof(modelo.Email), "Ya existe un usuario con ese email.");
            }

            if (await _userManager.Users.AnyAsync(u => u.Dni == modelo.Dni))
            {
                ModelState.AddModelError(nameof(modelo.Dni), "Ya existe un usuario con ese DNI.");
            }

            if (!ModelState.IsValid)
            {
                return PartialView("_FormularioDocentePartial", modelo);
            }

            var docente = new Usuario
            {
                UserName = modelo.Email,
                Email = modelo.Email,
                Nombre = modelo.Nombre,
                Apellido = modelo.Apellido,
                Dni = modelo.Dni,
                Legajo = modelo.Legajo,
                EmailConfirmed = true,
                Activo = true
            };

            // Identity valida la política de contraseña y la hashea
            var resultado = await _userManager.CreateAsync(docente, modelo.Password);
            if (!resultado.Succeeded)
            {
                foreach (var error in resultado.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return PartialView("_FormularioDocentePartial", modelo);
            }

            var rol = await _userManager.AddToRoleAsync(docente, "Docente");
            if (!rol.Succeeded)
            {
                // Para no dejar un usuario sin rol
                await _userManager.DeleteAsync(docente);
                ModelState.AddModelError(string.Empty, "No se pudo asignar el rol Docente. Intente nuevamente.");
                return PartialView("_FormularioDocentePartial", modelo);
            }

            return Json(new { success = true, mensaje = "Docente registrado correctamente." });
        }
    }
}