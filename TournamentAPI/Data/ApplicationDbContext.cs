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
    public DbSet<TournamentParticipant> TournamentParticipants { get; set; }
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

        builder.Entity<Tournament>()
            .HasOne(t => t.Owner)
            .WithMany(u => u.OwnedTournaments)
            .HasForeignKey(t => t.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Tournament>().HasQueryFilter(t => !t.IsDeleted);

        builder.Entity<TournamentParticipant>()
            .HasKey(tp => new { tp.TournamentId, tp.ParticipantId });

        builder.Entity<TournamentParticipant>().HasQueryFilter(tp => !tp.IsDeleted);

        builder.Entity<TournamentParticipant>()
            .HasOne(tp => tp.Tournament)
            .WithMany(t => t.Participants)
            .HasForeignKey(tp => tp.TournamentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<TournamentParticipant>()
            .HasOne(tp => tp.Participant)
            .WithMany(u => u.ParticipatedTournaments)
            .HasForeignKey(tp => tp.ParticipantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Match>().HasQueryFilter(m => !m.IsDeleted);
        builder.Entity<Bracket>().HasQueryFilter(b => !b.IsDeleted);

        builder.Entity<Match>()
            .HasOne(m => m.Winner)
            .WithMany(u => u.MatchesWon)
            .HasForeignKey(m => m.WinnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Match>()
            .HasOne(m => m.Bracket)
            .WithMany(b => b.Matches)
            .HasForeignKey(m => m.BracketId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
