using Microsoft.EntityFrameworkCore;
using SIMCRUL.Common.Constants;
using SIMCRUL.Data.Context;
using SIMCRUL.Entity;
using System.Security.Cryptography;
using System.Text;

namespace SIMCRUL.Data.Initialization;

public static class DatabaseInitializer
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        await context.Database.EnsureCreatedAsync();
        await EnsurePasswordRecoverySchemaAsync(context);
        await EnsureMaintenanceSchemaAsync(context);

        var company = await EnsureCompanyAsync(context);
        await EnsureRolesAsync(context);

        var admin = await EnsureUserAsync(
            context,
            "admin.flota",
            "admin.flota@simcrul.local",
            "Administrador",
            "Flota",
            "999111111",
            Roles.Administrador,
            "Admin123!");

        var chief = await EnsureUserAsync(
            context,
            "jefe.mantenimiento",
            "jefe.mantenimiento@simcrul.local",
            "Javier",
            "Rojas",
            "999222222",
            Roles.JefeMantenimiento,
            "Jefe123!");

        var technician = await EnsureUserAsync(
            context,
            "tecnico.mantenimiento",
            "tecnico.mantenimiento@simcrul.local",
            "Lucia",
            "Quispe",
            "999333333",
            Roles.TecnicoMantenimiento,
            "Tecnico123!");

        var driver = await EnsureUserAsync(
            context,
            "conductor.bus01",
            "conductor.bus01@simcrul.local",
            "Carlos",
            "Mendoza",
            "999444444",
            Roles.Conductor,
            "Conductor123!");

        await EnsureConductorRecordAsync(context, company.IdEmpresa, driver.IdUsuario);
        var vehicles = await EnsureVehiclesAsync(context, company.IdEmpresa);
        await EnsureDemoMaintenanceDataAsync(context, admin, chief, technician, driver, vehicles);
    }

    private static async Task EnsurePasswordRecoverySchemaAsync(ApplicationDbContext context)
    {
        const string sql = """
            IF OBJECT_ID('PASSWORD_RESET_TOKENS', 'U') IS NULL
            BEGIN
                CREATE TABLE PASSWORD_RESET_TOKENS
                (
                    id_password_reset_token INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    id_usuario INT NOT NULL,
                    token_hash NVARCHAR(200) NOT NULL,
                    expiration_utc DATETIME2 NOT NULL,
                    used_at_utc DATETIME2 NULL,
                    created_at_utc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                    requested_by_ip NVARCHAR(64) NULL,
                    email_sent_to NVARCHAR(200) NOT NULL,
                    CONSTRAINT FK_PASSWORD_RESET_TOKENS_USUARIOS FOREIGN KEY (id_usuario) REFERENCES USUARIOS(id_usuario)
                );

                CREATE INDEX IX_PASSWORD_RESET_TOKENS_ID_USUARIO ON PASSWORD_RESET_TOKENS(id_usuario);
                CREATE INDEX IX_PASSWORD_RESET_TOKENS_TOKEN_HASH ON PASSWORD_RESET_TOKENS(token_hash);
            END
            """;

        await context.Database.ExecuteSqlRawAsync(sql);
    }

    private static async Task EnsureMaintenanceSchemaAsync(ApplicationDbContext context)
    {
        const string vehicleColumnsSql = """
            IF COL_LENGTH('VEHICULOS', 'kilometraje_actual') IS NULL
            BEGIN
                ALTER TABLE VEHICULOS ADD kilometraje_actual DECIMAL(18,2) NOT NULL CONSTRAINT DF_VEHICULOS_KM_ACTUAL DEFAULT 0;
            END

            IF COL_LENGTH('VEHICULOS', 'estado_operativo') IS NULL
            BEGIN
                ALTER TABLE VEHICULOS ADD estado_operativo NVARCHAR(30) NOT NULL CONSTRAINT DF_VEHICULOS_ESTADO_OPERATIVO DEFAULT 'OPERATIVO';
            END

            IF COL_LENGTH('VEHICULOS', 'fecha_ultima_inspeccion') IS NULL
            BEGIN
                ALTER TABLE VEHICULOS ADD fecha_ultima_inspeccion DATETIME2 NULL;
            END

            IF COL_LENGTH('VEHICULOS', 'fecha_ultimo_mantenimiento') IS NULL
            BEGIN
                ALTER TABLE VEHICULOS ADD fecha_ultimo_mantenimiento DATETIME2 NULL;
            END

            IF COL_LENGTH('VEHICULOS', 'observaciones_mantenimiento') IS NULL
            BEGIN
                ALTER TABLE VEHICULOS ADD observaciones_mantenimiento NVARCHAR(1000) NULL;
            END
            """;

        const string vehicleNormalizationSql = """
            UPDATE VEHICULOS
            SET estado_operativo = ISNULL(NULLIF(estado_operativo, ''), 'OPERATIVO')
            WHERE estado_operativo IS NULL OR estado_operativo = '';
            """;

        const string inspectionsSql = """
            IF OBJECT_ID('INSPECCIONES_DIARIAS', 'U') IS NULL
            BEGIN
                CREATE TABLE INSPECCIONES_DIARIAS
                (
                    id_inspeccion_diaria INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    id_vehiculo INT NOT NULL,
                    id_conductor_usuario INT NOT NULL,
                    fecha_inspeccion DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                    kilometraje DECIMAL(18,2) NOT NULL DEFAULT 0,
                    nivel_combustible NVARCHAR(20) NOT NULL DEFAULT 'MEDIO',
                    limpieza_cabina BIT NOT NULL DEFAULT 1,
                    limpieza_exterior BIT NOT NULL DEFAULT 1,
                    luces_operativas BIT NOT NULL DEFAULT 1,
                    frenos_operativos BIT NOT NULL DEFAULT 1,
                    neumaticos_operativos BIT NOT NULL DEFAULT 1,
                    bocina_operativa BIT NOT NULL DEFAULT 1,
                    espejos_operativos BIT NOT NULL DEFAULT 1,
                    botiquin_disponible BIT NOT NULL DEFAULT 1,
                    extintor_disponible BIT NOT NULL DEFAULT 1,
                    documentos_completos BIT NOT NULL DEFAULT 1,
                    resultado NVARCHAR(20) NOT NULL DEFAULT 'APROBADO',
                    observaciones NVARCHAR(1000) NULL,
                    CONSTRAINT FK_INSPECCIONES_DIARIAS_VEHICULOS FOREIGN KEY (id_vehiculo) REFERENCES VEHICULOS(id_vehiculo),
                    CONSTRAINT FK_INSPECCIONES_DIARIAS_USUARIOS FOREIGN KEY (id_conductor_usuario) REFERENCES USUARIOS(id_usuario)
                );

                CREATE INDEX IX_INSPECCIONES_DIARIAS_VEHICULO_FECHA ON INSPECCIONES_DIARIAS(id_vehiculo, fecha_inspeccion DESC);
            END
            """;

        const string incidentsSql = """
            IF OBJECT_ID('INCIDENCIAS_MANTENIMIENTO', 'U') IS NULL
            BEGIN
                CREATE TABLE INCIDENCIAS_MANTENIMIENTO
                (
                    id_incidencia_mantenimiento INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    id_vehiculo INT NOT NULL,
                    id_reportado_por_usuario INT NOT NULL,
                    id_inspeccion_diaria INT NULL,
                    fecha_reporte DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                    tipo_incidencia NVARCHAR(30) NOT NULL DEFAULT 'MECANICA',
                    severidad NVARCHAR(20) NOT NULL DEFAULT 'MEDIA',
                    titulo NVARCHAR(150) NOT NULL,
                    descripcion NVARCHAR(2000) NOT NULL,
                    estado NVARCHAR(30) NOT NULL DEFAULT 'REPORTADA',
                    requiere_parada BIT NOT NULL DEFAULT 0,
                    ubicacion_referencia NVARCHAR(200) NULL,
                    fecha_cierre DATETIME2 NULL,
                    CONSTRAINT FK_INCIDENCIAS_VEHICULOS FOREIGN KEY (id_vehiculo) REFERENCES VEHICULOS(id_vehiculo),
                    CONSTRAINT FK_INCIDENCIAS_USUARIOS FOREIGN KEY (id_reportado_por_usuario) REFERENCES USUARIOS(id_usuario),
                    CONSTRAINT FK_INCIDENCIAS_INSPECCIONES FOREIGN KEY (id_inspeccion_diaria) REFERENCES INSPECCIONES_DIARIAS(id_inspeccion_diaria)
                );

                CREATE INDEX IX_INCIDENCIAS_ESTADO ON INCIDENCIAS_MANTENIMIENTO(estado, fecha_reporte DESC);
            END
            """;

        const string plansSql = """
            IF OBJECT_ID('PLANES_MANTENIMIENTO_PREVENTIVO', 'U') IS NULL
            BEGIN
                CREATE TABLE PLANES_MANTENIMIENTO_PREVENTIVO
                (
                    id_plan_mantenimiento_preventivo INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    id_vehiculo INT NOT NULL,
                    id_creado_por_usuario INT NOT NULL,
                    fecha_registro DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                    proxima_fecha_programada DATETIME2 NOT NULL,
                    frecuencia_dias INT NOT NULL,
                    frecuencia_kilometros DECIMAL(18,2) NULL,
                    actividades NVARCHAR(2000) NOT NULL,
                    estado NVARCHAR(20) NOT NULL DEFAULT 'PROGRAMADO',
                    prioridad NVARCHAR(20) NOT NULL DEFAULT 'MEDIA',
                    observaciones NVARCHAR(1000) NULL,
                    ultima_ejecucion DATETIME2 NULL,
                    CONSTRAINT FK_PLANES_VEHICULOS FOREIGN KEY (id_vehiculo) REFERENCES VEHICULOS(id_vehiculo),
                    CONSTRAINT FK_PLANES_USUARIOS FOREIGN KEY (id_creado_por_usuario) REFERENCES USUARIOS(id_usuario)
                );

                CREATE INDEX IX_PLANES_PROXIMA_FECHA ON PLANES_MANTENIMIENTO_PREVENTIVO(proxima_fecha_programada, estado);
            END
            """;

        const string ordersSql = """
            IF OBJECT_ID('ORDENES_TRABAJO', 'U') IS NULL
            BEGIN
                CREATE TABLE ORDENES_TRABAJO
                (
                    id_orden_trabajo INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    id_vehiculo INT NOT NULL,
                    id_generado_por_usuario INT NOT NULL,
                    id_tecnico_usuario INT NULL,
                    id_plan_mantenimiento_preventivo INT NULL,
                    id_incidencia_mantenimiento INT NULL,
                    numero_orden NVARCHAR(30) NOT NULL,
                    tipo_orden NVARCHAR(20) NOT NULL DEFAULT 'CORRECTIVO',
                    prioridad NVARCHAR(20) NOT NULL DEFAULT 'MEDIA',
                    estado NVARCHAR(20) NOT NULL DEFAULT 'GENERADA',
                    fecha_generacion DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                    fecha_programada DATETIME2 NOT NULL,
                    fecha_asignacion DATETIME2 NULL,
                    fecha_inicio DATETIME2 NULL,
                    fecha_fin DATETIME2 NULL,
                    trabajo_solicitado NVARCHAR(2000) NOT NULL,
                    diagnostico_inicial NVARCHAR(2000) NULL,
                    observaciones NVARCHAR(1000) NULL,
                    CONSTRAINT FK_ORDENES_VEHICULOS FOREIGN KEY (id_vehiculo) REFERENCES VEHICULOS(id_vehiculo),
                    CONSTRAINT FK_ORDENES_USUARIO_GENERA FOREIGN KEY (id_generado_por_usuario) REFERENCES USUARIOS(id_usuario),
                    CONSTRAINT FK_ORDENES_USUARIO_TECNICO FOREIGN KEY (id_tecnico_usuario) REFERENCES USUARIOS(id_usuario),
                    CONSTRAINT FK_ORDENES_PLAN FOREIGN KEY (id_plan_mantenimiento_preventivo) REFERENCES PLANES_MANTENIMIENTO_PREVENTIVO(id_plan_mantenimiento_preventivo),
                    CONSTRAINT FK_ORDENES_INCIDENCIA FOREIGN KEY (id_incidencia_mantenimiento) REFERENCES INCIDENCIAS_MANTENIMIENTO(id_incidencia_mantenimiento)
                );

                CREATE UNIQUE INDEX UX_ORDENES_TRABAJO_NUMERO ON ORDENES_TRABAJO(numero_orden);
                CREATE INDEX IX_ORDENES_TRABAJO_ESTADO ON ORDENES_TRABAJO(estado, fecha_programada DESC);
            END
            """;

        const string executionsSql = """
            IF OBJECT_ID('MANTENIMIENTOS_EJECUTADOS', 'U') IS NULL
            BEGIN
                CREATE TABLE MANTENIMIENTOS_EJECUTADOS
                (
                    id_mantenimiento_ejecutado INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    id_orden_trabajo INT NOT NULL,
                    id_tecnico_usuario INT NOT NULL,
                    fecha_inicio DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                    fecha_fin DATETIME2 NULL,
                    tipo_mantenimiento NVARCHAR(20) NOT NULL DEFAULT 'CORRECTIVO',
                    diagnostico NVARCHAR(2000) NOT NULL,
                    acciones_realizadas NVARCHAR(4000) NOT NULL,
                    recomendaciones NVARCHAR(2000) NULL,
                    estado_resultado NVARCHAR(30) NOT NULL DEFAULT 'COMPLETADO',
                    nuevo_estado_operativo_vehiculo NVARCHAR(30) NOT NULL DEFAULT 'OPERATIVO',
                    CONSTRAINT FK_MANTENIMIENTOS_ORDENES FOREIGN KEY (id_orden_trabajo) REFERENCES ORDENES_TRABAJO(id_orden_trabajo),
                    CONSTRAINT FK_MANTENIMIENTOS_TECNICO FOREIGN KEY (id_tecnico_usuario) REFERENCES USUARIOS(id_usuario)
                );

                CREATE INDEX IX_MANTENIMIENTOS_FECHA_FIN ON MANTENIMIENTOS_EJECUTADOS(fecha_fin DESC);
            END
            """;

        const string sparePartsSql = """
            IF OBJECT_ID('REPUESTOS_UTILIZADOS', 'U') IS NULL
            BEGIN
                CREATE TABLE REPUESTOS_UTILIZADOS
                (
                    id_repuesto_utilizado INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    id_mantenimiento_ejecutado INT NOT NULL,
                    codigo_repuesto NVARCHAR(50) NOT NULL,
                    nombre_repuesto NVARCHAR(150) NOT NULL,
                    cantidad DECIMAL(18,2) NOT NULL DEFAULT 1,
                    costo_unitario DECIMAL(18,2) NOT NULL DEFAULT 0,
                    observaciones NVARCHAR(500) NULL,
                    CONSTRAINT FK_REPUESTOS_MANTENIMIENTOS FOREIGN KEY (id_mantenimiento_ejecutado) REFERENCES MANTENIMIENTOS_EJECUTADOS(id_mantenimiento_ejecutado)
                );
            END
            """;

        await context.Database.ExecuteSqlRawAsync(vehicleColumnsSql);
        await context.Database.ExecuteSqlRawAsync(vehicleNormalizationSql);
        await context.Database.ExecuteSqlRawAsync(inspectionsSql);
        await context.Database.ExecuteSqlRawAsync(incidentsSql);
        await context.Database.ExecuteSqlRawAsync(plansSql);
        await context.Database.ExecuteSqlRawAsync(ordersSql);
        await context.Database.ExecuteSqlRawAsync(executionsSql);
        await context.Database.ExecuteSqlRawAsync(sparePartsSql);
        await EnsureMaintenanceDashboardStoredProceduresAsync(context);
    }

    private static async Task EnsureMaintenanceDashboardStoredProceduresAsync(ApplicationDbContext context)
    {
        const string dashboardSummarySp = """
            CREATE OR ALTER PROCEDURE dbo.sp_MaintenanceDashboardSummary
            AS
            BEGIN
                SET NOCOUNT ON;

                DECLARE @today DATE = CONVERT(DATE, SYSUTCDATETIME());
                DECLARE @monthStart DATE = DATEFROMPARTS(YEAR(@today), MONTH(@today), 1);

                SELECT
                    TotalVehiculos = COUNT(CASE WHEN v.estado = 1 THEN 1 END),
                    VehiculosOperativos = COUNT(CASE WHEN v.estado = 1 AND v.estado_operativo = 'OPERATIVO' THEN 1 END),
                    VehiculosEnMantenimiento = COUNT(CASE WHEN v.estado = 1 AND v.estado_operativo = 'EN_MANTENIMIENTO' THEN 1 END),
                    VehiculosFueraDeServicio = COUNT(CASE WHEN v.estado = 1 AND v.estado_operativo = 'FUERA_DE_SERVICIO' THEN 1 END),
                    InspeccionesHoy = (SELECT COUNT(1) FROM INSPECCIONES_DIARIAS i WHERE i.fecha_inspeccion >= @today),
                    IncidenciasAbiertas = (SELECT COUNT(1) FROM INCIDENCIAS_MANTENIMIENTO i WHERE i.estado <> 'CERRADA'),
                    OrdenesPendientes = (SELECT COUNT(1) FROM ORDENES_TRABAJO o WHERE o.estado <> 'FINALIZADA'),
                    OrdenesFinalizadasMes = (SELECT COUNT(1) FROM ORDENES_TRABAJO o WHERE o.estado = 'FINALIZADA' AND o.fecha_fin >= @monthStart)
                FROM VEHICULOS v;
            END
            """;

        const string incidentsByTypeSp = """
            CREATE OR ALTER PROCEDURE dbo.sp_MaintenanceDashboardIncidenciasPorTipo
            AS
            BEGIN
                SET NOCOUNT ON;

                SELECT
                    Label = tipo_incidencia,
                    Value = COUNT(1)
                FROM INCIDENCIAS_MANTENIMIENTO
                GROUP BY tipo_incidencia
                ORDER BY Value DESC;
            END
            """;

        const string ordersByStatusSp = """
            CREATE OR ALTER PROCEDURE dbo.sp_MaintenanceDashboardOrdenesPorEstado
            AS
            BEGIN
                SET NOCOUNT ON;

                SELECT
                    Label = estado,
                    Value = COUNT(1)
                FROM ORDENES_TRABAJO
                GROUP BY estado
                ORDER BY Value DESC;
            END
            """;

        const string monthlyHistorySp = """
            CREATE OR ALTER PROCEDURE dbo.sp_MaintenanceDashboardHistorialMensual
            AS
            BEGIN
                SET NOCOUNT ON;

                DECLARE @fromDate DATE = DATEADD(MONTH, -5, CONVERT(DATE, SYSUTCDATETIME()));

                SELECT
                    Label = CONCAT(YEAR(fecha_generacion), '-', RIGHT(CONCAT('00', MONTH(fecha_generacion)), 2)),
                    Value = COUNT(1)
                FROM ORDENES_TRABAJO
                WHERE fecha_generacion >= @fromDate
                GROUP BY YEAR(fecha_generacion), MONTH(fecha_generacion)
                ORDER BY Label;
            END
            """;

        const string upcomingPlansSp = """
            CREATE OR ALTER PROCEDURE dbo.sp_MaintenanceDashboardProximosPreventivos
            AS
            BEGIN
                SET NOCOUNT ON;

                SELECT TOP (5)
                    IdPlanMantenimientoPreventivo = p.id_plan_mantenimiento_preventivo,
                    Vehiculo = CONCAT(v.codigo_interno, ' - ', v.placa),
                    ProximaFechaProgramada = p.proxima_fecha_programada,
                    Prioridad = p.prioridad,
                    Actividades = p.actividades
                FROM PLANES_MANTENIMIENTO_PREVENTIVO p
                INNER JOIN VEHICULOS v ON v.id_vehiculo = p.id_vehiculo
                WHERE p.estado = 'PROGRAMADO'
                ORDER BY p.proxima_fecha_programada;
            END
            """;

        await context.Database.ExecuteSqlRawAsync(dashboardSummarySp);
        await context.Database.ExecuteSqlRawAsync(incidentsByTypeSp);
        await context.Database.ExecuteSqlRawAsync(ordersByStatusSp);
        await context.Database.ExecuteSqlRawAsync(monthlyHistorySp);
        await context.Database.ExecuteSqlRawAsync(upcomingPlansSp);
    }

    private static async Task EnsureRolesAsync(ApplicationDbContext context)
    {
        var requiredRoles = new[]
        {
            Roles.Administrador,
            Roles.JefeMantenimiento,
            Roles.TecnicoMantenimiento,
            Roles.Conductor
        };

        foreach (var roleName in requiredRoles)
        {
            var role = await context.Roles.FirstOrDefaultAsync(r => r.Nombre == roleName);
            if (role != null)
            {
                role.Activo = true;
                role.Descripcion = $"Rol de {roleName} para gestion del mantenimiento de flota.";
                continue;
            }

            context.Roles.Add(new Rol
            {
                Nombre = roleName,
                Descripcion = $"Rol de {roleName} para gestion del mantenimiento de flota.",
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task<EmpresaTransporte> EnsureCompanyAsync(ApplicationDbContext context)
    {
        var company = await context.EmpresasTransporte
            .OrderBy(e => e.IdEmpresa)
            .FirstOrDefaultAsync();

        if (company != null)
        {
            company.Estado = true;
            if (string.IsNullOrWhiteSpace(company.NombreComercial))
            {
                company.NombreComercial = "SIMCRUL Flota";
            }

            await context.SaveChangesAsync();
            return company;
        }

        company = new EmpresaTransporte
        {
            Ruc = "20601234567",
            RazonSocial = "SIMCRUL Transporte y Mantenimiento S.A.C.",
            NombreComercial = "SIMCRUL Flota",
            Direccion = "Av. Industrial 100, Lima",
            Telefono = "014445555",
            Email = "operaciones@simcrul.local",
            Estado = true,
            FechaRegistro = DateTime.UtcNow
        };

        context.EmpresasTransporte.Add(company);
        await context.SaveChangesAsync();
        return company;
    }

    private static async Task<Usuario> EnsureUserAsync(
        ApplicationDbContext context,
        string username,
        string email,
        string nombres,
        string apellidos,
        string telefono,
        string roleName,
        string password)
    {
        var role = await context.Roles.FirstAsync(r => r.Nombre == roleName);
        var user = await context.Usuarios
            .FirstOrDefaultAsync(u => u.Username == username || u.Email == email);

        if (user == null)
        {
            user = new Usuario
            {
                Username = username,
                Email = email,
                Nombres = nombres,
                Apellidos = apellidos,
                Telefono = telefono,
                PasswordHash = ComputeHash(password),
                IdRol = role.IdRol,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };

            context.Usuarios.Add(user);
        }
        else
        {
            user.Username = username;
            user.Email = email;
            user.Nombres = nombres;
            user.Apellidos = apellidos;
            user.Telefono = telefono;
            user.PasswordHash = ComputeHash(password);
            user.IdRol = role.IdRol;
            user.Activo = true;
        }

        await context.SaveChangesAsync();
        return user;
    }

    private static async Task EnsureConductorRecordAsync(ApplicationDbContext context, int companyId, int userId)
    {
        var existing = await context.Conductores.FirstOrDefaultAsync(c =>
            c.IdUsuario == userId ||
            c.Dni == "72849284" ||
            c.NumeroLicencia == "Q72849284");

        if (existing != null)
        {
            existing.IdEmpresa = companyId;
            existing.IdUsuario = userId;
            existing.Nombres = "Carlos";
            existing.Apellidos = "Mendoza";
            existing.Dni = "72849284";
            existing.NumeroLicencia = "Q72849284";
            existing.CategoriaLicencia = "A-IIIB";
            existing.FechaVencimientoLicencia = DateTime.Today.AddYears(2);
            existing.Telefono = "999444444";
            existing.Estado = true;
            await context.SaveChangesAsync();
            return;
        }

        context.Conductores.Add(new Conductor
        {
            IdEmpresa = companyId,
            IdUsuario = userId,
            Nombres = "Carlos",
            Apellidos = "Mendoza",
            Dni = "72849284",
            NumeroLicencia = "Q72849284",
            CategoriaLicencia = "A-IIIB",
            FechaVencimientoLicencia = DateTime.Today.AddYears(2),
            Telefono = "999444444",
            Estado = true,
            FechaRegistro = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
    }

    private static async Task<List<Vehiculo>> EnsureVehiclesAsync(ApplicationDbContext context, int companyId)
    {
        var seeds = new[]
        {
            new { Placa = "ABC-101", Codigo = "BUS-101", Tipo = "BUS", Marca = "Mercedes-Benz", Modelo = "OF-1721", Anio = 2021, Capacidad = 45, Velocidad = 90M, Km = 124560M, EstadoOperativo = "OPERATIVO", Observaciones = "Unidad disponible para servicio." },
            new { Placa = "ABC-102", Codigo = "BUS-102", Tipo = "BUS", Marca = "Volvo", Modelo = "B270F", Anio = 2020, Capacidad = 50, Velocidad = 90M, Km = 143220M, EstadoOperativo = "EN_MANTENIMIENTO", Observaciones = "Pendiente de atencion por sistema de frenos." },
            new { Placa = "ABC-103", Codigo = "BUS-103", Tipo = "BUS", Marca = "Scania", Modelo = "K280", Anio = 2022, Capacidad = 52, Velocidad = 90M, Km = 98740M, EstadoOperativo = "OPERATIVO", Observaciones = "Plan preventivo programado." }
        };

        foreach (var seed in seeds)
        {
            var vehicle = await context.Vehiculos.FirstOrDefaultAsync(v => v.Placa == seed.Placa);
            if (vehicle == null)
            {
                vehicle = new Vehiculo
                {
                    IdEmpresa = companyId,
                    Placa = seed.Placa,
                    CodigoInterno = seed.Codigo,
                    TipoVehiculo = seed.Tipo,
                    Marca = seed.Marca,
                    Modelo = seed.Modelo,
                    Anio = seed.Anio,
                    CapacidadPasajeros = seed.Capacidad,
                    VelocidadMaximaKmh = seed.Velocidad,
                    KilometrajeActual = seed.Km,
                    EstadoOperativo = seed.EstadoOperativo,
                    ObservacionesMantenimiento = seed.Observaciones,
                    Estado = true,
                    FechaRegistro = DateTime.UtcNow
                };

                context.Vehiculos.Add(vehicle);
            }
            else
            {
                vehicle.IdEmpresa = companyId;
                vehicle.CodigoInterno = seed.Codigo;
                vehicle.TipoVehiculo = seed.Tipo;
                vehicle.Marca = seed.Marca;
                vehicle.Modelo = seed.Modelo;
                vehicle.Anio = seed.Anio;
                vehicle.CapacidadPasajeros = seed.Capacidad;
                vehicle.VelocidadMaximaKmh = seed.Velocidad;
                vehicle.KilometrajeActual = seed.Km;
                vehicle.EstadoOperativo = seed.EstadoOperativo;
                vehicle.ObservacionesMantenimiento = seed.Observaciones;
                vehicle.Estado = true;
            }
        }

        await context.SaveChangesAsync();

        return await context.Vehiculos
            .Where(v => seeds.Select(s => s.Placa).Contains(v.Placa))
            .OrderBy(v => v.Placa)
            .ToListAsync();
    }

    private static async Task EnsureDemoMaintenanceDataAsync(
        ApplicationDbContext context,
        Usuario admin,
        Usuario chief,
        Usuario technician,
        Usuario driver,
        List<Vehiculo> vehicles)
    {
        var bus101 = vehicles.First(v => v.Placa == "ABC-101");
        var bus102 = vehicles.First(v => v.Placa == "ABC-102");
        var bus103 = vehicles.First(v => v.Placa == "ABC-103");

        if (!await context.InspeccionesDiarias.AnyAsync(i => i.IdVehiculo == bus101.IdVehiculo && i.IdConductorUsuario == driver.IdUsuario))
        {
            context.InspeccionesDiarias.Add(new InspeccionDiaria
            {
                IdVehiculo = bus101.IdVehiculo,
                IdConductorUsuario = driver.IdUsuario,
                FechaInspeccion = DateTime.UtcNow.AddDays(-1),
                Kilometraje = 124500,
                NivelCombustible = "ALTO",
                LimpiezaCabina = true,
                LimpiezaExterior = true,
                LucesOperativas = true,
                FrenosOperativos = true,
                NeumaticosOperativos = true,
                BocinaOperativa = true,
                EspejosOperativos = true,
                BotiquinDisponible = true,
                ExtintorDisponible = true,
                DocumentosCompletos = true,
                Resultado = "APROBADO",
                Observaciones = "Revision diaria sin observaciones."
            });
        }

        if (!await context.InspeccionesDiarias.AnyAsync(i => i.IdVehiculo == bus102.IdVehiculo && i.Resultado == "OBSERVADO"))
        {
            context.InspeccionesDiarias.Add(new InspeccionDiaria
            {
                IdVehiculo = bus102.IdVehiculo,
                IdConductorUsuario = driver.IdUsuario,
                FechaInspeccion = DateTime.UtcNow.AddHours(-6),
                Kilometraje = 143210,
                NivelCombustible = "MEDIO",
                LimpiezaCabina = true,
                LimpiezaExterior = true,
                LucesOperativas = true,
                FrenosOperativos = false,
                NeumaticosOperativos = true,
                BocinaOperativa = true,
                EspejosOperativos = true,
                BotiquinDisponible = true,
                ExtintorDisponible = true,
                DocumentosCompletos = true,
                Resultado = "OBSERVADO",
                Observaciones = "Se detecta respuesta irregular en freno posterior."
            });
        }

        await context.SaveChangesAsync();

        var observedInspection = await context.InspeccionesDiarias
            .Where(i => i.IdVehiculo == bus102.IdVehiculo)
            .OrderByDescending(i => i.FechaInspeccion)
            .FirstAsync();

        var incident = await context.IncidenciasMantenimiento
            .FirstOrDefaultAsync(i => i.Titulo == "Falla en sistema de frenos BUS-102");

        if (incident == null)
        {
            incident = new IncidenciaMantenimiento
            {
                IdVehiculo = bus102.IdVehiculo,
                IdReportadoPorUsuario = driver.IdUsuario,
                IdInspeccionDiaria = observedInspection.IdInspeccionDiaria,
                FechaReporte = DateTime.UtcNow.AddHours(-5),
                TipoIncidencia = "MECANICA",
                Severidad = "ALTA",
                Titulo = "Falla en sistema de frenos BUS-102",
                Descripcion = "Durante la inspeccion diaria se detecta frenado desigual en eje posterior.",
                Estado = "EN_REPARACION",
                RequiereParada = true,
                UbicacionReferencia = "Patio principal"
            };
            context.IncidenciasMantenimiento.Add(incident);
            await context.SaveChangesAsync();
        }

        var preventivePlan = await context.PlanesMantenimientoPreventivo
            .FirstOrDefaultAsync(p => p.IdVehiculo == bus103.IdVehiculo);

        if (preventivePlan == null)
        {
            preventivePlan = new PlanMantenimientoPreventivo
            {
                IdVehiculo = bus103.IdVehiculo,
                IdCreadoPorUsuario = chief.IdUsuario,
                FechaRegistro = DateTime.UtcNow.AddDays(-3),
                ProximaFechaProgramada = DateTime.UtcNow.AddDays(2),
                FrecuenciaDias = 30,
                FrecuenciaKilometros = 10000,
                Actividades = "Cambio de aceite, revision de filtros, verificacion electrica y ajuste de suspension.",
                Estado = "PROGRAMADO",
                Prioridad = "MEDIA",
                Observaciones = "Preventivo mensual de rutina.",
                UltimaEjecucion = DateTime.UtcNow.AddDays(-28)
            };
            context.PlanesMantenimientoPreventivo.Add(preventivePlan);
            await context.SaveChangesAsync();
        }

        var correctiveOrder = await EnsureOrderAsync(
            context,
            "OT-202607-0001",
            bus102.IdVehiculo,
            chief.IdUsuario,
            technician.IdUsuario,
            null,
            incident.IdIncidenciaMantenimiento,
            "CORRECTIVO",
            "ALTA",
            DateTime.UtcNow.AddHours(-4),
            "Diagnosticar y corregir falla reportada en sistema de frenos.",
            "Orden generada por incidencia de conductor.",
            "ASIGNADA");

        var preventiveOrder = await EnsureOrderAsync(
            context,
            "OT-202607-0002",
            bus103.IdVehiculo,
            chief.IdUsuario,
            technician.IdUsuario,
            preventivePlan.IdPlanMantenimientoPreventivo,
            null,
            "PREVENTIVO",
            "MEDIA",
            DateTime.UtcNow.AddDays(2),
            "Ejecutar mantenimiento preventivo mensual de BUS-103.",
            "Orden creada desde plan preventivo.",
            "GENERADA");

        var completedOrder = await EnsureOrderAsync(
            context,
            "OT-202606-0098",
            bus101.IdVehiculo,
            admin.IdUsuario,
            technician.IdUsuario,
            null,
            null,
            "CORRECTIVO",
            "MEDIA",
            DateTime.UtcNow.AddDays(-4),
            "Cambio de bateria y limpieza de terminales.",
            "Orden historica de referencia.",
            "FINALIZADA");

        if (!await context.MantenimientosEjecutados.AnyAsync(m => m.IdOrdenTrabajo == completedOrder.IdOrdenTrabajo))
        {
            var maintenance = new MantenimientoEjecutado
            {
                IdOrdenTrabajo = completedOrder.IdOrdenTrabajo,
                IdTecnicoUsuario = technician.IdUsuario,
                FechaInicio = DateTime.UtcNow.AddDays(-4).AddHours(1),
                FechaFin = DateTime.UtcNow.AddDays(-4).AddHours(3),
                TipoMantenimiento = "CORRECTIVO",
                Diagnostico = "Bateria sulfatada y terminales con desgaste.",
                AccionesRealizadas = "Se reemplaza bateria, se limpian bornes y se verifica carga del alternador.",
                Recomendaciones = "Controlar sistema electrico en proximo preventivo.",
                EstadoResultado = "COMPLETADO",
                NuevoEstadoOperativoVehiculo = "OPERATIVO"
            };

            context.MantenimientosEjecutados.Add(maintenance);
            await context.SaveChangesAsync();

            context.RepuestosUtilizados.Add(new RepuestoUtilizado
            {
                IdMantenimientoEjecutado = maintenance.IdMantenimientoEjecutado,
                CodigoRepuesto = "BAT-24V-120A",
                NombreRepuesto = "Bateria 24V 120A",
                Cantidad = 1,
                CostoUnitario = 850,
                Observaciones = "Instalada con garantia de 12 meses."
            });

            bus101.FechaUltimoMantenimiento = maintenance.FechaFin;
            bus101.EstadoOperativo = "OPERATIVO";
            await context.SaveChangesAsync();
        }

        bus101.FechaUltimaInspeccion = await context.InspeccionesDiarias
            .Where(i => i.IdVehiculo == bus101.IdVehiculo)
            .MaxAsync(i => (DateTime?)i.FechaInspeccion);
        bus102.FechaUltimaInspeccion = await context.InspeccionesDiarias
            .Where(i => i.IdVehiculo == bus102.IdVehiculo)
            .MaxAsync(i => (DateTime?)i.FechaInspeccion);
        bus102.EstadoOperativo = "EN_MANTENIMIENTO";
        bus102.ObservacionesMantenimiento = "Unidad inmovilizada por atencion correctiva en frenos.";
        bus103.ObservacionesMantenimiento = "Preventivo pendiente segun calendario.";

        if (correctiveOrder.Estado == "ASIGNADA")
        {
            bus102.ObservacionesMantenimiento = "Orden correctiva asignada al tecnico de mantenimiento.";
        }

        if (preventiveOrder.Estado == "GENERADA")
        {
            bus103.ObservacionesMantenimiento = "Orden preventiva generada y pendiente de ejecucion.";
        }

        await context.SaveChangesAsync();
    }

    private static async Task<OrdenTrabajo> EnsureOrderAsync(
        ApplicationDbContext context,
        string numeroOrden,
        int vehicleId,
        int generatedByUserId,
        int? technicianUserId,
        int? preventivePlanId,
        int? incidentId,
        string orderType,
        string priority,
        DateTime scheduledDate,
        string workRequested,
        string? observations,
        string status)
    {
        var order = await context.OrdenesTrabajo.FirstOrDefaultAsync(o => o.NumeroOrden == numeroOrden);
        if (order == null)
        {
            order = new OrdenTrabajo
            {
                NumeroOrden = numeroOrden,
                IdVehiculo = vehicleId,
                IdGeneradoPorUsuario = generatedByUserId,
                IdTecnicoUsuario = technicianUserId,
                IdPlanMantenimientoPreventivo = preventivePlanId,
                IdIncidenciaMantenimiento = incidentId,
                TipoOrden = orderType,
                Prioridad = priority,
                Estado = status,
                FechaGeneracion = DateTime.UtcNow,
                FechaProgramada = scheduledDate,
                FechaAsignacion = technicianUserId.HasValue ? DateTime.UtcNow : null,
                TrabajoSolicitado = workRequested,
                Observaciones = observations
            };
            context.OrdenesTrabajo.Add(order);
        }
        else
        {
            order.IdVehiculo = vehicleId;
            order.IdGeneradoPorUsuario = generatedByUserId;
            order.IdTecnicoUsuario = technicianUserId;
            order.IdPlanMantenimientoPreventivo = preventivePlanId;
            order.IdIncidenciaMantenimiento = incidentId;
            order.TipoOrden = orderType;
            order.Prioridad = priority;
            order.Estado = status;
            order.FechaProgramada = scheduledDate;
            order.FechaAsignacion = technicianUserId.HasValue ? order.FechaAsignacion ?? DateTime.UtcNow : null;
            order.TrabajoSolicitado = workRequested;
            order.Observaciones = observations;
        }

        if (status == "FINALIZADA" && order.FechaInicio == null)
        {
            order.FechaInicio = scheduledDate.AddHours(1);
            order.FechaFin = scheduledDate.AddHours(3);
        }

        await context.SaveChangesAsync();
        return order;
    }

    private static string ComputeHash(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        var builder = new StringBuilder();
        foreach (var b in bytes)
        {
            builder.Append(b.ToString("x2"));
        }

        return builder.ToString();
    }
}
