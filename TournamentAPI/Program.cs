using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TournamentAPI;
using TournamentAPI.Brackets;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;
using TournamentAPI.Matches;
using TournamentAPI.Participants;
using TournamentAPI.Services;
using TournamentAPI.Tournaments;
using TournamentAPI.Users;
using MatchType = TournamentAPI.Matches.MatchType;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddIdentity<ApplicationUser, IdentityRole<int>>(opt =>
{
    opt.User.RequireUniqueEmail = true;
})
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
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
    .AddMutationConventions()
    .AddQueryConventions()
    .AddType<TournamentType>()
    .AddType<BracketType>()
    .AddType<MatchType>()
    .AddType<TournamentParticipantType>()
    .AddType<ApplicationUserType>()
    .AddDataLoader<OwnerByTournamentIdDataLoader>()
    .AddProjections()
    .AddFiltering()
    .AddSorting()
    .AddMaxExecutionDepthRule(5);

builder.Services.AddScoped<JwtService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    await DatabaseSeeder.SeedAsync(context, userManager, recreateDatabase: true);
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapGraphQL();

app.Run();

public partial class Program { }