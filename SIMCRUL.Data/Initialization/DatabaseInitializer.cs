using Microsoft.EntityFrameworkCore;
using SIMCRUL.Data.Context;
using SIMCRUL.Entity;
using System.Security.Cryptography;
using System.Text;

namespace SIMCRUL.Data.Initialization;

public static class DatabaseInitializer
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Ensure the database and schema are created
        await context.Database.EnsureCreatedAsync();
        await EnsureCompatibilitySchemaAsync(context);
        await EnsureOperationalDashboardProceduresAsync(context);
        await EnsureElChinoRouteAsync(context);

        // 1. Seed Roles
        var requiredRoles = new[] { "Administrador", "Supervisor", "Operador", "Conductor", "Pasajero" };
        foreach (var roleName in requiredRoles)
        {
            var exists = await context.Roles.AnyAsync(r => r.Nombre == roleName);
            if (!exists)
            {
                context.Roles.Add(new Rol
                {
                    Nombre = roleName,
                    Descripcion = $"Rol de {roleName} para control de accesos",
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                });
            }
        }
        await context.SaveChangesAsync();

        // 2. Seed Default Admin User if none exists
        var adminRole = await context.Roles.FirstAsync(r => r.Nombre == "Administrador");
        var hasAdmin = await context.Usuarios.AnyAsync(u => u.IdRol == adminRole.IdRol);
        if (!hasAdmin)
        {
            var adminUser = new Usuario
            {
                Username = "admin",
                Email = "admin@simcrul.com",
                Nombres = "Administrador",
                Apellidos = "Principal",
                Telefono = "999999999",
                PasswordHash = ComputeHash("admin123456"),
                IdRol = adminRole.IdRol,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };
            context.Usuarios.Add(adminUser);
            await context.SaveChangesAsync();
        }

        var conductorRole = await context.Roles.FirstAsync(r => r.Nombre == "Conductor");
        var hasDriverUser = await context.Usuarios.AnyAsync(u => u.IdRol == conductorRole.IdRol);
        if (!hasDriverUser)
        {
            context.Usuarios.Add(new Usuario
            {
                Username = "conductor.demo",
                Email = "conductor@simcrul.com",
                Nombres = "Conductor",
                Apellidos = "Demo",
                Telefono = "900000001",
                PasswordHash = ComputeHash("conductor123"),
                IdRol = conductorRole.IdRol,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            });

            await context.SaveChangesAsync();
        }

        // 3. Seed Demo Transport Company
        var hasCompany = await context.EmpresasTransporte.AnyAsync();
        if (!hasCompany)
        {
            var company = new EmpresaTransporte
            {
                Ruc = "20123456789",
                RazonSocial = "Consorcio de Transportes Lima Sur S.A.",
                NombreComercial = "El Rápido Express",
                Direccion = "Av. Alfredo Mendiola 1420, Los Olivos",
                Telefono = "01-5334455",
                Email = "contacto@elrapido.com",
                Estado = true,
                FechaRegistro = DateTime.UtcNow
            };
            context.EmpresasTransporte.Add(company);
            await context.SaveChangesAsync();

            // 4. Seed Demo Route
            var route = new Ruta
            {
                IdEmpresa = company.IdEmpresa,
                CodigoRuta = "R01",
                NombreRuta = "Línea Metropolitana Sur-Norte (SJL - Chorrillos)",
                Origen = "San Juan de Lurigancho",
                Destino = "Chorrillos",
                DistanciaKm = 22.5M,
                TiempoEstimadoMin = 75,
                VelocidadMaximaKmh = 60,
                Activa = true,
                FechaRegistro = DateTime.UtcNow
            };
            context.Rutas.Add(route);
            await context.SaveChangesAsync();

            // 5. Seed Route Control Points (Lima coordinates path)
            var p1 = new RutaPuntoControl { IdRuta = route.IdRuta, Orden = 1, Descripcion = "Inicio SJL", Latitud = -12.015000M, Longitud = -77.012000M, RadioToleranciaMetros = 150 };
            var p2 = new RutaPuntoControl { IdRuta = route.IdRuta, Orden = 2, Descripcion = "Paradero Abancay", Latitud = -12.046000M, Longitud = -77.030000M, RadioToleranciaMetros = 150 };
            var p3 = new RutaPuntoControl { IdRuta = route.IdRuta, Orden = 3, Descripcion = "Paradero Javier Prado", Latitud = -12.088000M, Longitud = -77.023000M, RadioToleranciaMetros = 150 };
            var p4 = new RutaPuntoControl { IdRuta = route.IdRuta, Orden = 4, Descripcion = "Paradero Angamos", Latitud = -12.115000M, Longitud = -77.025000M, RadioToleranciaMetros = 150 };
            var p5 = new RutaPuntoControl { IdRuta = route.IdRuta, Orden = 5, Descripcion = "Término Chorrillos", Latitud = -12.155000M, Longitud = -77.022000M, RadioToleranciaMetros = 150 };

            context.RutaPuntosControl.AddRange(p1, p2, p3, p4, p5);

            // 6. Seed Paraderos
            var par1 = new Paradero { Nombre = "Estación Bayóvar", DireccionReferencia = "Av. Próceres de la Independencia", Latitud = -12.015000M, Longitud = -77.012000M, Distrito = "San Juan de Lurigancho", Activo = true };
            var par2 = new Paradero { Nombre = "Paradero Abancay", DireccionReferencia = "Cruce Av. Abancay y Nicolás de Piérola", Latitud = -12.046000M, Longitud = -77.030000M, Distrito = "Lima Centro", Activo = true };
            var par3 = new Paradero { Nombre = "Paradero Javier Prado", DireccionReferencia = "Av. Paseo de la República", Latitud = -12.088000M, Longitud = -77.023000M, Distrito = "La Victoria", Activo = true };
            var par4 = new Paradero { Nombre = "Paradero Angamos", DireccionReferencia = "Cruce Av. Angamos Este", Latitud = -12.115000M, Longitud = -77.025000M, Distrito = "Surquillo", Activo = true };
            var par5 = new Paradero { Nombre = "Paradero Terminal Chorrillos", DireccionReferencia = "Av. Huaylas cuadra 12", Latitud = -12.155000M, Longitud = -77.022000M, Distrito = "Chorrillos", Activo = true };

            context.Paraderos.AddRange(par1, par2, par3, par4, par5);
            await context.SaveChangesAsync();

            // Link Paraderos to Route
            context.RutaParaderos.AddRange(
                new RutaParadero { IdRuta = route.IdRuta, IdParadero = par1.IdParadero, Orden = 1, TiempoEstimadoDesdeInicioMin = 0, Activo = true },
                new RutaParadero { IdRuta = route.IdRuta, IdParadero = par2.IdParadero, Orden = 2, TiempoEstimadoDesdeInicioMin = 18, Activo = true },
                new RutaParadero { IdRuta = route.IdRuta, IdParadero = par3.IdParadero, Orden = 3, TiempoEstimadoDesdeInicioMin = 36, Activo = true },
                new RutaParadero { IdRuta = route.IdRuta, IdParadero = par4.IdParadero, Orden = 4, TiempoEstimadoDesdeInicioMin = 54, Activo = true },
                new RutaParadero { IdRuta = route.IdRuta, IdParadero = par5.IdParadero, Orden = 5, TiempoEstimadoDesdeInicioMin = 75, Activo = true }
            );

            // 7. Seed Vehicle
            var vehicle = new Vehiculo
            {
                IdEmpresa = company.IdEmpresa,
                Placa = "F3V-894",
                CodigoInterno = "V-102",
                TipoVehiculo = "CUSTER",
                Marca = "Toyota",
                Modelo = "Coaster",
                Anio = 2021,
                CapacidadPasajeros = 25,
                VelocidadMaximaKmh = 90,
                Estado = true,
                FechaRegistro = DateTime.UtcNow
            };
            context.Vehiculos.Add(vehicle);
            await context.SaveChangesAsync();

            // 8. Seed GPS Device (default simulator IMEI)
            var device = new DispositivoGps
            {
                IdVehiculo = vehicle.IdVehiculo,
                Imei = "IMEI88847294829",
                NumeroSerie = "SN-GPS-102A",
                Proveedor = "TrackerPeru",
                FechaInstalacion = DateTime.UtcNow,
                Estado = true
            };
            context.DispositivosGps.Add(device);

            // 9. Seed Driver
            var driver = new Conductor
            {
                IdEmpresa = company.IdEmpresa,
                Nombres = "Carlos Daniel",
                Apellidos = "Mendoza Flores",
                Dni = "72849284",
                NumeroLicencia = "Q72849284",
                CategoriaLicencia = "A-IIIa",
                FechaVencimientoLicencia = DateTime.Today.AddYears(3),
                Telefono = "988776655",
                Estado = true,
                FechaRegistro = DateTime.UtcNow
            };
            context.Conductores.Add(driver);
            await context.SaveChangesAsync();

            // 10. Seed Operation Assignment for Today
            var assignment = new AsignacionOperacion
            {
                IdRuta = route.IdRuta,
                IdVehiculo = vehicle.IdVehiculo,
                IdConductor = driver.IdConductor,
                FechaInicioProgramada = DateTime.Today,
                FechaFinProgramada = DateTime.Today.AddDays(1).AddSeconds(-1),
                Turno = "MAÑANA",
                Estado = "ACTIVA",
                Observaciones = "Sembrado de Datos Dinámicos para Sustentación"
            };
            context.AsignacionesOperacion.Add(assignment);
            await context.SaveChangesAsync();
        }
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

    private static async Task EnsureCompatibilitySchemaAsync(ApplicationDbContext context)
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
                    created_at_utc DATETIME2 NOT NULL CONSTRAINT DF_PASSWORD_RESET_TOKENS_CREATED DEFAULT SYSUTCDATETIME(),
                    requested_by_ip NVARCHAR(64) NULL,
                    email_sent_to NVARCHAR(200) NOT NULL,
                    CONSTRAINT FK_PASSWORD_RESET_TOKENS_USUARIOS FOREIGN KEY (id_usuario) REFERENCES USUARIOS(id_usuario)
                );

                CREATE INDEX IX_PASSWORD_RESET_TOKENS_ID_USUARIO ON PASSWORD_RESET_TOKENS(id_usuario);
                CREATE INDEX IX_PASSWORD_RESET_TOKENS_TOKEN_HASH ON PASSWORD_RESET_TOKENS(token_hash);
            END

            IF OBJECT_ID('SOLICITUDES_PASAJERO', 'U') IS NULL
            BEGIN
                CREATE TABLE SOLICITUDES_PASAJERO
                (
                    id_solicitud_pasajero INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    id_usuario INT NOT NULL,
                    id_ruta INT NULL,
                    tipo_solicitud NVARCHAR(30) NOT NULL,
                    asunto NVARCHAR(150) NOT NULL,
                    descripcion NVARCHAR(2000) NOT NULL,
                    estado NVARCHAR(30) NOT NULL CONSTRAINT DF_SOLICITUDES_PASAJERO_ESTADO DEFAULT 'PENDIENTE',
                    fecha_registro DATETIME2 NOT NULL CONSTRAINT DF_SOLICITUDES_PASAJERO_FECHA DEFAULT SYSUTCDATETIME(),
                    email_contacto NVARCHAR(200) NULL,
                    telefono_contacto NVARCHAR(30) NULL,
                    respuesta NVARCHAR(2000) NULL,
                    fecha_respuesta DATETIME2 NULL,
                    CONSTRAINT FK_SOLICITUDES_PASAJERO_USUARIOS FOREIGN KEY (id_usuario) REFERENCES USUARIOS(id_usuario),
                    CONSTRAINT FK_SOLICITUDES_PASAJERO_RUTAS FOREIGN KEY (id_ruta) REFERENCES RUTAS(id_ruta)
                );

                CREATE INDEX IX_SOLICITUDES_PASAJERO_ID_USUARIO ON SOLICITUDES_PASAJERO(id_usuario);
                CREATE INDEX IX_SOLICITUDES_PASAJERO_ID_RUTA ON SOLICITUDES_PASAJERO(id_ruta);
            END
            """;

        await context.Database.ExecuteSqlRawAsync(sql);
    }

    private static async Task EnsureOperationalDashboardProceduresAsync(ApplicationDbContext context)
    {
        const string summaryProcedure = """
            CREATE OR ALTER PROCEDURE dbo.SP_DASHBOARD_OPERATIVO_RESUMEN
                @FechaDesde DATE,
                @FechaHasta DATE,
                @IdRuta INT = NULL
            AS
            BEGIN
                SET NOCOUNT ON;

                DECLARE @Inicio DATETIME2 = CAST(@FechaDesde AS DATETIME2);
                DECLARE @Fin DATETIME2 = DATEADD(DAY, 1, CAST(@FechaHasta AS DATETIME2));

                CREATE TABLE #AlertasFiltradas
                (
                    codigo NVARCHAR(50) NOT NULL,
                    fecha_alerta DATETIME2 NOT NULL
                );

                INSERT INTO #AlertasFiltradas (codigo, fecha_alerta)
                SELECT ta.codigo, a.fecha_alerta
                FROM ALERTAS a
                INNER JOIN TIPOS_ALERTA ta ON ta.id_tipo_alerta = a.id_tipo_alerta
                LEFT JOIN VIAJES vi ON vi.id_viaje = a.id_viaje
                LEFT JOIN ASIGNACIONES_OPERACION ao ON ao.id_asignacion = vi.id_asignacion
                WHERE a.fecha_alerta >= @Inicio
                  AND a.fecha_alerta < @Fin
                  AND (@IdRuta IS NULL OR ao.id_ruta = @IdRuta);

                ;WITH Lecturas AS
                (
                    SELECT
                        gl.id_lectura,
                        CAST(gl.fecha_gps AS DATE) AS fecha,
                        gl.fecha_gps,
                        gl.id_vehiculo,
                        v.placa,
                        v.codigo_interno,
                        et.razon_social AS empresa,
                        r.codigo_ruta,
                        r.nombre_ruta,
                        ao.id_ruta,
                        CAST(gl.latitud AS FLOAT) AS latitud,
                        CAST(gl.longitud AS FLOAT) AS longitud,
                        LAG(CAST(gl.latitud AS FLOAT)) OVER (PARTITION BY gl.id_vehiculo, CAST(gl.fecha_gps AS DATE) ORDER BY gl.fecha_gps, gl.id_lectura) AS prev_latitud,
                        LAG(CAST(gl.longitud AS FLOAT)) OVER (PARTITION BY gl.id_vehiculo, CAST(gl.fecha_gps AS DATE) ORDER BY gl.fecha_gps, gl.id_lectura) AS prev_longitud
                    FROM GPS_LECTURAS gl
                    INNER JOIN VEHICULOS v ON v.id_vehiculo = gl.id_vehiculo
                    INNER JOIN EMPRESAS_TRANSPORTE et ON et.id_empresa = v.id_empresa
                    LEFT JOIN VIAJES vi ON vi.id_viaje = gl.id_viaje
                    LEFT JOIN ASIGNACIONES_OPERACION ao ON ao.id_asignacion = vi.id_asignacion
                    LEFT JOIN RUTAS r ON r.id_ruta = ao.id_ruta
                    WHERE gl.fecha_gps >= @Inicio
                      AND gl.fecha_gps < @Fin
                      AND (@IdRuta IS NULL OR ao.id_ruta = @IdRuta)
                ),
                Distancias AS
                (
                    SELECT
                        fecha,
                        id_vehiculo,
                        placa,
                        codigo_interno,
                        empresa,
                        codigo_ruta,
                        nombre_ruta,
                        CASE
                            WHEN prev_latitud IS NULL OR prev_longitud IS NULL THEN 0
                            ELSE geography::Point(latitud, longitud, 4326).STDistance(geography::Point(prev_latitud, prev_longitud, 4326)) / 1000.0
                        END AS distancia_km
                    FROM Lecturas
                ),
                DistanciaAgrupada AS
                (
                    SELECT
                        empresa,
                        placa,
                        codigo_interno,
                        CONCAT(ISNULL(codigo_ruta, 'S/R'), ' - ', ISNULL(nombre_ruta, 'Sin ruta')) AS ruta,
                        SUM(distancia_km) AS distancia_km
                    FROM Distancias
                    GROUP BY empresa, placa, codigo_interno, codigo_ruta, nombre_ruta
                ),
                Tiempo AS
                (
                    SELECT
                        SUM(DATEDIFF(MINUTE, vi.fecha_inicio_real, ISNULL(vi.fecha_fin_real, SYSUTCDATETIME()))) AS tiempo_min
                    FROM VIAJES vi
                    INNER JOIN ASIGNACIONES_OPERACION ao ON ao.id_asignacion = vi.id_asignacion
                    WHERE vi.fecha_inicio_real >= @Inicio
                      AND vi.fecha_inicio_real < @Fin
                      AND (@IdRuta IS NULL OR ao.id_ruta = @IdRuta)
                )
                SELECT
                    CAST(ISNULL((SELECT SUM(distancia_km) FROM Distancias), 0) AS DECIMAL(18,2)) AS DistanciaRecorridaKm,
                    CAST(ISNULL((SELECT tiempo_min FROM Tiempo), 0) AS INT) AS TiempoOperativoMin,
                    CAST((SELECT COUNT(1) FROM #AlertasFiltradas WHERE codigo = 'VELOCIDAD') AS INT) AS ExcesosVelocidad,
                    CAST((SELECT COUNT(1) FROM #AlertasFiltradas WHERE codigo = 'DESVIO_RUTA') AS INT) AS DesviosRuta,
                    CAST(0 AS INT) AS ReclamosRecibidos,
                    ISNULL((SELECT TOP 1 empresa FROM DistanciaAgrupada ORDER BY distancia_km DESC), 'Sin datos') AS TopEmpresa,
                    ISNULL((SELECT TOP 1 placa FROM DistanciaAgrupada ORDER BY distancia_km DESC), 'Sin datos') AS TopVehiculo,
                    ISNULL((SELECT TOP 1 ruta FROM DistanciaAgrupada ORDER BY distancia_km DESC), 'Sin datos') AS TopRuta,
                    CAST(ISNULL((SELECT TOP 1 distancia_km FROM DistanciaAgrupada ORDER BY distancia_km DESC), 0) AS DECIMAL(18,2)) AS TopDistanciaKm;

                SELECT
                    FORMAT(CAST(fecha_alerta AS DATE), 'dd/MM') AS Label,
                    CAST(COUNT(1) AS DECIMAL(18,2)) AS Value
                FROM #AlertasFiltradas
                WHERE codigo = 'VELOCIDAD'
                GROUP BY CAST(fecha_alerta AS DATE)
                ORDER BY CAST(fecha_alerta AS DATE);

                SELECT
                    FORMAT(DATEPART(HOUR, gl.fecha_gps), '00') AS Label,
                    CAST(COUNT(DISTINCT gl.id_vehiculo) AS DECIMAL(18,2)) AS Value
                FROM GPS_LECTURAS gl
                LEFT JOIN VIAJES vi ON vi.id_viaje = gl.id_viaje
                LEFT JOIN ASIGNACIONES_OPERACION ao ON ao.id_asignacion = vi.id_asignacion
                WHERE gl.fecha_gps >= @Inicio
                  AND gl.fecha_gps < @Fin
                  AND (@IdRuta IS NULL OR ao.id_ruta = @IdRuta)
                GROUP BY DATEPART(HOUR, gl.fecha_gps)
                ORDER BY DATEPART(HOUR, gl.fecha_gps);
            END
            """;

        const string detailProcedure = """
            CREATE OR ALTER PROCEDURE dbo.SP_DASHBOARD_OPERATIVO_DETALLE
                @Tipo NVARCHAR(30),
                @FechaDesde DATE,
                @FechaHasta DATE,
                @IdRuta INT = NULL
            AS
            BEGIN
                SET NOCOUNT ON;

                DECLARE @Inicio DATETIME2 = CAST(@FechaDesde AS DATETIME2);
                DECLARE @Fin DATETIME2 = DATEADD(DAY, 1, CAST(@FechaHasta AS DATETIME2));

                IF @Tipo = 'distancia'
                BEGIN
                    ;WITH Lecturas AS
                    (
                        SELECT
                            CAST(gl.fecha_gps AS DATE) AS fecha,
                            gl.fecha_gps,
                            gl.id_lectura,
                            gl.id_vehiculo,
                            et.razon_social AS empresa,
                            v.placa,
                            v.codigo_interno,
                            CONCAT(ISNULL(r.codigo_ruta, 'S/R'), ' - ', ISNULL(r.nombre_ruta, 'Sin ruta')) AS ruta,
                            CAST(gl.latitud AS FLOAT) AS latitud,
                            CAST(gl.longitud AS FLOAT) AS longitud,
                            LAG(CAST(gl.latitud AS FLOAT)) OVER (PARTITION BY gl.id_vehiculo, CAST(gl.fecha_gps AS DATE) ORDER BY gl.fecha_gps, gl.id_lectura) AS prev_latitud,
                            LAG(CAST(gl.longitud AS FLOAT)) OVER (PARTITION BY gl.id_vehiculo, CAST(gl.fecha_gps AS DATE) ORDER BY gl.fecha_gps, gl.id_lectura) AS prev_longitud
                        FROM GPS_LECTURAS gl
                        INNER JOIN VEHICULOS v ON v.id_vehiculo = gl.id_vehiculo
                        INNER JOIN EMPRESAS_TRANSPORTE et ON et.id_empresa = v.id_empresa
                        LEFT JOIN VIAJES vi ON vi.id_viaje = gl.id_viaje
                        LEFT JOIN ASIGNACIONES_OPERACION ao ON ao.id_asignacion = vi.id_asignacion
                        LEFT JOIN RUTAS r ON r.id_ruta = ao.id_ruta
                        WHERE gl.fecha_gps >= @Inicio
                          AND gl.fecha_gps < @Fin
                          AND (@IdRuta IS NULL OR ao.id_ruta = @IdRuta)
                    )
                    SELECT
                        fecha AS Fecha,
                        empresa AS Empresa,
                        placa AS Placa,
                        codigo_interno AS CodigoVehiculo,
                        ruta AS Ruta,
                        CAST(SUM(CASE WHEN prev_latitud IS NULL OR prev_longitud IS NULL THEN 0 ELSE geography::Point(latitud, longitud, 4326).STDistance(geography::Point(prev_latitud, prev_longitud, 4326)) / 1000.0 END) AS DECIMAL(18,2)) AS DistanciaKm,
                        CAST(0 AS INT) AS TiempoOperativoMin,
                        CAST(COUNT(1) AS INT) AS Eventos,
                        CAST(NULL AS DECIMAL(18,2)) AS ValorMaximo,
                        CAST('Recorrido GPS registrado' AS NVARCHAR(400)) AS Descripcion,
                        CAST('REGISTRADO' AS NVARCHAR(30)) AS Estado
                    FROM Lecturas
                    GROUP BY fecha, empresa, placa, codigo_interno, ruta
                    ORDER BY Fecha DESC, DistanciaKm DESC;
                    RETURN;
                END

                IF @Tipo = 'tiempo'
                BEGIN
                    SELECT
                        CAST(vi.fecha_inicio_real AS DATE) AS Fecha,
                        et.razon_social AS Empresa,
                        v.placa AS Placa,
                        v.codigo_interno AS CodigoVehiculo,
                        CONCAT(r.codigo_ruta, ' - ', r.nombre_ruta) AS Ruta,
                        CAST(0 AS DECIMAL(18,2)) AS DistanciaKm,
                        CAST(DATEDIFF(MINUTE, vi.fecha_inicio_real, ISNULL(vi.fecha_fin_real, SYSUTCDATETIME())) AS INT) AS TiempoOperativoMin,
                        CAST(1 AS INT) AS Eventos,
                        CAST(NULL AS DECIMAL(18,2)) AS ValorMaximo,
                        CAST(CONCAT('Inicio: ', CONVERT(VARCHAR(16), vi.fecha_inicio_real, 120), ' / Fin: ', ISNULL(CONVERT(VARCHAR(16), vi.fecha_fin_real, 120), 'En progreso')) AS NVARCHAR(400)) AS Descripcion,
                        vi.estado AS Estado
                    FROM VIAJES vi
                    INNER JOIN ASIGNACIONES_OPERACION ao ON ao.id_asignacion = vi.id_asignacion
                    INNER JOIN VEHICULOS v ON v.id_vehiculo = ao.id_vehiculo
                    INNER JOIN EMPRESAS_TRANSPORTE et ON et.id_empresa = v.id_empresa
                    INNER JOIN RUTAS r ON r.id_ruta = ao.id_ruta
                    WHERE vi.fecha_inicio_real >= @Inicio
                      AND vi.fecha_inicio_real < @Fin
                      AND (@IdRuta IS NULL OR ao.id_ruta = @IdRuta)
                    ORDER BY vi.fecha_inicio_real DESC;
                    RETURN;
                END

                SELECT
                    CAST(a.fecha_alerta AS DATE) AS Fecha,
                    et.razon_social AS Empresa,
                    v.placa AS Placa,
                    v.codigo_interno AS CodigoVehiculo,
                    CONCAT(ISNULL(r.codigo_ruta, 'S/R'), ' - ', ISNULL(r.nombre_ruta, 'Sin ruta')) AS Ruta,
                    CAST(0 AS DECIMAL(18,2)) AS DistanciaKm,
                    CAST(0 AS INT) AS TiempoOperativoMin,
                    CAST(1 AS INT) AS Eventos,
                    CAST(a.valor_detectado AS DECIMAL(18,2)) AS ValorMaximo,
                    a.descripcion AS Descripcion,
                    a.estado AS Estado
                FROM ALERTAS a
                INNER JOIN TIPOS_ALERTA ta ON ta.id_tipo_alerta = a.id_tipo_alerta
                INNER JOIN VEHICULOS v ON v.id_vehiculo = a.id_vehiculo
                INNER JOIN EMPRESAS_TRANSPORTE et ON et.id_empresa = v.id_empresa
                LEFT JOIN VIAJES vi ON vi.id_viaje = a.id_viaje
                LEFT JOIN ASIGNACIONES_OPERACION ao ON ao.id_asignacion = vi.id_asignacion
                LEFT JOIN RUTAS r ON r.id_ruta = ao.id_ruta
                WHERE a.fecha_alerta >= @Inicio
                  AND a.fecha_alerta < @Fin
                  AND (@IdRuta IS NULL OR ao.id_ruta = @IdRuta)
                  AND ((@Tipo = 'velocidad' AND ta.codigo = 'VELOCIDAD') OR (@Tipo = 'desvio' AND ta.codigo = 'DESVIO_RUTA'))
                ORDER BY a.fecha_alerta DESC;
            END
            """;

        await context.Database.ExecuteSqlRawAsync(summaryProcedure);
        await context.Database.ExecuteSqlRawAsync(detailProcedure);
    }

    private static async Task EnsureElChinoRouteAsync(ApplicationDbContext context)
    {
        const string routeCode = "8108-C";

        var company = await context.EmpresasTransporte
            .FirstOrDefaultAsync(e => e.Ruc == "20581081080");

        if (company == null)
        {
            company = new EmpresaTransporte
            {
                Ruc = "20581081080",
                RazonSocial = "El Condor S.A.",
                NombreComercial = "Los Chinos",
                Direccion = "Ruta Villa El Salvador - Puente Piedra",
                Telefono = "980793614",
                Email = "operaciones@loschinos.local",
                Estado = true,
                FechaRegistro = DateTime.UtcNow
            };
            context.EmpresasTransporte.Add(company);
            await context.SaveChangesAsync();
        }

        var route = await context.Rutas.FirstOrDefaultAsync(r => r.CodigoRuta == routeCode);
        if (route == null)
        {
            route = new Ruta
            {
                IdEmpresa = company.IdEmpresa,
                CodigoRuta = routeCode,
                NombreRuta = "El Chino C - Villa El Salvador - Puente Piedra",
                Origen = "Av. Lima, Villa El Salvador",
                Destino = "Valle Hermoso - Lomas, Puente Piedra",
                DistanciaKm = 62.8M,
                TiempoEstimadoMin = 172,
                VelocidadMaximaKmh = 60,
                Activa = true,
                FechaRegistro = DateTime.UtcNow
            };
            context.Rutas.Add(route);
            await context.SaveChangesAsync();
        }

        if (!await context.RutaPuntosControl.AnyAsync(p => p.IdRuta == route.IdRuta))
        {
            var controlPoints = new[]
            {
                ("Los Chinos - Av. Lima", -12.227500M, -76.943500M),
                ("Separadora Industrial", -12.218700M, -76.950500M),
                ("Ovalo Oasis / 200 Millas", -12.207000M, -76.939500M),
                ("Pastor Sevilla", -12.196600M, -76.948900M),
                ("Proceres / Mall del Sur", -12.165800M, -76.970600M),
                ("Atocongo", -12.148900M, -76.982600M),
                ("Benavides", -12.126300M, -76.992200M),
                ("Primavera / Pentagonito", -12.104900M, -76.981700M),
                ("Javier Prado", -12.087200M, -76.978100M),
                ("Puente Santa Anita", -12.049000M, -76.958000M),
                ("Puente Nuevo", -12.035300M, -76.947400M),
                ("Acho", -12.041900M, -77.023400M),
                ("Caqueta", -12.028700M, -77.049000M),
                ("Habich", -12.015300M, -77.053000M),
                ("Fiori / Plaza Norte", -12.006200M, -77.059600M),
                ("Mega Plaza", -11.994400M, -77.061000M),
                ("Panamericana Norte / Pro", -11.962900M, -77.072000M),
                ("Trapiche", -11.925700M, -77.076300M),
                ("Puente Piedra", -11.863900M, -77.076100M),
                ("Ovalo Zapallal", -11.825900M, -77.088300M),
                ("Valle Hermoso - Lomas", -11.807700M, -77.095000M)
            };

            var order = 1;
            context.RutaPuntosControl.AddRange(controlPoints.Select(point => new RutaPuntoControl
            {
                IdRuta = route.IdRuta,
                Orden = order++,
                Descripcion = point.Item1,
                Latitud = point.Item2,
                Longitud = point.Item3,
                RadioToleranciaMetros = 300,
                EsParadero = true
            }));
            await context.SaveChangesAsync();
        }

        if (!await context.RutaParaderos.AnyAsync(rp => rp.IdRuta == route.IdRuta))
        {
            var stops = new[]
            {
                ("Los Chinos", "Av. Lima", "Villa El Salvador", -12.227500M, -76.943500M, 0),
                ("Ovalo Oasis & 200 Millas", "Av. 200 Millas", "Villa El Salvador", -12.207000M, -76.939500M, 14),
                ("Proceres / Mall del Sur", "Av. Los Heroes", "San Juan de Miraflores", -12.165800M, -76.970600M, 35),
                ("Atocongo", "Panamericana Sur", "San Juan de Miraflores", -12.148900M, -76.982600M, 44),
                ("Benavides", "Av. Benavides", "Santiago de Surco", -12.126300M, -76.992200M, 58),
                ("Javier Prado", "Av. Javier Prado", "La Molina", -12.087200M, -76.978100M, 75),
                ("Puente Santa Anita", "Carretera Central", "Santa Anita", -12.049000M, -76.958000M, 92),
                ("Acho", "Via de Evitamiento", "Rimac", -12.041900M, -77.023400M, 108),
                ("Caqueta", "Panamericana Norte", "San Martin de Porres", -12.028700M, -77.049000M, 118),
                ("Fiori / Plaza Norte", "Panamericana Norte", "Independencia", -12.006200M, -77.059600M, 128),
                ("Mega Plaza", "Panamericana Norte", "Independencia", -11.994400M, -77.061000M, 134),
                ("Trapiche", "Panamericana Norte", "Comas", -11.925700M, -77.076300M, 150),
                ("Ovalo Zapallal", "Panamericana Norte", "Puente Piedra", -11.825900M, -77.088300M, 166),
                ("Valle Hermoso - Lomas", "Valle Hermoso", "Puente Piedra", -11.807700M, -77.095000M, 172)
            };

            var order = 1;
            foreach (var stop in stops)
            {
                var paradero = await FindOrCreateParaderoAsync(
                    context,
                    stop.Item1,
                    stop.Item2,
                    stop.Item3,
                    stop.Item4,
                    stop.Item5);

                context.RutaParaderos.Add(new RutaParadero
                {
                    IdRuta = route.IdRuta,
                    IdParadero = paradero.IdParadero,
                    Orden = order++,
                    TiempoEstimadoDesdeInicioMin = stop.Item6,
                    Activo = true
                });
            }
            await context.SaveChangesAsync();
        }

        var drivers = new[]
        {
            ("Miguel Angel", "Quispe Huaman", "45892173", "Q45892173", "987654321"),
            ("Jose Luis", "Ramos Paredes", "47123698", "Q47123698", "986123457"),
            ("Edwin Raul", "Soto Cardenas", "43678125", "Q43678125", "984567123")
        };

        foreach (var driverSeed in drivers)
        {
            if (!await context.Conductores.AnyAsync(c => c.Dni == driverSeed.Item3))
            {
                context.Conductores.Add(new Conductor
                {
                    IdEmpresa = company.IdEmpresa,
                    Nombres = driverSeed.Item1,
                    Apellidos = driverSeed.Item2,
                    Dni = driverSeed.Item3,
                    NumeroLicencia = driverSeed.Item4,
                    CategoriaLicencia = "A-IIIa",
                    FechaVencimientoLicencia = DateTime.Today.AddYears(3),
                    Telefono = driverSeed.Item5,
                    Estado = true,
                    FechaRegistro = DateTime.UtcNow
                });
            }
        }
        await context.SaveChangesAsync();

        var vehicles = new[]
        {
            ("CHI-108", "CH-C01", "IMEI81080001"),
            ("CHI-218", "CH-C02", "IMEI81080002"),
            ("CHI-326", "CH-C03", "IMEI81080003")
        };

        foreach (var vehicleSeed in vehicles)
        {
            var vehicle = await context.Vehiculos.FirstOrDefaultAsync(v => v.Placa == vehicleSeed.Item1);
            if (vehicle == null)
            {
                vehicle = new Vehiculo
                {
                    IdEmpresa = company.IdEmpresa,
                    Placa = vehicleSeed.Item1,
                    CodigoInterno = vehicleSeed.Item2,
                    TipoVehiculo = "BUS",
                    Marca = "Mercedes-Benz",
                    Modelo = "OF-1721",
                    Anio = 2022,
                    CapacidadPasajeros = 80,
                    VelocidadMaximaKmh = 90,
                    Estado = true,
                    FechaRegistro = DateTime.UtcNow
                };
                context.Vehiculos.Add(vehicle);
                await context.SaveChangesAsync();
            }

            if (!await context.DispositivosGps.AnyAsync(d => d.Imei == vehicleSeed.Item3))
            {
                context.DispositivosGps.Add(new DispositivoGps
                {
                    IdVehiculo = vehicle.IdVehiculo,
                    Imei = vehicleSeed.Item3,
                    NumeroSerie = $"SN-{vehicleSeed.Item3}",
                    Proveedor = "TrackerPeru",
                    FechaInstalacion = DateTime.UtcNow,
                    Estado = true
                });
            }
        }
        await context.SaveChangesAsync();

        var routeDrivers = await context.Conductores
            .Where(c => c.IdEmpresa == company.IdEmpresa)
            .OrderBy(c => c.IdConductor)
            .Take(3)
            .ToListAsync();

        var routeVehicles = await context.Vehiculos
            .Where(v => v.IdEmpresa == company.IdEmpresa && v.CodigoInterno.StartsWith("CH-C"))
            .OrderBy(v => v.IdVehiculo)
            .ToListAsync();

        for (var i = 0; i < Math.Min(routeDrivers.Count, routeVehicles.Count); i++)
        {
            var vehicle = routeVehicles[i];
            var driver = routeDrivers[i];
            var existsAssignment = await context.AsignacionesOperacion.AnyAsync(a =>
                a.IdRuta == route.IdRuta &&
                a.IdVehiculo == vehicle.IdVehiculo &&
                a.Estado == "ACTIVA");

            if (!existsAssignment)
            {
                context.AsignacionesOperacion.Add(new AsignacionOperacion
                {
                    IdRuta = route.IdRuta,
                    IdVehiculo = vehicle.IdVehiculo,
                    IdConductor = driver.IdConductor,
                    FechaInicioProgramada = DateTime.Today.AddHours(4),
                    FechaFinProgramada = DateTime.Today.AddHours(21).AddMinutes(16),
                    Turno = i == 0 ? "MANANA" : i == 1 ? "TARDE" : "REFUERZO",
                    Estado = "ACTIVA",
                    Observaciones = "Ruta 8108-C El Chino C sembrada desde Moovit como referencia de pruebas."
                });
            }
        }
        await context.SaveChangesAsync();

        await EnsureElChinoOperationalHistoryAsync(context, route.IdRuta);
    }

    private static async Task EnsureElChinoOperationalHistoryAsync(ApplicationDbContext context, int routeId)
    {
        var assignments = await context.AsignacionesOperacion
            .Where(a => a.IdRuta == routeId && a.Estado == "ACTIVA")
            .OrderBy(a => a.IdAsignacion)
            .Take(3)
            .ToListAsync();

        if (assignments.Count == 0)
        {
            return;
        }

        var assignmentIds = assignments.Select(a => a.IdAsignacion).ToArray();
        var alreadySeeded = await context.Viajes.AnyAsync(v =>
            assignmentIds.Contains(v.IdAsignacion) &&
            v.Observaciones == "Historial demo El Chino C para dashboard operativo.");

        if (alreadySeeded)
        {
            return;
        }

        var controlPoints = await context.RutaPuntosControl
            .Where(p => p.IdRuta == routeId)
            .OrderBy(p => p.Orden)
            .Select(p => new { p.Latitud, p.Longitud })
            .ToListAsync();

        if (controlPoints.Count < 2)
        {
            return;
        }

        var speedAlertType = await FindOrCreateAlertTypeAsync(
            context,
            "VELOCIDAD",
            "Exceso de velocidad",
            "Unidad supera la velocidad maxima permitida para la ruta.",
            4);

        var deviationAlertType = await FindOrCreateAlertTypeAsync(
            context,
            "DESVIO_RUTA",
            "Desvio de ruta",
            "Unidad fuera del trazo permitido de la ruta.",
            3);

        var today = DateTime.Today;
        var sampledPoints = controlPoints
            .Where((_, index) => index % 3 == 0)
            .Append(controlPoints.Last())
            .ToList();

        for (var dayOffset = 7; dayOffset >= 1; dayOffset--)
        {
            var serviceDate = today.AddDays(-dayOffset);

            for (var assignmentIndex = 0; assignmentIndex < assignments.Count; assignmentIndex++)
            {
                var assignment = assignments[assignmentIndex];
                var start = serviceDate
                    .AddHours(5 + assignmentIndex * 3)
                    .AddMinutes((dayOffset + assignmentIndex) % 4 * 7);
                var durationMinutes = 145 + dayOffset * 5 + assignmentIndex * 12;
                var end = start.AddMinutes(durationMinutes);
                var firstPoint = sampledPoints.First();
                var lastPoint = sampledPoints.Last();

                var trip = new Viaje
                {
                    IdAsignacion = assignment.IdAsignacion,
                    FechaInicioReal = start,
                    FechaFinReal = end,
                    Estado = "FINALIZADO",
                    LatitudInicio = firstPoint.Latitud,
                    LongitudInicio = firstPoint.Longitud,
                    LatitudFin = lastPoint.Latitud,
                    LongitudFin = lastPoint.Longitud,
                    Observaciones = "Historial demo El Chino C para dashboard operativo.",
                    ScoreConduccion = 82 + (dayOffset + assignmentIndex) % 12
                };

                context.Viajes.Add(trip);
                await context.SaveChangesAsync();

                var deviceId = await context.DispositivosGps
                    .Where(d => d.IdVehiculo == assignment.IdVehiculo && d.Estado)
                    .Select(d => (int?)d.IdDispositivo)
                    .FirstOrDefaultAsync();

                var readings = new List<GpsLectura>();
                for (var pointIndex = 0; pointIndex < sampledPoints.Count; pointIndex++)
                {
                    var point = sampledPoints[pointIndex];
                    var readingTime = start.AddMinutes(durationMinutes * pointIndex / Math.Max(sampledPoints.Count - 1, 1));
                    var speedingPoint = pointIndex == 3 && (dayOffset + assignmentIndex) % 2 == 0;
                    readings.Add(new GpsLectura
                    {
                        IdViaje = trip.IdViaje,
                        IdVehiculo = assignment.IdVehiculo,
                        IdDispositivo = deviceId,
                        FechaGps = readingTime,
                        Latitud = point.Latitud,
                        Longitud = point.Longitud,
                        VelocidadKmh = speedingPoint ? 74 + assignmentIndex * 3 : 36 + (pointIndex * 4 + dayOffset) % 20,
                        RumboGrados = 15 + pointIndex * 12,
                        PrecisionMetros = 8,
                        OrigenDato = "SIMULADOR",
                        Procesado = true,
                        FechaRegistro = readingTime
                    });
                }

                context.GpsLecturas.AddRange(readings);
                await context.SaveChangesAsync();

                var speedReading = readings.FirstOrDefault(r => r.VelocidadKmh > 60);
                if (speedReading != null)
                {
                    context.Alertas.Add(new Alerta
                    {
                        IdTipoAlerta = speedAlertType.IdTipoAlerta,
                        IdViaje = trip.IdViaje,
                        IdVehiculo = assignment.IdVehiculo,
                        IdConductor = assignment.IdConductor,
                        IdLecturaGps = speedReading.IdLectura,
                        FechaAlerta = speedReading.FechaGps,
                        Descripcion = "Exceso de velocidad detectado durante recorrido historico de prueba.",
                        ValorDetectado = speedReading.VelocidadKmh,
                        ValorPermitido = 60,
                        Latitud = speedReading.Latitud,
                        Longitud = speedReading.Longitud,
                        Estado = "PENDIENTE"
                    });
                }

                if ((dayOffset + assignmentIndex) % 3 == 0)
                {
                    var deviationReading = readings[Math.Min(4, readings.Count - 1)];
                    context.Alertas.Add(new Alerta
                    {
                        IdTipoAlerta = deviationAlertType.IdTipoAlerta,
                        IdViaje = trip.IdViaje,
                        IdVehiculo = assignment.IdVehiculo,
                        IdConductor = assignment.IdConductor,
                        IdLecturaGps = deviationReading.IdLectura,
                        FechaAlerta = deviationReading.FechaGps.AddMinutes(2),
                        Descripcion = "Desvio de ruta: vehiculo fuera del corredor permitido.",
                        ValorDetectado = 420,
                        ValorPermitido = 300,
                        Latitud = deviationReading.Latitud + 0.002M,
                        Longitud = deviationReading.Longitud - 0.002M,
                        Estado = "PENDIENTE"
                    });
                }

                await context.SaveChangesAsync();
            }
        }
    }

    private static async Task<TipoAlerta> FindOrCreateAlertTypeAsync(
        ApplicationDbContext context,
        string codigo,
        string nombre,
        string descripcion,
        int severidad)
    {
        var type = await context.TiposAlerta.FirstOrDefaultAsync(t => t.Codigo == codigo);
        if (type != null)
        {
            return type;
        }

        type = new TipoAlerta
        {
            Codigo = codigo,
            Nombre = nombre,
            Descripcion = descripcion,
            NivelSeveridad = severidad,
            Activa = true
        };
        context.TiposAlerta.Add(type);
        await context.SaveChangesAsync();
        return type;
    }

    private static async Task<Paradero> FindOrCreateParaderoAsync(
        ApplicationDbContext context,
        string nombre,
        string referencia,
        string distrito,
        decimal latitud,
        decimal longitud)
    {
        var paradero = await context.Paraderos.FirstOrDefaultAsync(p =>
            p.Nombre == nombre &&
            p.Distrito == distrito);

        if (paradero != null)
        {
            return paradero;
        }

        paradero = new Paradero
        {
            Nombre = nombre,
            DireccionReferencia = referencia,
            Distrito = distrito,
            Latitud = latitud,
            Longitud = longitud,
            Activo = true
        };
        context.Paraderos.Add(paradero);
        await context.SaveChangesAsync();
        return paradero;
    }
}
