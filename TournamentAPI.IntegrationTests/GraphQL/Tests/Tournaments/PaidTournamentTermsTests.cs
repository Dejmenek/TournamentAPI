using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TournamentAPI.Data.Models;
using TournamentAPI.Payments;
using TournamentAPI.Shared.Helpers;
using TournamentAPI.Shared.Models;
using TournamentAPI.Tournaments;

namespace TournamentAPI.IntegrationTests.GraphQL.Tests.Tournaments;

public class PaidTournamentTermsTests : BaseIntegrationTest
{
    private const string AliceEmail = "alice@example.com";
    private const string BobEmail = "bob@example.com";

    public PaidTournamentTermsTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateTournament_WithoutPaidTerms_HasPoolNotRequired()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);

        // Act
        var response = await CreateAsync(client, new { status = "OPEN" });

        // Assert
        Assert.False(response.HasErrors);
        var tournament = response.Data!.CreateTournament!.Tournament!;
        Assert.Equal("NOT_REQUIRED", tournament.PrizePoolStatus);
        Assert.Equal(0, tournament.EntryFee);
        Assert.Empty(tournament.Prizes!);
        Assert.Equal("OPEN", tournament.Status);
    }

    [Fact]
    public async Task CreateTournament_WithPrizes_IsUnfundedAndStoresPrizes()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);

        // Act
        var response = await CreateAsync(client, new
        {
            status = "CLOSED",
            entryFeeAmount = 1500,
            prizes = new[] { new { placement = 1, amount = 5000 }, new { placement = 2, amount = 2000 } }
        });

        // Assert
        Assert.False(response.HasErrors);
        var tournament = response.Data!.CreateTournament!.Tournament!;
        Assert.Equal("UNFUNDED", tournament.PrizePoolStatus);
        Assert.Equal(1500, tournament.EntryFee);
        Assert.Equal([5000L, 2000L], tournament.Prizes!.OrderBy(p => p.Placement).Select(p => p.Amount));
    }

    [Fact]
    public async Task CreateTournament_WithPrizesAndOpenStatus_ReturnsPrizePoolNotFunded()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);

        // Act
        var response = await CreateAsync(client, new
        {
            status = "OPEN",
            prizes = new[] { new { placement = 1, amount = 5000 } }
        });

        // Assert
        AssertErrorCode(response, TournamentErrorCodes.PrizePoolNotFunded);
        Assert.Equal(0, await DbContext.Tournaments.CountAsync(t => t.Name == "Paid Cup"));
    }

    [Fact]
    public async Task CreateTournament_WithEntryFeeAndOpenStatus_ReturnsPayoutAccountNotEnabled_WhenOwnerHasNoEnabledAccount()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);

        // Act
        var response = await CreateAsync(client, new { status = "OPEN", entryFeeAmount = 1000 });

        // Assert
        AssertErrorCode(response, PaymentErrorCodes.PayoutAccountNotEnabled);
    }

    [Fact]
    public async Task CreateTournament_WithEntryFeeAndOpenStatus_Succeeds_WhenOwnerPayoutAccountIsEnabled()
    {
        // Arrange
        await EnablePayoutAccountAsync(AliceEmail);
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);

        // Act
        var response = await CreateAsync(client, new { status = "OPEN", entryFeeAmount = 1000 });

        // Assert
        Assert.False(response.HasErrors);
        var tournament = response.Data!.CreateTournament!.Tournament!;
        Assert.Equal("OPEN", tournament.Status);
        Assert.Equal("NOT_REQUIRED", tournament.PrizePoolStatus);
    }

    [Fact]
    public async Task CreateTournament_WithNegativeEntryFee_ReturnsInvalidEntryFee()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);

        // Act
        var response = await CreateAsync(client, new { status = "CLOSED", entryFeeAmount = -1 });

        // Assert
        AssertErrorCode(response, TournamentErrorCodes.InvalidEntryFee);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task CreateTournament_WithNonPositivePrizeAmount_ReturnsInvalidPrizeAmount(long amount)
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);

        // Act
        var response = await CreateAsync(client, new
        {
            status = "CLOSED",
            prizes = new[] { new { placement = 1, amount } }
        });

        // Assert
        AssertErrorCode(response, TournamentErrorCodes.InvalidPrizeAmount);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 3)]
    [InlineData(2, 2)]
    [InlineData(0, 1)]
    public async Task CreateTournament_WithInvalidPrizePlacements_ReturnsInvalidPrizePlacements(int first, int second)
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);

        // Act
        var response = await CreateAsync(client, new
        {
            status = "CLOSED",
            format = "ROUND_ROBIN",
            prizes = new[] { new { placement = first, amount = 100 }, new { placement = second, amount = 50 } }
        });

        // Assert
        AssertErrorCode(response, TournamentErrorCodes.InvalidPrizePlacements);
    }

    [Fact]
    public async Task CreateTournament_WithMorePrizesThanMaxParticipants_ReturnsInvalidPrizePlacements()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);

        // Act
        var response = await CreateAsync(client, new
        {
            status = "CLOSED",
            format = "ROUND_ROBIN",
            maxParticipants = 2,
            prizes = new[]
            {
                new { placement = 1, amount = 300 },
                new { placement = 2, amount = 200 },
                new { placement = 3, amount = 100 }
            }
        });

        // Assert
        AssertErrorCode(response, TournamentErrorCodes.InvalidPrizePlacements);
    }

    [Fact]
    public async Task CreateTournament_SingleEliminationWithThirdPlacePrize_ReturnsPrizePlacementNotSupportedForFormat()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);

        // Act
        var response = await CreateAsync(client, new
        {
            status = "CLOSED",
            format = "SINGLE_ELIMINATION",
            prizes = new[]
            {
                new { placement = 1, amount = 300 },
                new { placement = 2, amount = 200 },
                new { placement = 3, amount = 100 }
            }
        });

        // Assert
        AssertErrorCode(response, TournamentErrorCodes.PrizePlacementNotSupportedForFormat);
    }

    [Fact]
    public async Task CreateTournament_RoundRobinWithThreePrizes_Succeeds()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);

        // Act
        var response = await CreateAsync(client, new
        {
            status = "CLOSED",
            format = "ROUND_ROBIN",
            prizes = new[]
            {
                new { placement = 1, amount = 300 },
                new { placement = 2, amount = 200 },
                new { placement = 3, amount = 100 }
            }
        });

        // Assert
        Assert.False(response.HasErrors);
        Assert.Equal(3, response.Data!.CreateTournament!.Tournament!.Prizes!.Count);
    }

    [Fact]
    public async Task UpdateTournament_ReplacesEntryFeeAndPrizes_WhenUnfundedWithNoEntries()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);
        var created = await CreateAsync(client, new
        {
            status = "CLOSED",
            prizes = new[] { new { placement = 1, amount = 5000 }, new { placement = 2, amount = 2000 } }
        });
        var id = created.Data!.CreateTournament!.Tournament!.Id;

        // Act
        var response = await UpdateAsync(client, new
        {
            tournamentId = id,
            entryFeeAmount = 700,
            prizes = new[] { new { placement = 1, amount = 9000 } }
        });

        // Assert
        Assert.False(response.HasErrors);
        var tournament = response.Data!.UpdateTournament!.Tournament!;
        Assert.Equal(700, tournament.EntryFee);
        var prize = Assert.Single(tournament.Prizes!);
        Assert.Equal(9000, prize.Amount);
        Assert.Equal(1, await DbContext.Set<TournamentPrize>().CountAsync(p => p.TournamentId == id));
    }

    [Fact]
    public async Task UpdateTournament_WithEmptyPrizes_ReturnsPoolToNotRequired()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);
        var created = await CreateAsync(client, new
        {
            status = "CLOSED",
            prizes = new[] { new { placement = 1, amount = 5000 } }
        });
        var id = created.Data!.CreateTournament!.Tournament!.Id;

        // Act
        var response = await UpdateAsync(client, new { tournamentId = id, prizes = Array.Empty<object>() });

        // Assert
        Assert.False(response.HasErrors);
        var tournament = response.Data!.UpdateTournament!.Tournament!;
        Assert.Equal("NOT_REQUIRED", tournament.PrizePoolStatus);
        Assert.Empty(tournament.Prizes!);
    }

    [Theory]
    [InlineData(PrizePoolStatus.Funded)]
    [InlineData(PrizePoolStatus.PaidOut)]
    [InlineData(PrizePoolStatus.Refunded)]
    public async Task UpdateTournament_ChangingTerms_ReturnsPaidTermsLocked_WhenPoolIsPastUnfunded(PrizePoolStatus poolStatus)
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);
        var created = await CreateAsync(client, new
        {
            status = "CLOSED",
            prizes = new[] { new { placement = 1, amount = 5000 } }
        });
        var id = created.Data!.CreateTournament!.Tournament!.Id;
        await DbContext.Tournaments.Where(t => t.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.PrizePoolStatus, poolStatus));

        // Act
        var response = await UpdateAsync(client, new { tournamentId = id, entryFeeAmount = 100 });

        // Assert
        AssertErrorCode(response, TournamentErrorCodes.PaidTermsLocked);
    }

    [Fact]
    public async Task UpdateTournament_ChangingTerms_ReturnsPaidTermsLocked_WhenTournamentHasEntries()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);
        var created = await CreateAsync(client, new { status = "CLOSED" });
        var id = created.Data!.CreateTournament!.Tournament!.Id;
        var bob = await DbContext.Users.AsNoTracking().FirstAsync(u => u.Email == BobEmail);
        DbContext.TournamentParticipants.Add(new TournamentParticipant
        {
            TournamentId = id,
            ParticipantId = bob.Id,
            SlotNumber = 1,
            EntryStatus = EntryStatus.PendingPayment
        });
        await DbContext.SaveChangesAsync();

        // Act
        var response = await UpdateAsync(client, new
        {
            tournamentId = id,
            prizes = new[] { new { placement = 1, amount = 100 } }
        });

        // Assert
        AssertErrorCode(response, TournamentErrorCodes.PaidTermsLocked);
    }

    [Fact]
    public async Task UpdateTournament_OpeningWithUnfundedPool_ReturnsPrizePoolNotFunded()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);
        var created = await CreateAsync(client, new
        {
            status = "CLOSED",
            prizes = new[] { new { placement = 1, amount = 5000 } }
        });
        var id = created.Data!.CreateTournament!.Tournament!.Id;

        // Act
        var response = await UpdateAsync(client, new { tournamentId = id, status = "OPEN" });

        // Assert
        AssertErrorCode(response, TournamentErrorCodes.PrizePoolNotFunded);
    }

    [Fact]
    public async Task UpdateTournament_OpeningCompletedTournamentWithUnfundedPool_ReturnsCannotChangeCompletedStatus()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);
        var created = await CreateAsync(client, new
        {
            status = "CLOSED",
            prizes = new[] { new { placement = 1, amount = 5000 } }
        });
        var id = created.Data!.CreateTournament!.Tournament!.Id;
        await DbContext.Tournaments.Where(t => t.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.Status, TournamentStatus.Completed));

        // Act
        var response = await UpdateAsync(client, new { tournamentId = id, status = "OPEN" });

        // Assert
        AssertErrorCode(response, TournamentErrorCodes.CannotChangeCompletedStatus);
    }

    [Fact]
    public async Task UpdateTournament_OpeningWithFundedPool_Succeeds()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);
        var created = await CreateAsync(client, new
        {
            status = "CLOSED",
            prizes = new[] { new { placement = 1, amount = 5000 } }
        });
        var id = created.Data!.CreateTournament!.Tournament!.Id;
        await DbContext.Tournaments.Where(t => t.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.PrizePoolStatus, PrizePoolStatus.Funded));

        // Act
        var response = await UpdateAsync(client, new { tournamentId = id, status = "OPEN" });

        // Assert
        Assert.False(response.HasErrors);
        Assert.Equal("OPEN", response.Data!.UpdateTournament!.Tournament!.Status);
    }

    [Fact]
    public async Task UpdateTournament_OpeningWithEntryFee_ReturnsPayoutAccountNotEnabled_WhenOwnerHasNoEnabledAccount()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);
        var created = await CreateAsync(client, new { status = "CLOSED", entryFeeAmount = 1000 });
        var id = created.Data!.CreateTournament!.Tournament!.Id;

        // Act
        var response = await UpdateAsync(client, new { tournamentId = id, status = "OPEN" });

        // Assert
        AssertErrorCode(response, PaymentErrorCodes.PayoutAccountNotEnabled);
    }

    [Fact]
    public async Task UpdateTournament_AddingPrizesToOpenFreeTournament_ReturnsPrizePoolNotFunded()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);
        var created = await CreateAsync(client, new { status = "OPEN" });
        var id = created.Data!.CreateTournament!.Tournament!.Id;

        // Act
        var response = await UpdateAsync(client, new
        {
            tournamentId = id,
            prizes = new[] { new { placement = 1, amount = 100 } }
        });

        // Assert
        AssertErrorCode(response, TournamentErrorCodes.PrizePoolNotFunded);
    }

    [Fact]
    public async Task UpdateTournament_LoweringMaxParticipantsBelowPrizeCount_ReturnsInvalidPrizePlacements()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);
        var created = await CreateAsync(client, new
        {
            status = "CLOSED",
            format = "ROUND_ROBIN",
            maxParticipants = 4,
            prizes = new[]
            {
                new { placement = 1, amount = 300 },
                new { placement = 2, amount = 200 },
                new { placement = 3, amount = 100 }
            }
        });
        var id = created.Data!.CreateTournament!.Tournament!.Id;
        await DbContext.Tournaments.Where(t => t.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.PrizePoolStatus, PrizePoolStatus.Funded)
                .SetProperty(t => t.Status, TournamentStatus.Open));

        // Act
        var response = await UpdateAsync(client, new { tournamentId = id, maxParticipants = 2 });

        // Assert
        AssertErrorCode(response, TournamentErrorCodes.InvalidPrizePlacements);
    }

    [Fact]
    public async Task JoinTournament_OnPaidTournament_ReturnsPaidTournamentRequiresCheckout()
    {
        // Arrange
        await EnablePayoutAccountAsync(AliceEmail);
        using var aliceClient = await CreateClientLoggedInAsAsync(AliceEmail);
        var created = await CreateAsync(aliceClient, new { status = "OPEN", entryFeeAmount = 1000 });
        var id = created.Data!.CreateTournament!.Tournament!.Id;
        using var bobClient = await CreateClientLoggedInAsAsync(BobEmail);

        // Act
        var response = await bobClient.ExecuteMutationAsync<JoinTournamentResponse>(
            Shared.MutationExamples.Mutations.Tournaments.JoinTournament,
            new { input = new { tournamentId = id } });

        // Assert
        AssertErrorCode(response, PaymentErrorCodes.PaidTournamentRequiresCheckout);
        Assert.Equal(0, await DbContext.TournamentParticipants.CountAsync(tp => tp.TournamentId == id));
    }

    [Fact]
    public async Task AddParticipant_OnPaidTournament_ReturnsPaidTournamentRequiresCheckout()
    {
        // Arrange
        await EnablePayoutAccountAsync(AliceEmail);
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);
        var created = await CreateAsync(client, new { status = "OPEN", entryFeeAmount = 1000 });
        var id = created.Data!.CreateTournament!.Tournament!.Id;
        var bob = await DbContext.Users.AsNoTracking().FirstAsync(u => u.Email == BobEmail);

        // Act
        var response = await client.ExecuteMutationAsync<JsonElement>(
            Shared.MutationExamples.Mutations.Participant.AddParticipantWithBasicFieldsReturn,
            new { input = new { tournamentId = id, userId = bob.Id } });

        // Assert
        Assert.True(response.HasErrors);
        Assert.Equal(PaymentErrorCodes.PaidTournamentRequiresCheckout, response.Errors!.First().Extensions!["code"]?.ToString());
        Assert.Equal(0, await DbContext.TournamentParticipants.CountAsync(tp => tp.TournamentId == id));
    }

    [Fact]
    public async Task Query_ExposesEntryFeePrizesPoolStatusAndEntryStatus()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);
        var created = await CreateAsync(client, new
        {
            status = "CLOSED",
            entryFeeAmount = 250,
            prizes = new[] { new { placement = 1, amount = 900 } }
        });
        var id = created.Data!.CreateTournament!.Tournament!.Id;
        var bob = await DbContext.Users.AsNoTracking().FirstAsync(u => u.Email == BobEmail);
        DbContext.TournamentParticipants.Add(new TournamentParticipant { TournamentId = id, ParticipantId = bob.Id, SlotNumber = 1 });
        await DbContext.SaveChangesAsync();

        // Act
        var response = await client.ExecuteQueryAsync<JsonElement>(
            Shared.QueryExamples.Queries.Tournaments.GetAllWithPaidTermsAndEntryStatus);

        // Assert
        Assert.False(response.HasErrors, string.Join("; ", response.Errors?.Select(e => e.Message) ?? []));
        var node = response.Data.GetProperty("tournaments").GetProperty("edges").EnumerateArray()
            .Select(e => e.GetProperty("node"))
            .First(n => n.GetProperty("id").GetInt32() == id);
        Assert.Equal(250, node.GetProperty("entryFee").GetInt64());
        Assert.Equal("UNFUNDED", node.GetProperty("prizePoolStatus").GetString());
        Assert.Equal(900, node.GetProperty("prizes")[0].GetProperty("amount").GetInt64());
        Assert.Equal("CONFIRMED", node.GetProperty("participants").GetProperty("edges")[0].GetProperty("node").GetProperty("entryStatus").GetString());
    }

    private static void AssertErrorCode<T>(GraphQLResponse<T> response, string expectedCode)
    {
        Assert.True(response.HasErrors);
        Assert.NotNull(response.Errors);
        Assert.Equal(expectedCode, response.Errors.First().Extensions!["code"]?.ToString());
    }

    private static Task<GraphQLResponse<CreateTournamentResponse>> CreateAsync(TestClient client, object overrides)
    {
        var input = new Dictionary<string, object?>
        {
            ["name"] = "Paid Cup",
            ["startDate"] = DateTime.UtcNow.AddDays(7).ToString("o"),
            ["status"] = "CLOSED",
            ["maxParticipants"] = 8
        };

        foreach (var property in overrides.GetType().GetProperties())
            input[property.Name] = property.GetValue(overrides);

        return client.ExecuteMutationAsync<CreateTournamentResponse>(
            Shared.MutationExamples.Mutations.Tournaments.CreateTournamentWithPaidTermsReturn,
            new { input });
    }

    private static Task<GraphQLResponse<UpdateTournamentResponse>> UpdateAsync(TestClient client, object input)
        => client.ExecuteMutationAsync<UpdateTournamentResponse>(
            Shared.MutationExamples.Mutations.Tournaments.UpdateTournamentWithPaidTermsReturn,
            new { input });

    private async Task EnablePayoutAccountAsync(string email)
        => await DbContext.Users.Where(u => u.Email == email)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.PayoutAccountStatus, PayoutAccountStatus.Enabled));

    private async Task<TestClient> CreateClientLoggedInAsAsync(string email)
    {
        var client = CreateClient();
        var token = await client.ExecuteMutationAsync<LoginResponse>(
            Shared.MutationExamples.Mutations.Users.LoginUser,
            new { input = new { email, password = "Password123!" } });
        client.SetAuthToken(token.Data!.LoginUser!.String!);
        return client;
    }
}
