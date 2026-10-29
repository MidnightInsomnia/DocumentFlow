using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DocumentFlow.Api.Auth;
using DocumentFlow.Api.Health;
using DocumentFlow.Application;
using DocumentFlow.Application.Abstractions.Users;
using DocumentFlow.Application.Documents.Processing.Routing;
using DocumentFlow.Infrastructure;
using DocumentFlow.Infrastructure.Health;
using DocumentFlow.Infrastructure.Persistence;
using DocumentFlow.Infrastructure.Workspace;
using Microsoft.EntityFrameworkCore;
using DocumentFlow.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
{
    document.Components ??= new OpenApiComponents();
    document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
    {
        ["Bearer"] = new OpenApiSecurityScheme { Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT" }
    };
    foreach (var (path, item) in document.Paths)
    {
        if (item is null || path.Equals("/api/auth/login", StringComparison.OrdinalIgnoreCase)) continue;
        foreach (var operation in item.Operations?.Values.OfType<OpenApiOperation>() ?? Enumerable.Empty<OpenApiOperation>())
        {
            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", document)] = [] });
        }
    }
    return Task.CompletedTask;
}));
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
    });
});
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddDocumentWorkspace(builder.Configuration);
builder.Services.AddDocumentFlowHealthChecks();
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddSingleton<ITesseractLanguageLister, ProcessTesseractLanguageLister>();
builder.Services.AddTransient<OcrHealthProbe>();
builder.Services.AddHttpClient<AiHealthProbe>();
builder.Services.AddSingleton<IDashboardHealthSource, DashboardHealthSource>();
builder.Services.AddSingleton<DashboardHealthService>();
var jwt = JwtSettings.FromConfiguration(builder.Configuration);
builder.Services.AddSingleton(jwt);
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddScoped<IdentityBootstrapper>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwt.Issuer,
        ValidateAudience = true,
        ValidAudience = jwt.Audience,
        ValidateLifetime = true,
        RequireExpirationTime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        ClockSkew = TimeSpan.FromSeconds(30),
        NameClaimType = JwtRegisteredClaimNames.UniqueName,
        RoleClaimType = "role"
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var manager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
            if (!await JwtUserValidator.IsValidAsync(manager, context.Principal))
                context.Fail("Session is no longer valid.");
        }
    };
});
builder.Services.AddAuthorization(options => options.FallbackPolicy = new AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser().Build());

var app = builder.Build();
_ = app.Services.GetRequiredService<IOptions<RoutingOptions>>().Value;
using (var scope = app.Services.CreateScope())
{
    // Schema changes are explicitly opt-in; production can continue using the EF migration command.
    if (builder.Configuration.GetValue<bool>("Database:ApplyMigrations"))
        await scope.ServiceProvider.GetRequiredService<DocumentFlowDbContext>().Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<WorkspaceInitializer>().InitializeAsync();
    await scope.ServiceProvider.GetRequiredService<IdentityBootstrapper>().SeedAsync();
}
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.UseExceptionHandler();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = HealthCheckResponseWriter.WriteAsync
}).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
    ResponseWriter = HealthCheckResponseWriter.WriteAsync
}).AllowAnonymous();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
    ResponseWriter = HealthCheckResponseWriter.WriteAsync
}).AllowAnonymous();
app.Run();

public partial class Program;
