using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PharmeasyAPI.Data;
using PharmeasyAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
var dbSection = builder.Configuration.GetSection("Database");
var provider = dbSection["Provider"] ?? "Sqlite";
var connectionString = dbSection["ConnectionString"] ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (provider.Equals("MySql", StringComparison.OrdinalIgnoreCase))
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
    else
        options.UseSqlite(connectionString);
});

// ── Authentication ────────────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException("Jwt:SecretKey is not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<OtpService>();
builder.Services.AddHttpClient();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// ── Swagger / OpenAPI ─────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Pharmeasy API",
        Version = "v1",
        Description = """
            REST API for the **Pharmeasy** pharmacy and doctor-consultation platform.

            ## Authentication
            Protected endpoints require a **Bearer JWT** token.
            1. Call `POST /send-otp` with your email to receive a one-time password.
            2. Call `POST /verify-otp/customer` (or `/admin`, `/doctor`) with the OTP to receive a token.
            3. Click **Authorize** above and paste the token (no `Bearer ` prefix needed).

            ## Roles
            | Role | Access |
            |------|--------|
            | **Customer** | Products, cart, orders, bookings, coupon apply |
            | **Doctor** | Own unavailability schedule |
            | **Admin** | Full management access to all resources |
            """,
        Contact = new OpenApiContact
        {
            Name = "Pharmeasy Dev Team",
            Email = "dev@pharmeasy.com"
        }
    });

    // JWT security definition
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token (without the 'Bearer ' prefix)."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });

    // Include XML doc comments generated at build time
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

// ── App pipeline ──────────────────────────────────────────────────────────────
var app = builder.Build();

// Auto-migrate on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseCors();

var imagesPath = Path.Combine(app.Environment.ContentRootPath, "images");
Directory.CreateDirectory(imagesPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(imagesPath),
    RequestPath = "/images"
});

app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI(ui =>
{
    ui.SwaggerEndpoint("/swagger/v1/swagger.json", "Pharmeasy API v1");
    ui.RoutePrefix = "swagger";
    ui.DocumentTitle = "Pharmeasy API Docs";
    ui.DefaultModelsExpandDepth(-1); // collapse schema section by default
});

app.MapControllers();

// ── Startup URL banner ────────────────────────────────────────────────────────
app.Lifetime.ApplicationStarted.Register(() =>
{
    var baseUrl  = app.Urls.FirstOrDefault() ?? "http://localhost:5000";
    var swagger  = $"{baseUrl}/swagger";
    var jsonSpec = $"{baseUrl}/swagger/v1/swagger.json";

    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine();
    Console.WriteLine("  ╔══════════════════════════════════════════════════════════════╗");
    Console.WriteLine("  ║                  Pharmeasy API is running                   ║");
    Console.WriteLine("  ╠══════════════════════════════════════════════════════════════╣");
    Console.WriteLine($"  ║  Application  →  {baseUrl,-44}║");
    Console.WriteLine($"  ║  Swagger UI   →  {swagger,-44}║");
    Console.WriteLine($"  ║  OpenAPI JSON →  {jsonSpec,-44}║");
    Console.WriteLine("  ╚══════════════════════════════════════════════════════════════╝");
    Console.ResetColor();
    Console.WriteLine();
});

app.Run();
