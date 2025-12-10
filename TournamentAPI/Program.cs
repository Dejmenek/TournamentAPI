using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TournamentAPI;
using TournamentAPI.Data;
using TournamentAPI.Models;
using TournamentAPI.Services;
using TournamentAPI.Types;
using MatchType = TournamentAPI.Types.MatchType;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddIdentity<ApplicationUser, IdentityRole<int>>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured."),
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer is not configured."),
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured."))
            )
        };
    });

builder.Services.AddScoped<JwtService>();

builder.Services
    .AddGraphQLServer()
    .RegisterDbContextFactory<ApplicationDbContext>()
    .AddAuthorization()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddType<TournamentType>()
    .AddType<BracketType>()
    .AddType<MatchType>()
    .AddType<TournamentParticipantType>()
    .AddType<ApplicationUserType>()
    .AddProjections()
    .AddFiltering()
    .AddSorting()
    .AddMaxExecutionDepthRule(5);

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    await context.Database.EnsureDeletedAsync();
    await context.Database.EnsureCreatedAsync();

    var user1 = new ApplicationUser { UserName = "alice", Email = "alice@example.com", FirstName = "Alice", LastName = "Smith" };
    var user2 = new ApplicationUser { UserName = "bob", Email = "bob@example.com", FirstName = "Bob", LastName = "Johnson" };
    var user3 = new ApplicationUser { UserName = "carol", Email = "carol@example.com", FirstName = "Carol", LastName = "Williams" };

    if (await userManager.FindByNameAsync(user1.UserName) == null)
        await userManager.CreateAsync(user1, "Password123!");
    if (await userManager.FindByNameAsync(user2.UserName) == null)
        await userManager.CreateAsync(user2, "Password123!");
    if (await userManager.FindByNameAsync(user3.UserName) == null)
        await userManager.CreateAsync(user3, "Password123!");

    if (!context.Tournaments.Any())
    {
        var tournament = new Tournament
        {
            Name = "Spring Invitational",
            StartDate = DateTime.UtcNow.AddDays(7),
            Status = TournamentStatus.Open,
            Participants = new List<ApplicationUser> { user1, user2 }
        };
        context.Tournaments.Add(tournament);

        var tournament2 = new Tournament
        {
            Name = "Summer Cup",
            StartDate = DateTime.UtcNow.AddDays(30),
            Status = TournamentStatus.Open,
            Participants = new List<ApplicationUser> { user2, user3 }
        };
        context.Tournaments.Add(tournament2);

        await context.SaveChangesAsync();
    }
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGraphQL();

app.Run();
