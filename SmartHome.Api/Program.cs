using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SmartHome.Api.Data;
using SmartHome.Api.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ================= DATABASE =================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection must be configured.");
}

builder.Services.AddDbContext<SmartHomeDbContext>(options =>
    options.UseSqlServer(
        connectionString
    ));

// ================= SERVICES =================
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<ISensorService, SensorService>();
builder.Services.AddScoped<ICameraService, CameraService>();
builder.Services.AddScoped<IFireAlarmService, FireAlarmService>();

// ================= JWT CONFIG =================
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = jwtSettings["Key"] ?? throw new InvalidOperationException("Missing Jwt:Key configuration.");
var issuer = jwtSettings["Issuer"];
var audience = jwtSettings["Audience"];
if (string.IsNullOrWhiteSpace(issuer))
    throw new InvalidOperationException("Missing Jwt:Issuer configuration.");
if (string.IsNullOrWhiteSpace(audience))
    throw new InvalidOperationException("Missing Jwt:Audience configuration.");
if (key.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
{
    if (!builder.Environment.IsDevelopment())
    {
        throw new InvalidOperationException("Jwt:Key must be configured from a secure secret source.");
    }
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = issuer,
        ValidAudience = audience,

        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(key))
    };
});

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
    var allowLanAnyOriginInDevelopment =
        builder.Configuration.GetValue<bool>("Cors:AllowLanAnyOriginInDevelopment");

    options.AddPolicy("LanPolicy", policy =>
    {
        if (allowedOrigins is { Length: > 0 })
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
            return;
        }

        if (builder.Environment.IsDevelopment())
        {
            if (allowLanAnyOriginInDevelopment)
            {
                policy.AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
                return;
            }

            policy.WithOrigins("http://localhost:3000", "http://localhost:5173", "http://localhost:51799")
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

// ================= SWAGGER =================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SmartHome.Api",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nh?p: Bearer {your_token}"
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

// ================= MIDDLEWARE =================
app.UseExceptionHandler();

if (!app.Environment.IsDevelopment())
{
    var allowedOrigins = app.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
    if (allowedOrigins is null || allowedOrigins.Length == 0)
    {
        app.Logger.LogWarning("CORS:AllowedOrigins is empty. Browser clients will be blocked by CORS in production until origins are configured.");
    }

    var bootstrapServiceEmail = app.Configuration.GetValue<string>("BootstrapServiceAccount:Email");
    var bootstrapServicePassword = app.Configuration.GetValue<string>("BootstrapServiceAccount:Password");
    if (string.IsNullOrWhiteSpace(bootstrapServiceEmail) || string.IsNullOrWhiteSpace(bootstrapServicePassword))
    {
        app.Logger.LogWarning("BootstrapServiceAccount is not configured. Service-to-service ingest will require a pre-provisioned Service user account.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.UseCors("LanPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

using (var scope = app.Services.CreateScope())
{
    await BootstrapAdminSeeder.SeedAsync(scope.ServiceProvider, app.Configuration);
    await BootstrapServiceAccountSeeder.SeedAsync(scope.ServiceProvider, app.Configuration);
}

app.Run(); 

public partial class Program { }

