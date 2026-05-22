using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using SIMCRUL.Entity;
using System.Text.Json;

namespace SIMCRUL.Data.Context;

public class ApplicationDbContext : DbContext
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IHttpContextAccessor? httpContextAccessor = null) : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string CurrentUsername
    {
        get
        {
            var user = _httpContextAccessor?.HttpContext?.User;
            var name = user?.Identity?.Name ?? user?.FindFirst(ClaimTypes.Name)?.Value ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return name ?? "SYSTEM";
        }
        set { }
    }

    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<EmpresaTransporte> EmpresasTransporte => Set<EmpresaTransporte>();
    public DbSet<Conductor> Conductores => Set<Conductor>();
    public DbSet<Vehiculo> Vehiculos => Set<Vehiculo>();
    public DbSet<DispositivoGps> DispositivosGps => Set<DispositivoGps>();
    public DbSet<Ruta> Rutas => Set<Ruta>();
    public DbSet<Paradero> Paraderos => Set<Paradero>();
    public DbSet<RutaParadero> RutaParaderos => Set<RutaParadero>();
    public DbSet<RutaPuntoControl> RutaPuntosControl => Set<RutaPuntoControl>();
    public DbSet<AsignacionOperacion> AsignacionesOperacion => Set<AsignacionOperacion>();
    public DbSet<Viaje> Viajes => Set<Viaje>();
    public DbSet<GpsLectura> GpsLecturas => Set<GpsLectura>();
    public DbSet<TipoAlerta> TiposAlerta => Set<TipoAlerta>();
    public DbSet<Alerta> Alertas => Set<Alerta>();
    public DbSet<Pasajero> Pasajeros => Set<Pasajero>();
    public DbSet<FavoritoPasajero> FavoritosPasajero => Set<FavoritoPasajero>();
    public DbSet<ConsultaRuta> ConsultasRuta => Set<ConsultaRuta>();
    public DbSet<Auditoria> Auditorias => Set<Auditoria>();

    // Vistas keyless
    public DbSet<VwAlertaPendiente> VwAlertasPendientes => Set<VwAlertaPendiente>();
    public DbSet<VwUltimaUbicacionVehiculo> VwUltimaUbicacionVehiculos => Set<VwUltimaUbicacionVehiculo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Mapeos explicitos a tablas
        modelBuilder.Entity<Rol>().ToTable("ROLES").HasKey(x => x.IdRol);
        modelBuilder.Entity<Usuario>().ToTable("USUARIOS").HasKey(x => x.IdUsuario);
        modelBuilder.Entity<EmpresaTransporte>().ToTable("EMPRESAS_TRANSPORTE").HasKey(x => x.IdEmpresa);
        modelBuilder.Entity<Conductor>().ToTable("CONDUCTORES").HasKey(x => x.IdConductor);
        modelBuilder.Entity<Vehiculo>().ToTable("VEHICULOS").HasKey(x => x.IdVehiculo);
        modelBuilder.Entity<DispositivoGps>().ToTable("DISPOSITIVOS_GPS").HasKey(x => x.IdDispositivo);
        modelBuilder.Entity<Ruta>().ToTable("RUTAS").HasKey(x => x.IdRuta);
        modelBuilder.Entity<Paradero>().ToTable("PARADEROS").HasKey(x => x.IdParadero);
        modelBuilder.Entity<RutaParadero>().ToTable("RUTA_PARADEROS").HasKey(x => x.IdRutaParadero);
        modelBuilder.Entity<RutaPuntoControl>().ToTable("RUTA_PUNTOS_CONTROL").HasKey(x => x.IdPuntoControl);
        modelBuilder.Entity<AsignacionOperacion>().ToTable("ASIGNACIONES_OPERACION").HasKey(x => x.IdAsignacion);
        modelBuilder.Entity<Viaje>().ToTable("VIAJES").HasKey(x => x.IdViaje);
        modelBuilder.Entity<GpsLectura>().ToTable("GPS_LECTURAS").HasKey(x => x.IdLectura);
        modelBuilder.Entity<TipoAlerta>().ToTable("TIPOS_ALERTA").HasKey(x => x.IdTipoAlerta);
        modelBuilder.Entity<Alerta>().ToTable("ALERTAS").HasKey(x => x.IdAlerta);
        modelBuilder.Entity<Pasajero>().ToTable("PASAJEROS").HasKey(x => x.IdPasajero);
        modelBuilder.Entity<FavoritoPasajero>().ToTable("FAVORITOS_PASAJERO").HasKey(x => x.IdFavorito);
        modelBuilder.Entity<ConsultaRuta>().ToTable("CONSULTAS_RUTA").HasKey(x => x.IdConsulta);
        modelBuilder.Entity<Auditoria>().ToTable("AUDITORIA").HasKey(x => x.IdAuditoria);

        // Vistas mapeadas
        modelBuilder.Entity<VwAlertaPendiente>().ToView("vw_AlertasPendientes").HasNoKey();
        modelBuilder.Entity<VwUltimaUbicacionVehiculo>().ToView("vw_UltimaUbicacionVehiculo").HasNoKey();

        // Mapeo automático de propiedades CamelCase a columnas snake_case
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            if (entity.IsKeyless) continue;
            
            foreach (var property in entity.GetProperties())
            {
                var snakeName = ToSnakeCase(property.Name);
                property.SetColumnName(snakeName);
            }
        }

        // Relaciones especificas
        modelBuilder.Entity<Usuario>()
            .HasOne(u => u.Rol)
            .WithMany(r => r.Usuarios)
            .HasForeignKey(u => u.IdRol)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Conductor>()
            .HasOne(c => c.EmpresaTransporte)
            .WithMany(e => e.Conductores)
            .HasForeignKey(c => c.IdEmpresa)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Vehiculo>()
            .HasOne(v => v.EmpresaTransporte)
            .WithMany(e => e.Vehiculos)
            .HasForeignKey(v => v.IdEmpresa)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DispositivoGps>()
            .HasOne(d => d.Vehiculo)
            .WithMany(v => v.DispositivosGps)
            .HasForeignKey(d => d.IdVehiculo)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Ruta>()
            .HasOne(r => r.EmpresaTransporte)
            .WithMany(e => e.Rutas)
            .HasForeignKey(r => r.IdEmpresa)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RutaParadero>()
            .HasOne(rp => rp.Ruta)
            .WithMany(r => r.RutaParaderos)
            .HasForeignKey(rp => rp.IdRuta)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RutaParadero>()
            .HasOne(rp => rp.Paradero)
            .WithMany(p => p.RutaParaderos)
            .HasForeignKey(rp => rp.IdParadero)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RutaPuntoControl>()
            .HasOne(rpc => rpc.Ruta)
            .WithMany(r => r.RutaPuntosControl)
            .HasForeignKey(rpc => rpc.IdRuta)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AsignacionOperacion>()
            .HasOne(ao => ao.Ruta)
            .WithMany(r => r.Asignaciones)
            .HasForeignKey(ao => ao.IdRuta)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AsignacionOperacion>()
            .HasOne(ao => ao.Vehiculo)
            .WithMany(v => v.Asignaciones)
            .HasForeignKey(ao => ao.IdVehiculo)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AsignacionOperacion>()
            .HasOne(ao => ao.Conductor)
            .WithMany(c => c.Asignaciones)
            .HasForeignKey(ao => ao.IdConductor)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Viaje>()
            .HasOne(v => v.AsignacionOperacion)
            .WithMany(ao => ao.Viajes)
            .HasForeignKey(v => v.IdAsignacion)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GpsLectura>()
            .HasOne(g => g.Viaje)
            .WithMany(v => v.GpsLecturas)
            .HasForeignKey(g => g.IdViaje)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<GpsLectura>()
            .HasOne(g => g.Vehiculo)
            .WithMany()
            .HasForeignKey(g => g.IdVehiculo)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<GpsLectura>()
            .HasOne(g => g.DispositivoGps)
            .WithMany()
            .HasForeignKey(g => g.IdDispositivo)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Alerta>()
            .HasOne(a => a.TipoAlerta)
            .WithMany(ta => ta.Alertas)
            .HasForeignKey(a => a.IdTipoAlerta)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Alerta>()
            .HasOne(a => a.Viaje)
            .WithMany(v => v.Alertas)
            .HasForeignKey(a => a.IdViaje)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Alerta>()
            .HasOne(a => a.Vehiculo)
            .WithMany()
            .HasForeignKey(a => a.IdVehiculo)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Alerta>()
            .HasOne(a => a.Conductor)
            .WithMany()
            .HasForeignKey(a => a.IdConductor)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Alerta>()
            .HasOne(a => a.GpsLectura)
            .WithMany(g => g.Alertas)
            .HasForeignKey(a => a.IdLecturaGps)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Alerta>()
            .HasOne(a => a.AtendidoPorUsuario)
            .WithMany()
            .HasForeignKey(a => a.AtendidoPor)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<FavoritoPasajero>()
            .HasOne(f => f.Pasajero)
            .WithMany(p => p.Favoritos)
            .HasForeignKey(f => f.IdPasajero)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FavoritoPasajero>()
            .HasOne(f => f.Ruta)
            .WithMany(r => r.Favoritos)
            .HasForeignKey(f => f.IdRuta)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FavoritoPasajero>()
            .HasOne(f => f.ParaderoOrigen)
            .WithMany(p => p.FavoritosOrigen)
            .HasForeignKey(f => f.IdParaderoOrigen)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FavoritoPasajero>()
            .HasOne(f => f.ParaderoDestino)
            .WithMany(p => p.FavoritosDestino)
            .HasForeignKey(f => f.IdParaderoDestino)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ConsultaRuta>()
            .HasOne(cr => cr.Pasajero)
            .WithMany(p => p.Consultas)
            .HasForeignKey(cr => cr.IdPasajero)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ConsultaRuta>()
            .HasOne(cr => cr.Ruta)
            .WithMany(r => r.Consultas)
            .HasForeignKey(cr => cr.IdRuta)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return System.Text.RegularExpressions.Regex.Replace(input, "([a-z0-9])([A-Z])", "$1_$2").ToLower();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var auditEntries = OnBeforeSaveChanges();
        var result = await base.SaveChangesAsync(cancellationToken);
        await OnAfterSaveChanges(auditEntries, cancellationToken);
        return result;
    }

    private List<AuditEntry> OnBeforeSaveChanges()
    {
        ChangeTracker.DetectChanges();
        var auditEntries = new List<AuditEntry>();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is Auditoria || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                continue;

            var auditEntry = new AuditEntry(entry)
            {
                TableName = entry.Metadata.GetTableName() ?? entry.Metadata.Name,
                Username = CurrentUsername
            };
            auditEntries.Add(auditEntry);

            foreach (var property in entry.Properties)
            {
                string propertyName = property.Metadata.Name;
                if (property.Metadata.IsPrimaryKey())
                {
                    auditEntry.KeyValues[propertyName] = property.CurrentValue;
                    continue;
                }

                switch (entry.State)
                {
                    case EntityState.Added:
                        auditEntry.AuditType = "INSERT";
                        auditEntry.NewValues[propertyName] = property.CurrentValue;
                        break;

                    case EntityState.Deleted:
                        auditEntry.AuditType = "DELETE";
                        auditEntry.OldValues[propertyName] = property.OriginalValue;
                        break;

                    case EntityState.Modified:
                        if (property.IsModified)
                        {
                            auditEntry.ChangedColumns.Add(propertyName);
                            auditEntry.AuditType = "UPDATE";
                            auditEntry.OldValues[propertyName] = property.OriginalValue;
                            auditEntry.NewValues[propertyName] = property.CurrentValue;
                        }
                        break;
                }
            }
        }

        return auditEntries;
    }

    private async Task OnAfterSaveChanges(List<AuditEntry> auditEntries, CancellationToken cancellationToken)
    {
        if (auditEntries == null || auditEntries.Count == 0)
            return;

        foreach (var auditEntry in auditEntries)
        {
            var audit = new Auditoria
            {
                Usuario = auditEntry.Username,
                Accion = auditEntry.AuditType,
                TablaAfectada = auditEntry.TableName,
                Fecha = DateTime.UtcNow,
                DatosAnteriores = auditEntry.OldValues.Count == 0 ? null : JsonSerializer.Serialize(auditEntry.OldValues),
                DatosNuevos = auditEntry.NewValues.Count == 0 ? null : JsonSerializer.Serialize(auditEntry.NewValues)
            };
            Auditorias.Add(audit);
        }

        await base.SaveChangesAsync(cancellationToken);
    }
}

internal class AuditEntry(EntityEntry entry)
{
    public EntityEntry Entry { get; } = entry;
    public string TableName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string AuditType { get; set; } = string.Empty;
    public Dictionary<string, object?> KeyValues { get; } = new();
    public Dictionary<string, object?> OldValues { get; } = new();
    public Dictionary<string, object?> NewValues { get; } = new();
    public List<string> ChangedColumns { get; } = new();
}
