using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AppAlumnos.Models;
using System.Security.Claims;

namespace AppAlumnos.Data
{
    public class ApplicationDbContext : IdentityDbContext<Usuario>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            IHttpContextAccessor httpContextAccessor)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<Materia> Materias { get; set; }
        public DbSet<Carrera> Carreras { get; set; }
        public DbSet<Inscripcion> Inscripciones { get; set; }
        public DbSet<DocenteMateria> DocentesMaterias { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Materia>()
                .HasOne(m => m.Carrera)
                .WithMany(c => c.Materias)
                .HasForeignKey(m => m.CarreraId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Inscripcion>()
                .HasOne(i => i.Alumno)
                .WithMany()
                .HasForeignKey(i => i.AlumnoId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Inscripcion>()
                .HasOne(i => i.Materia)
                .WithMany()
                .HasForeignKey(i => i.MateriaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<DocenteMateria>()
                .HasOne(dm => dm.Docente)
                .WithMany()
                .HasForeignKey(dm => dm.DocenteId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<DocenteMateria>()
                .HasOne(dm => dm.Materia)
                .WithMany()
                .HasForeignKey(dm => dm.MateriaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<DocenteMateria>()
                .HasIndex(dm => new { dm.DocenteId, dm.MateriaId })
                .IsUnique();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.FechaCreacion = DateTime.Now;
                    entry.Entity.UsuarioCreacionId = userId;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.FechaModificacion = DateTime.Now;
                    entry.Entity.UsuarioModificacionId = userId;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}