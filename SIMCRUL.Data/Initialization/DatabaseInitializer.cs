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

        // 1. Seed Roles
        var requiredRoles = new[] { "Administrador", "Supervisor", "Operador", "Pasajero" };
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
}
