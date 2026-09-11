using Hangfire;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Serilog;
using Serilog.Sinks.OpenTelemetry;
using TournamentAPI.Brackets;
using TournamentAPI.Configuration.Extensions;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;
using TournamentAPI.Matches;
using TournamentAPI.Participants;
using TournamentAPI.Services;
using TournamentAPI.Tournaments;
using TournamentAPI.Users;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

builder.Services.AddApplicationOptions();

if (builder.Environment.IsDevelopment())
    builder.Services.AddForwardedHeaders();

builder.Services.AddScoped<MatchService>();
builder.Services.AddScoped<ApplicationUserService>();
builder.Services.AddScoped<BracketLookupService>();
builder.Services.AddScoped<ParticipantsService>();
builder.Services.AddScoped<TournamentLookupService>();
builder.Services.AddApplicationDatabase();
builder.Services.AddApplicationRateLimiting();
builder.Services.AddApplicationTelemetry();
builder.Services.AddApplicationAuthentication();
builder.Services.AddApplicationHealthChecks();
builder.Services.AddApplicationAuthorization();
builder.Services.AddApplicationMetrics();
builder.Services.AddApplicationGraphQL(builder.Environment.IsDevelopment());
builder.Services.AddApplicationHangfire();

builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<TournamentAutoCloseJob>();

builder.Services.AddSerilog((_, loggerConfiguration) =>
    loggerConfiguration
        .WriteTo.Console()
        .WriteTo.OpenTelemetry(opt =>
        {
            opt.Endpoint = new Uri("http://localhost:3100/otlp").ToString();
            opt.Protocol = OtlpProtocol.HttpProtobuf;
            opt.ResourceAttributes = new Dictionary<string, object>
            {
                ["service.name"] = "TournamentAPI"
            };
        }));

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

using (var scope = app.Services.CreateScope())
{
    var recurringJobs = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    recurringJobs.AddOrUpdate<TournamentAutoCloseJob>(
        "auto-close-tournaments",
        job => job.RunAsync(CancellationToken.None),
        Cron.MinuteInterval(5));
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
    .WithOptions(options =>
    {
        options.Tool.Enable = app.Environment.IsDevelopment();
    })
    .RequireRateLimiting("IpBasedTokenBucket");

app.Run();

public partial class Program { }
