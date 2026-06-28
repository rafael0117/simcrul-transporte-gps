using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SIMCRUL.Business.Hubs;
using SIMCRUL.Business.Interfaces;
using SIMCRUL.Business.Security;
using SIMCRUL.Business.Services;
using SIMCRUL.Data.Context;
using SIMCRUL.Data.Initialization;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. JWT and Security Configuration
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("JwtOptions"));
builder.Services.Configure<PasswordRecoveryOptions>(builder.Configuration.GetSection("PasswordRecovery"));
builder.Services.Configure<RecaptchaOptions>(builder.Configuration.GetSection("Recaptcha"));
var jwtSettings = builder.Configuration.GetSection("JwtOptions").Get<JwtOptions>() ?? new JwtOptions();
var tokenKey = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(tokenKey),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// 2. CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .SetIsOriginAllowed(_ => true) // Support SignalR connections from web/mobile
              .AllowCredentials();
    });
});

// 3. Database Configuration
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 4. DI Registrations
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddHttpClient<IRecaptchaService, GoogleRecaptchaService>();
builder.Services.AddScoped<IGpsProcessingService, GpsProcessingService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddSingleton<IFleetSimulationService, FleetSimulationService>();

// 5. Real-Time and Web APIs
builder.Services.AddSignalR();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddEndpointsApiExplorer();

// Swagger with Security definitions
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "SIMCRUL API", 
        Version = "v1",
        Description = "Backend de Monitoreo GPS y Control de Rutas Urbanas."
    });
    
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingrese 'Bearer [token]' para autenticar."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// 6. Seed Database on startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await DatabaseInitializer.SeedAsync(dbContext);
        Console.WriteLine("--> Base de datos inicializada y semilla cargada con éxito.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"--> Error al inicializar base de datos: {ex.Message}");
    }
}

// 7. Middlewares
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();

// Route endpoints
app.MapControllers();
app.MapHub<GpsHub>("/gpsHub");

app.Run();
