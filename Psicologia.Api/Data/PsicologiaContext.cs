using Microsoft.EntityFrameworkCore;
using Psicologia.Api.Models;

namespace Psicologia.Api.Data;

public class PsicologiaContext : DbContext
{
    public PsicologiaContext(DbContextOptions<PsicologiaContext> options) : base(options) { }

    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Consultante> Consultantes => Set<Consultante>();
    public DbSet<Cita> Citas => Set<Cita>();
    public DbSet<NotaSesion> Notas => Set<NotaSesion>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<RegistroAcceso> Accesos => Set<RegistroAcceso>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Pesos colombianos: sin decimales
        b.Entity<Usuario>().Property(u => u.ValorSesion).HasColumnType("decimal(14,0)");
        b.Entity<Cita>().Property(c => c.Valor).HasColumnType("decimal(14,0)");
        b.Entity<Pago>().Property(p => p.Monto).HasColumnType("decimal(14,0)");

        // Índices únicos (igual que en la base de datos)
        b.Entity<Usuario>().HasIndex(u => u.Email).IsUnique();
        b.Entity<Consultante>().HasIndex(c => new { c.TipoDocumento, c.Documento }).IsUnique();
        b.Entity<NotaSesion>().HasIndex(n => n.IdCita).IsUnique();

        // Llaves foráneas explícitas (nuestros nombres empiezan por "Id")
        b.Entity<Usuario>().HasOne(u => u.Rol).WithMany().HasForeignKey(u => u.IdRol);

        b.Entity<Consultante>().HasOne(c => c.Profesional).WithMany().HasForeignKey(c => c.IdProfesional);

        b.Entity<Cita>().HasOne(c => c.Consultante).WithMany().HasForeignKey(c => c.IdConsultante);
        b.Entity<Cita>().HasOne(c => c.Profesional).WithMany().HasForeignKey(c => c.IdProfesional);
        b.Entity<Cita>().HasOne(c => c.CreadaPor).WithMany().HasForeignKey(c => c.IdCreadaPor);

        b.Entity<NotaSesion>().HasOne(n => n.Cita).WithOne().HasForeignKey<NotaSesion>(n => n.IdCita);
        b.Entity<NotaSesion>().HasOne(n => n.Consultante).WithMany().HasForeignKey(n => n.IdConsultante);
        b.Entity<NotaSesion>().HasOne(n => n.Profesional).WithMany().HasForeignKey(n => n.IdProfesional);

        b.Entity<Pago>().HasOne(p => p.Consultante).WithMany().HasForeignKey(p => p.IdConsultante);
        b.Entity<Pago>().HasOne(p => p.Cita).WithMany().HasForeignKey(p => p.IdCita);
        b.Entity<Pago>().HasOne(p => p.RegistradoPor).WithMany().HasForeignKey(p => p.IdRegistradoPor);

        b.Entity<RegistroAcceso>().HasOne(r => r.Usuario).WithMany().HasForeignKey(r => r.IdUsuario);

        // Nada se borra en cascada: la información clínica no desaparece por accidente
        foreach (var fk in b.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;
    }
}