using System.Text.Json;
using Infrastructure;
using Infrastructure.Persistence;
using Microservice.Logging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.FeatureManagement;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging.Console;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
const string LocalDevCorsPolicy = "LocalDevCorsPolicy";

builder.Logging.ClearProviders();
builder.Logging.AddConsole(options =>
{
    options.FormatterName = CustomJsonConsoleFormatter.FormatterName;
});
builder.Logging.AddConsoleFormatter<CustomJsonConsoleFormatter, CustomJsonConsoleFormatterOptions>();

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy(LocalDevCorsPolicy, policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddFeatureManagement();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Catalog Microservice API",
        Version = "v1",
        Description = "Sample clean-architecture microservice for catalog items."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT access token from Auth0."
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

var auth0Domain = builder.Configuration["Auth0:Domain"] ?? "dev-w6r6q15ny6i4p6yc.us.auth0.com";
var auth0Audience = builder.Configuration["Auth0:Audience"] ?? "https://catalog-api";
var configuredAudiences = builder.Configuration.GetSection("Auth0:ValidAudiences").Get<string[]>() ?? Array.Empty<string>();
var defaultAudiences = new[]
{
    auth0Audience,
    $"https://{auth0Domain}/api/v2/",
    $"https://{auth0Domain}/userinfo"
};
var validAudiences = configuredAudiences
    .Concat(defaultAudiences)
    .Where(audience => !string.IsNullOrWhiteSpace(audience))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://{auth0Domain}/";
        options.Audience = null;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidAudiences = validAudiences,
            AudienceValidator = (tokenAudiences, _, _) =>
            {
                if (tokenAudiences is null)
                {
                    return false;
                }

                return tokenAudiences.Any(tokenAudience =>
                    validAudiences.Contains(tokenAudience, StringComparer.OrdinalIgnoreCase));
            },
            NameClaimType = "sub"
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

await CatalogDbInitializer.InitializeAsync(app.Services);
app.Logger.LogInformation("JWT valid audiences: {Audiences}", string.Join(", ", validAudiences));

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors(LocalDevCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live"),
    ResponseWriter = WriteHealthResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
    ResponseWriter = WriteHealthResponse
});

app.MapControllers();

app.Run();

static Task WriteHealthResponse(HttpContext context, Microsoft.Extensions.Diagnostics.HealthChecks.HealthReport report)
{
    context.Response.ContentType = "application/json";

    var payload = JsonSerializer.Serialize(new
    {
        status = report.Status.ToString(),
        checks = report.Entries.Select(entry => new
        {
            name = entry.Key,
            status = entry.Value.Status.ToString(),
            description = entry.Value.Description,
            duration = entry.Value.Duration.ToString()
        })
    });

    return context.Response.WriteAsync(payload);
}

public partial class Program;
