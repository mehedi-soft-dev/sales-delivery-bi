using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SalesDeliveryBI.Api.Middleware;
using SalesDeliveryBI.Application;
using SalesDeliveryBI.Infrastructure;
using SalesDeliveryBI.Infrastructure.Persistence.EfCore.Seed;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting SalesDeliveryBI API");

    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, config) => config
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .WriteTo.File(
            path: "logs/salesdeliverybi-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj} {Properties:j}{NewLine}{Exception}",
            formatProvider: CultureInfo.InvariantCulture));

    // Add services to the container.

    builder.Services.AddControllers();
    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // Dev-only signing key until the Identity service exists (docs/plans/security/security-plan.md §6,
    // open dependency) — swap for that service's real issuer/JWKS once it's built.
    string jwtSigningKey = builder.Configuration["Jwt:SigningKey"]
        ?? throw new InvalidOperationException("Missing 'Jwt:SigningKey' configuration.");

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = builder.Configuration["Jwt:Audience"],
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
                ValidateLifetime = true,
            };
        });

    // Dev-only CORS for the Angular dev server (`ng serve` on a different origin/port) — production serves the
    // frontend same-origin behind a reverse proxy (`apiBaseUrl: '/api'`, see frontend architecture.md), so no
    // CORS policy is registered outside Development.
    const string DevCorsPolicyName = "DevFrontend";
    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddCors(options =>
        {
            options.AddPolicy(DevCorsPolicyName, policy => policy
                .WithOrigins("http://localhost:4200", "https://localhost:4200")
                .AllowAnyHeader()
                .AllowAnyMethod());
        });
    }

    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails(options =>
    {
        options.CustomizeProblemDetails = context =>
            context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
    });

    WebApplication app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.UseCors(DevCorsPolicyName);

        using IServiceScope seedScope = app.Services.CreateScope();
        await seedScope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync();
    }

    app.UseSerilogRequestLogging();

    app.UseExceptionHandler();

    app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapGet("/", () => Results.Ok(new { status = "ok", service = "SalesDeliveryBI.Api" })).AllowAnonymous();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
