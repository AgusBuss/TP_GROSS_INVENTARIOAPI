using System.ComponentModel.DataAnnotations;

namespace AppAlumnos.Models
{
    public class DocenteMateria : AuditableEntity
    {
        public int Id { get; set; }

        [Required]
        public string DocenteId { get; set; } = string.Empty;
        public Usuario? Docente { get; set; }

        [Required]
        public int MateriaId { get; set; }
        public Materia? Materia { get; set; }
    }
}