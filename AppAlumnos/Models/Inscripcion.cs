using System.ComponentModel.DataAnnotations;

namespace AppAlumnos.Models
{
    public enum EstadoInscripcion
    {
        Cursando,
        Aprobada,
        Desaprobada,
        Libre
    }

    public class Inscripcion : AuditableEntity
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un alumno.")]
        [Display(Name = "Alumno")]
        public string AlumnoId { get; set; } = string.Empty;

        public Usuario? Alumno { get; set; }

        [Required(ErrorMessage = "Debe seleccionar una materia.")]
        [Display(Name = "Materia")]
        public int MateriaId { get; set; }

        public Materia? Materia { get; set; }

        [Required(ErrorMessage = "El año lectivo es obligatorio.")]
        [Range(2000, 2100, ErrorMessage = "Ingrese un año lectivo válido.")]
        [Display(Name = "Año Lectivo")]
        public int AnioLectivo { get; set; }

        [Required(ErrorMessage = "La fecha es obligatoria.")]
        [Display(Name = "Fecha")]
        [DataType(DataType.Date)]
        public DateTime Fecha { get; set; }

        [Required]
        [Display(Name = "Estado")]
        public EstadoInscripcion Estado { get; set; } = EstadoInscripcion.Cursando;

        [Range(0, 10, ErrorMessage = "La nota debe estar entre 0 y 10.")]
        [Display(Name = "Nota Final")]
        public double? NotaFinal { get; set; }
    }
}