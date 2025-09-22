using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Collections.Generic;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.OpenApi.Models;
using Npgsql;
using Course_management.Data;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel for Render deployment (production only)
if (builder.Environment.IsProduction())
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
        options.ListenAnyIP(int.Parse(port));
    });
}

// Add services to the container.

builder.Services.AddControllers();

// Add CORS configuration
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Add JWT Authentication support in Swagger
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below. Example: 'Bearer 12345abcdef'",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

// Add EF Core and Identity
string connectionString;
if (builder.Environment.IsProduction())
{
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    if (!string.IsNullOrEmpty(databaseUrl))
    {
        // Convert Render PostgreSQL URL to .NET connection string format
        connectionString = ConvertDatabaseUrl(databaseUrl);
    }
    else
    {
        connectionString = builder.Configuration.GetConnectionString("ProductionConnection") 
            ?? throw new InvalidOperationException("DATABASE_URL environment variable is not set for production.");
    }
}
else
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("DefaultConnection is not configured.");
}

// Helper method to convert DATABASE_URL to .NET connection string
static string ConvertDatabaseUrl(string databaseUrl)
{
    try
    {
        var uri = new Uri(databaseUrl);
        var host = uri.Host;
        var port = uri.Port == -1 ? 5432 : uri.Port; // Default PostgreSQL port
        var database = uri.AbsolutePath.TrimStart('/');
        var userInfo = uri.UserInfo.Split(':');
        var username = userInfo[0];
        var password = userInfo.Length > 1 ? userInfo[1] : "";
        
        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(database) || string.IsNullOrEmpty(username))
        {
            throw new ArgumentException($"Invalid DATABASE_URL format. Host: '{host}', Database: '{database}', Username: '{username}'");
        }
        
        var connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
        Console.WriteLine($"Converted DATABASE_URL to connection string: Host={host}, Port={port}, Database={database}, Username={username}");
        
        return connectionString;
    }
    catch (Exception ex)
    {
        throw new ArgumentException($"Failed to parse DATABASE_URL: {ex.Message}", ex);
    }
}

// Configure database based on environment
if (builder.Environment.IsProduction())
{
    builder.Services.AddDbContext<Course_management.Data.DataContext>(options =>
        options.UseNpgsql(connectionString));
}
else
{
    builder.Services.AddDbContext<Course_management.Data.DataContext>(options =>
        options.UseSqlServer(connectionString));
}
builder.Services.AddIdentity<Course_management.Models.User, Microsoft.AspNetCore.Identity.IdentityRole>()
    .AddEntityFrameworkStores<Course_management.Data.DataContext>()
    .AddDefaultTokenProviders();

// Add Authentication (JWT + Google)
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
        NameClaimType = ClaimTypes.Name,
        RoleClaimType = ClaimTypes.Role
    };
    
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = context =>
        {
            // Map the JWT claims to the ClaimsPrincipal
            var jwtToken = context.SecurityToken as JwtSecurityToken;
            if (jwtToken != null)
            {
                var identity = context.Principal.Identity as ClaimsIdentity;
                if (identity != null)
                {
                    // Ensure the Sub claim is mapped to the NameIdentifier
                    var subClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
                    if (subClaim != null && !identity.HasClaim(c => c.Type == ClaimTypes.NameIdentifier))
                    {
                        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, subClaim.Value));
                    }
                }
            }
            return Task.CompletedTask;
        }
    };
})
.AddGoogle(options =>
{
    // TODO: Insert your Google ClientId and ClientSecret in appsettings.json
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireTutorRole", policy => policy.RequireRole("Tutor"));
    options.AddPolicy("RequireStudentRole", policy => policy.RequireRole("Student"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
// Enable Swagger in both Development and Production for API documentation
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Course Management API V1");
    c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
    c.RoutePrefix = "swagger"; // Explicitly set the route prefix
});

// Remove HTTPS redirection for Render deployment
// app.UseHttpsRedirection();

// Enable CORS
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

// Add health check endpoint
app.MapGet("/health", () => "Healthy");

// Add root endpoint that redirects to Swagger
app.MapGet("/", () => Results.Redirect("/swagger"));

// Run database migrations automatically
//try
//{
//    using (var scope = app.Services.CreateScope())
//    {
//        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
//        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
//        logger.LogInformation("Starting database migration...");
        
//        // Log connection string info for debugging (without exposing sensitive data)
//        var dbConnection = context.Database.GetDbConnection();
//        logger.LogInformation("Database provider: {Provider}", context.Database.ProviderName);
//        logger.LogInformation("Connection string configured: {HasConnectionString}", !string.IsNullOrEmpty(dbConnection.ConnectionString));
        
//        if (string.IsNullOrEmpty(dbConnection.ConnectionString))
//        {
//            logger.LogError("Connection string is null or empty!");
            
//            // Log environment variables for debugging
//            var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
//            logger.LogInformation("DATABASE_URL environment variable: {HasDatabaseUrl}", !string.IsNullOrEmpty(databaseUrl));
            
//            if (!string.IsNullOrEmpty(databaseUrl))
//            {
//                logger.LogInformation("DATABASE_URL length: {Length}", databaseUrl.Length);
//                logger.LogInformation("DATABASE_URL starts with: {Prefix}", databaseUrl.Substring(0, Math.Min(10, databaseUrl.Length)));
//            }
            
//            throw new InvalidOperationException("Database connection string is not properly configured.");
//        }
        
//        // Test database connection before migration
//        logger.LogInformation("Testing database connection...");
//        await context.Database.CanConnectAsync();
//        logger.LogInformation("Database connection successful.");
        
//        // Ensure database is created and apply any pending migrations
//        await context.Database.MigrateAsync();
        
//        logger.LogInformation("Database migration completed successfully.");
        
//        // Seed database with test data
//        await DataContext.SeedData(app.Services);
        
//        logger.LogInformation("Database seeding completed.");
//    }
//}
//catch (Exception ex)
//{
//    var logger = app.Services.GetRequiredService<ILogger<Program>>();
//    logger.LogError(ex, "An error occurred during database migration: {Message}", ex.Message);
//    throw;
//}

app.MapControllers();

app.Run();
