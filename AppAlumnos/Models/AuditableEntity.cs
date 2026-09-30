namespace AppAlumnos.Models
{
    public abstract class AuditableEntity
    {
        public DateTime FechaCreacion { get; set; }
        public string? UsuarioCreacionId { get; set; }

        public DateTime? FechaModificacion { get; set; }
        public string? UsuarioModificacionId { get; set; }
    }
}