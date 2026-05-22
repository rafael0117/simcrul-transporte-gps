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
}
