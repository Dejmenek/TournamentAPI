using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TournamentAPI.Models;

namespace TournamentAPI.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Tournament> Tournaments { get; set; }
    public DbSet<Bracket> Brackets { get; set; }
    public DbSet<Match> Matches { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Match>()
            .HasOne(m => m.Player1)
            .WithMany(u => u.MatchesAsPlayer1)
            .HasForeignKey(m => m.Player1Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Match>()
            .HasOne(m => m.Player2)
            .WithMany(u => u.MatchesAsPlayer2)
            .HasForeignKey(m => m.Player2Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Match>()
            .HasOne(m => m.Winner)
            .WithMany(u => u.MatchesWon)
            .HasForeignKey(m => m.WinnerId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
