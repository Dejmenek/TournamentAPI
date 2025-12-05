using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TournamentAPI;
using TournamentAPI.Data;
using TournamentAPI.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddIdentity<ApplicationUser, IdentityRole<int>>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    await context.Database.EnsureDeletedAsync();
    await context.Database.EnsureCreatedAsync();
}

app.Run();
