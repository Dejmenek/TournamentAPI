using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Serilog;
using TournamentAPI.Configuration.Extensions;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;
using TournamentAPI.Services;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

builder.Services.AddApplicationOptions();

if (builder.Environment.IsDevelopment())
    builder.Services.AddForwardedHeaders();

builder.Services.AddApplicationDatabase();
builder.Services.AddApplicationRateLimiting();
builder.Services.AddApplicationTelemetry();
builder.Services.AddApplicationAuthentication();
builder.Services.AddApplicationHealthChecks();
builder.Services.AddApplicationAuthorization();
builder.Services.AddApplicationMetrics();
builder.Services.AddApplicationGraphQL();

builder.Services.AddScoped<JwtService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseForwardedHeaders();

    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    await context.Database.EnsureDeletedAsync();
    await context.Database.EnsureCreatedAsync();
    await DatabaseSeeder.SeedAsync(context, userManager);
}

app.UseRateLimiter();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
})
.RequireAuthorization("HealthCheckPolicy")
.RequireRateLimiting("IpBasedTokenBucket");

app.MapGraphQL()
    .RequireRateLimiting("IpBasedTokenBucket");

app.Run();

public partial class Program { }
