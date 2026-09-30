using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AppAlumnos.Models;

namespace AppAlumnos.Controllers
{
    [Authorize]
    public class PerfilController : Controller
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly IWebHostEnvironment _env;
        private static readonly string[] ExtensionesValidas = { ".jpg", ".jpeg", ".png" };
        private static readonly string[] MimesValidos = { "image/jpeg", "image/png" };
        private const long TamanioMaximo = 2 * 1024 * 1024; // 2 MB

        public PerfilController(UserManager<Usuario> userManager, IWebHostEnvironment env)
        {
            _userManager = userManager;
            _env = env;
        }

        // GET: Perfil/ObtenerFormulario
        [HttpGet]
        public async Task<IActionResult> ObtenerFormulario()
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return NotFound();
            return PartialView("_FormularioAvatarPartial", usuario);
        }

        // POST: Perfil/Guardar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guardar(IFormFile archivo)
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return Json(new { success = false, mensaje = "Usuario no encontrado." });

            if (archivo == null || archivo.Length == 0)
                return Json(new { success = false, mensaje = "Debe seleccionar una imagen." });

            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            if (!ExtensionesValidas.Contains(extension))
                return Json(new { success = false, mensaje = "Solo se permiten imágenes .jpg, .jpeg o .png." });

            if (!MimesValidos.Contains(archivo.ContentType))
                return Json(new { success = false, mensaje = "El tipo de archivo no es una imagen válida." });

            if (archivo.Length > TamanioMaximo)
                return Json(new { success = false, mensaje = "La imagen no puede superar los 2 MB." });

            var carpeta = Path.Combine(_env.WebRootPath, "images", "avatars");
            Directory.CreateDirectory(carpeta);

            var nombreArchivo = $"{Guid.NewGuid()}{extension}";
            var rutaFisica = Path.Combine(carpeta, nombreArchivo);

            using (var stream = new FileStream(rutaFisica, FileMode.Create))
            {
                await archivo.CopyToAsync(stream);
            }

            // Borrar avatar anterior si existía y no es el default
            if (!string.IsNullOrEmpty(usuario.FotoPerfilUrl))
            {
                var rutaAnterior = Path.Combine(_env.WebRootPath, usuario.FotoPerfilUrl.TrimStart('/'));
                if (System.IO.File.Exists(rutaAnterior))
                {
                    System.IO.File.Delete(rutaAnterior);
                }
            }

            usuario.FotoPerfilUrl = $"/images/avatars/{nombreArchivo}";
            await _userManager.UpdateAsync(usuario);

            return Json(new { success = true, mensaje = "Avatar actualizado correctamente.", url = usuario.FotoPerfilUrl });
        }
    }
}