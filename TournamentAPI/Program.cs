using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TournamentAPI.Data;
using TournamentAPI.DataLoaders;
using TournamentAPI.Models;
using TournamentAPI.Mutations;
using TournamentAPI.Queries;
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
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = ctx =>
            {
                Console.WriteLine("AUTH FAILED: " + ctx.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = ctx =>
            {
                Console.WriteLine("AUTH SUCCESS: User authenticated - Claims: " + string.Join(", ", ctx.Principal?.Claims.Select(c => $"{c.Type}={c.Value}") ?? Array.Empty<string>()));
                return Task.CompletedTask;
            }
        };
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured."),
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer is not configured."),
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured."))
            )
        };
    });

builder.Services.AddAuthorizationBuilder()
    .SetDefaultPolicy(new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser().Build());

builder.Services
    .AddGraphQLServer()
    .AddAuthorization()
    .RegisterDbContextFactory<ApplicationDbContext>()
    .AddQueryType<Query>()
    .AddTypeExtension<TournamentQueries>()
    .AddTypeExtension<UserQueries>()
    .AddTypeExtension<MatchQueries>()
    .AddMutationType<Mutation>()
    .AddTypeExtension<TournamentMutations>()
    .AddTypeExtension<UserMutations>()
    .AddTypeExtension<MatchMutations>()
    .AddTypeExtension<BracketMutations>()
    .AddTypeExtension<ParticipantMutations>()
    .AddType<TournamentType>()
    .AddType<BracketType>()
    .AddType<MatchType>()
    .AddType<TournamentParticipantType>()
    .AddType<ApplicationUserType>()
    .AddProjections()
    .AddFiltering()
    .AddSorting()
    .AddMaxExecutionDepthRule(5);

builder.Services.AddScoped<JwtService>();
builder.Services.AddDataLoader<ParticipantsByTournamentIdDataLoader>();
builder.Services.AddDataLoader<MatchesByBracketIdDataLoader>();

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

    async Task<ApplicationUser> EnsureUserAsync(ApplicationUser user)
    {
        var existing = await userManager.FindByNameAsync(user.UserName);
        if (existing == null)
        {
            var result = await userManager.CreateAsync(user, "Password123!");
            return result.Succeeded ? user : throw new Exception($"Failed to create user {user.UserName}");
        }
        return existing;
    }

    user1 = await EnsureUserAsync(user1);
    user2 = await EnsureUserAsync(user2);
    user3 = await EnsureUserAsync(user3);

    user1 = await context.Users.FirstAsync(u => u.UserName == user1.UserName);
    user2 = await context.Users.FirstAsync(u => u.UserName == user2.UserName);
    user3 = await context.Users.FirstAsync(u => u.UserName == user3.UserName);

    if (!context.Tournaments.Any())
    {
        var tournament1 = new Tournament
        {
            Name = "Spring Invitational",
            StartDate = DateTime.UtcNow.AddDays(7),
            Status = TournamentStatus.Open,
            OwnerId = user1.Id,
            Owner = user1,
            Participants = new List<TournamentParticipant>()
        };
        var tournament2 = new Tournament
        {
            Name = "Summer Cup",
            StartDate = DateTime.UtcNow.AddDays(30),
            Status = TournamentStatus.Open,
            OwnerId = user2.Id,
            Owner = user2,
            Participants = new List<TournamentParticipant>()
        };

        tournament1.Participants.Add(new TournamentParticipant { Tournament = tournament1, Participant = user1 });
        tournament1.Participants.Add(new TournamentParticipant { Tournament = tournament1, Participant = user2 });

        tournament2.Participants.Add(new TournamentParticipant { Tournament = tournament2, Participant = user2 });
        tournament2.Participants.Add(new TournamentParticipant { Tournament = tournament2, Participant = user3 });

        context.Tournaments.AddRange(tournament1, tournament2);
        await context.SaveChangesAsync();
    }
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapGraphQL();

app.Run();
