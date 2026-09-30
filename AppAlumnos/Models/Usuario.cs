using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace AppAlumnos.Models
{
    /// <summary>
    /// Entidad que extiende IdentityUser para almacenar los datos
    /// específicos de los perfiles del Sistema Académico.
    /// </summary>
    public class Usuario : IdentityUser
    {
        
            [Required(ErrorMessage = "El nombre es obligatorio.")]
            [StringLength(50, ErrorMessage = "El {0} no puede superar los {1} caracteres.")]
            [Display(Name = "Nombre")]
            public string Nombre { get; set; } = string.Empty;

            [Required(ErrorMessage = "El apellido es obligatorio.")]
            [StringLength(50, ErrorMessage = "El {0} no puede superar los {1} caracteres.")]
            [Display(Name = "Apellido")]
            public string Apellido { get; set; } = string.Empty;

            [Required(ErrorMessage = "El DNI es obligatorio.")]
            [Range(1000000, 99999999, ErrorMessage = "Ingrese un número de DNI válido (entre 7 y 8 dígitos).")]
            [Display(Name = "DNI")]
            public int Dni { get; set; }

            [StringLength(20, ErrorMessage = "El legajo no puede superar los {1} caracteres.")]
            [Display(Name = "Legajo")]
            public string? Legajo { get; set; }

            [Display(Name = "Foto de Perfil")]
            public string? FotoPerfilUrl { get; set; }

            [Required]
            [Display(Name = "Estado Activo")]
            public bool Activo { get; set; } = true;

            [Display(Name = "Fecha de Registro")]
            public DateTime FechaAlta { get; set; } = DateTime.Now;

            // Propiedad calculada (no genera columna en la Base de Datos)
            [Display(Name = "Nombre Completo")]
            public string NombreCompleto => $"{Apellido}, {Nombre}";

        }
}
