using Microsoft.EntityFrameworkCore;
using SistemaEscolarWeb.Models;

namespace SistemaEscolarWeb.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Personal> Personal => Set<Personal>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Dependencia> Dependencias => Set<Dependencia>();
    public DbSet<Tecnologia> Tecnologias => Set<Tecnologia>();
    public DbSet<Marca> Marcas => Set<Marca>();
    public DbSet<Modelo> Modelos => Set<Modelo>();
    public DbSet<TipoTecnologia> TiposTecnologia => Set<TipoTecnologia>();
    public DbSet<EntradaTecnologia> EntradasTecnologia => Set<EntradaTecnologia>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<Asignacion> Asignaciones => Set<Asignacion>();
    public DbSet<Reparacion> Reparaciones => Set<Reparacion>();
    public DbSet<Baja> Bajas => Set<Baja>();
    public DbSet<SolicitudImpresion> SolicitudesImpresion => Set<SolicitudImpresion>();
    public DbSet<EstadoImpresion> EstadosImpresion => Set<EstadoImpresion>();
    public DbSet<Bitacora> Bitacoras => Set<Bitacora>();
    public DbSet<Insumo> Insumos => Set<Insumo>();
    public DbSet<TipoInsumo> TiposInsumo => Set<TipoInsumo>();
    public DbSet<EntradaInsumo> EntradasInsumo => Set<EntradaInsumo>();
    public DbSet<SalidaInsumo> SalidasInsumo => Set<SalidaInsumo>();
    public DbSet<Permiso> Permisos => Set<Permiso>();
    public DbSet<RolPermiso> RolesPermisos => Set<RolPermiso>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Personal>()
            .HasOne(p => p.Rol)
            .WithMany()
            .HasForeignKey(p => p.IdRol);

        modelBuilder.Entity<Usuario>()
            .HasOne(u => u.Personal)
            .WithOne()
            .HasForeignKey<Usuario>(u => u.RutPersonal)
            .HasPrincipalKey<Personal>(p => p.RutPersonal);

        modelBuilder.Entity<Usuario>()
            .HasOne(u => u.Rol)
            .WithMany()
            .HasForeignKey(u => u.IdRol);

        modelBuilder.Entity<SolicitudImpresion>()
            .HasOne(s => s.EstadoImpresion)
            .WithMany()
            .HasForeignKey(s => s.IdEstadoImpresion);

        modelBuilder.Entity<SolicitudImpresion>()
            .HasOne(s => s.Personal)
            .WithMany()
            .HasForeignKey(s => s.RutPersonal);

        modelBuilder.Entity<RolPermiso>()
            .HasKey(rp => new { rp.IdRol, rp.IdPermiso });
    }
}
