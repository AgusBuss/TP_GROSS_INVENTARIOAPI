using System.ComponentModel.DataAnnotations;

namespace AppAlumnos.Models
{
    public class Carrera : AuditableEntity
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre de la carrera es obligatorio.")]
        [StringLength(150, ErrorMessage = "El nombre no puede superar los 150 caracteres.")]
        [Display(Name = "Nombre de la Carrera")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "La duración es obligatoria.")]
        [Range(1, 10, ErrorMessage = "La duración debe estar entre 1 y 10 años.")]
        [Display(Name = "Duración (años)")]
        public int DuracionAnios { get; set; }

        [StringLength(500, ErrorMessage = "La descripción no puede superar los 500 caracteres.")]
        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }

        // Navegación: una carrera tiene muchas materias
        public ICollection<Materia>? Materias { get; set; }
    }
}