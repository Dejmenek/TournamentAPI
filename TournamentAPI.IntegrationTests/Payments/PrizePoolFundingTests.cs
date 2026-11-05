using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;
using TournamentAPI.Data.Models;
using TournamentAPI.IntegrationTests.Infrastructure;
using TournamentAPI.Payments;
using TournamentAPI.Shared.Helpers;
using TournamentAPI.Shared.Models;
using TournamentAPI.Tournaments;

namespace TournamentAPI.IntegrationTests.Payments;

public class PrizePoolFundingTests : BaseIntegrationTest
{
    private const string AliceEmail = "alice@example.com";
    private const string BobEmail = "bob@example.com";

    public PrizePoolFundingTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Fund_CreatesCheckoutSessionForPrizeTotalPlusPlatformFee()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);
        var tournamentId = await CreateTournamentAsync(AliceEmail, prizes: [5000, 2000]);

        // Act
        var response = await FundAsync(client, tournamentId);

        // Assert
        Assert.False(response.HasErrors);
        var request = Assert.Single(Factory.StripeGateway.CallsTo<CheckoutSessionRequest>(nameof(IStripeGateway.CreateCheckoutSessionAsync)));
        Assert.Equal(7350, request.Amount);
        Assert.Equal("eur", request.Currency);
        Assert.Equal($"tournament-{tournamentId}", request.TransferGroup);
        Assert.Equal(PrizePoolFunding.SessionTypeValue, request.Metadata[PrizePoolFunding.SessionTypeMetadataKey]);
        Assert.Equal(tournamentId.ToString(), request.Metadata[PrizePoolFunding.TournamentIdMetadataKey]);
        Assert.Equal(await GetUserIdAsync(AliceEmail), int.Parse(request.Metadata[PrizePoolFunding.OwnerIdMetadataKey]));
        Assert.True(request.ExpiresAt > DateTime.UtcNow.AddMinutes(30) && request.ExpiresAt < DateTime.UtcNow.AddMinutes(35));

        var tournament = await GetTournamentAsync(tournamentId);
        Assert.Equal(response.Data!.FundTournamentPrizePool!.PrizePoolFundingLink!.Url, $"https://checkout.stripe.com/c/pay/cs_test_{request.IdempotencyKey}");
        Assert.Equal($"cs_test_{request.IdempotencyKey}", tournament.FundingSessionId);
    }

    [Fact]
    public async Task Fund_RoundsPlatformFeeUp()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);
        var tournamentId = await CreateTournamentAsync(AliceEmail, prizes: [333]);

        // Act
        await FundAsync(client, tournamentId);

        // Assert
        var request = Assert.Single(Factory.StripeGateway.CallsTo<CheckoutSessionRequest>(nameof(IStripeGateway.CreateCheckoutSessionAsync)));
        Assert.Equal(333 + 17, request.Amount);
    }

    [Fact]
    public async Task Fund_ReturnsNotOwner_ForSomeoneElsesTournament()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(BobEmail);
        var tournamentId = await CreateTournamentAsync(AliceEmail, prizes: [5000]);

        // Act
        var response = await FundAsync(client, tournamentId);

        // Assert
        AssertErrorCode(response, TournamentErrorCodes.TournamentNotOwner);
        Assert.Empty(Factory.StripeGateway.CallsTo<CheckoutSessionRequest>(nameof(IStripeGateway.CreateCheckoutSessionAsync)));
    }

    [Theory]
    [InlineData(TournamentStatus.Open, PrizePoolStatus.Unfunded, 7)]
    [InlineData(TournamentStatus.Closed, PrizePoolStatus.Unfunded, -1)]
    [InlineData(TournamentStatus.Closed, PrizePoolStatus.Funded, 7)]
    [InlineData(TournamentStatus.Closed, PrizePoolStatus.NotRequired, 7)]
    public async Task Fund_ReturnsNotFundable_WhenTournamentOrPoolIsInTheWrongState(
        TournamentStatus status, PrizePoolStatus poolStatus, int startsInDays)
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);
        var tournamentId = await CreateTournamentAsync(AliceEmail, prizes: [5000], status: status, poolStatus: poolStatus, startsInDays: startsInDays);

        // Act
        var response = await FundAsync(client, tournamentId);

        // Assert
        AssertErrorCode(response, PaymentErrorCodes.PrizePoolNotFundable);
        Assert.Empty(Factory.StripeGateway.CallsTo<CheckoutSessionRequest>(nameof(IStripeGateway.CreateCheckoutSessionAsync)));
    }

    [Fact]
    public async Task Fund_ReturnsPayoutAccountNotEnabled_WhenTournamentHasEntryFeeAndOwnerIsNotEnabled()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);
        var tournamentId = await CreateTournamentAsync(AliceEmail, prizes: [5000], entryFee: 1000);

        // Act
        var response = await FundAsync(client, tournamentId);

        // Assert
        AssertErrorCode(response, PaymentErrorCodes.PayoutAccountNotEnabled);
        Assert.Empty(Factory.StripeGateway.CallsTo<CheckoutSessionRequest>(nameof(IStripeGateway.CreateCheckoutSessionAsync)));
    }

    [Fact]
    public async Task Fund_AllowsPrizesOnlyTournament_WhenOwnerHasNoPayoutAccount()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);
        var tournamentId = await CreateTournamentAsync(AliceEmail, prizes: [5000]);

        // Act
        var response = await FundAsync(client, tournamentId);

        // Assert
        Assert.False(response.HasErrors);
        Assert.Single(Factory.StripeGateway.CallsTo<CheckoutSessionRequest>(nameof(IStripeGateway.CreateCheckoutSessionAsync)));
    }

    [Fact]
    public async Task Fund_AllowsEntryFeeTournament_WhenOwnerPayoutAccountIsEnabled()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);
        await EnablePayoutAccountAsync(AliceEmail);
        var tournamentId = await CreateTournamentAsync(AliceEmail, prizes: [5000], entryFee: 1000);

        // Act
        var response = await FundAsync(client, tournamentId);

        // Assert
        Assert.False(response.HasErrors);
    }

    [Fact]
    public async Task Fund_ReturnsTheSameSession_OnARepeatCall()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);
        var tournamentId = await CreateTournamentAsync(AliceEmail, prizes: [5000]);
        var first = await FundAsync(client, tournamentId);
        Factory.StripeGateway.CheckoutSessionResult = sessionId => OpenSession(sessionId);

        // Act
        var second = await FundAsync(client, tournamentId);

        // Assert
        Assert.False(second.HasErrors);
        Assert.Equal(first.Data!.FundTournamentPrizePool!.PrizePoolFundingLink!.Url, second.Data!.FundTournamentPrizePool!.PrizePoolFundingLink!.Url);
        Assert.Single(Factory.StripeGateway.CallsTo<CheckoutSessionRequest>(nameof(IStripeGateway.CreateCheckoutSessionAsync)));
    }

    [Fact]
    public async Task Fund_ClearsAndRecreatesTheSession_WhenStripeExpiredItWithoutAWebhook()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);
        var tournamentId = await CreateTournamentAsync(AliceEmail, prizes: [5000]);
        var first = await FundAsync(client, tournamentId);
        Factory.StripeGateway.CheckoutSessionResult = sessionId => new Session { Id = sessionId, Status = "expired" };

        // Act
        var second = await FundAsync(client, tournamentId);

        // Assert
        Assert.False(second.HasErrors);
        var secondUrl = second.Data!.FundTournamentPrizePool!.PrizePoolFundingLink!.Url;
        Assert.NotEqual(first.Data!.FundTournamentPrizePool!.PrizePoolFundingLink!.Url, secondUrl);
        var requests = Factory.StripeGateway.CallsTo<CheckoutSessionRequest>(nameof(IStripeGateway.CreateCheckoutSessionAsync));
        Assert.Equal(2, requests.Count);
        Assert.NotEqual(requests[0].IdempotencyKey, requests[1].IdempotencyKey);
        Assert.EndsWith(requests[1].IdempotencyKey, secondUrl);
    }

    [Fact]
    public async Task Fund_CreatesOneSession_ForConcurrentCalls()
    {
        // Arrange
        using var firstClient = await CreateClientLoggedInAsAsync(AliceEmail);
        using var secondClient = await CreateClientLoggedInAsAsync(AliceEmail);
        var tournamentId = await CreateTournamentAsync(AliceEmail, prizes: [5000]);
        Factory.StripeGateway.CheckoutSessionResult = sessionId => OpenSession(sessionId);

        // Act
        var responses = await Task.WhenAll(FundAsync(firstClient, tournamentId), FundAsync(secondClient, tournamentId));

        // Assert
        Assert.All(responses, r => Assert.False(r.HasErrors));
        var urls = responses.Select(r => r.Data!.FundTournamentPrizePool!.PrizePoolFundingLink!.Url).Distinct().ToList();
        Assert.Single(urls);
        var tournament = await GetTournamentAsync(tournamentId);
        Assert.NotNull(tournament.FundingSessionId);
    }

    [Fact]
    public async Task Fund_KeepsTheStoredSessionAndReturnsItsUrl_WhenAnotherCallStoredADifferentOneFirst()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);
        var tournamentId = await CreateTournamentAsync(AliceEmail, prizes: [5000]);
        Factory.StripeGateway.CreateCheckoutSessionResult = request =>
        {
            DbContext.Tournaments
                .Where(t => t.Id == tournamentId)
                .ExecuteUpdate(s => s.SetProperty(t => t.FundingSessionId, "cs_test_winner"));

            return new Session { Id = "cs_test_loser", Status = "open", Url = "https://checkout.stripe.com/c/pay/cs_test_loser", ExpiresAt = request.ExpiresAt };
        };
        Factory.StripeGateway.CheckoutSessionResult = sessionId => new Session
        {
            Id = sessionId,
            Status = "open",
            Url = $"https://checkout.stripe.com/c/pay/{sessionId}"
        };

        // Act
        var response = await FundAsync(client, tournamentId);

        // Assert
        Assert.False(response.HasErrors);
        Assert.Equal("https://checkout.stripe.com/c/pay/cs_test_winner", response.Data!.FundTournamentPrizePool!.PrizePoolFundingLink!.Url);
        Assert.Equal("cs_test_winner", (await GetTournamentAsync(tournamentId)).FundingSessionId);
    }

    [Fact]
    public async Task UpdateTournament_CannotChangeTerms_WhileAFundingSessionIsOpen()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);
        var tournamentId = await CreateTournamentAsync(AliceEmail, prizes: [5000]);
        await FundAsync(client, tournamentId);

        // Act
        var response = await UpdateAsync(client, new { tournamentId, prizes = new[] { new { placement = 1, amount = 9000 } } });

        // Assert
        AssertErrorCode(response, TournamentErrorCodes.PrizePoolFundingInProgress);
        Assert.Equal(5000, (await GetPrizesAsync(tournamentId)).Single());
    }

    [Fact]
    public async Task UpdateTournament_CanChangeTermsAgain_AfterTheSessionExpired()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);
        var tournamentId = await CreateTournamentAsync(AliceEmail, prizes: [5000]);
        await FundAsync(client, tournamentId);
        var sessionId = (await GetTournamentAsync(tournamentId)).FundingSessionId!;
        using var webhookClient = Factory.CreateClient();
        await webhookClient.SendAsync(StripeWebhookPayloads.CheckoutSessionExpired(sessionId).ToRequest());

        // Act
        var response = await UpdateAsync(client, new { tournamentId, prizes = new[] { new { placement = 1, amount = 9000 } } });

        // Assert
        Assert.False(response.HasErrors);
        Assert.Equal(9000, (await GetPrizesAsync(tournamentId)).Single());
    }

    [Fact]
    public async Task UpdateTournament_CanChangeName_WhileAFundingSessionIsOpen()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);
        var tournamentId = await CreateTournamentAsync(AliceEmail, prizes: [5000]);
        await FundAsync(client, tournamentId);

        // Act
        var response = await UpdateAsync(client, new { tournamentId, name = "Renamed Cup" });

        // Assert
        Assert.False(response.HasErrors);
    }

    [Fact]
    public async Task CheckoutSessionCompleted_FundsThePoolAndStoresTheCharge_ThenTheTournamentCanOpen()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);
        var tournamentId = await CreateTournamentAsync(AliceEmail, prizes: [5000]);
        await FundAsync(client, tournamentId);
        var sessionId = (await GetTournamentAsync(tournamentId)).FundingSessionId!;
        using var webhookClient = Factory.CreateClient();

        // Act
        var webhookResponse = await webhookClient.SendAsync(StripeWebhookPayloads.CheckoutSessionCompleted(sessionId, "pi_funding").ToRequest());
        var openResponse = await UpdateAsync(client, new { tournamentId, status = "OPEN" });

        // Assert
        Assert.Equal(HttpStatusCode.OK, webhookResponse.StatusCode);
        var tournament = await GetTournamentAsync(tournamentId);
        Assert.Equal(PrizePoolStatus.Funded, tournament.PrizePoolStatus);
        Assert.Equal("ch_test_pi_funding", tournament.FundingChargeId);
        Assert.Equal("pi_funding", Assert.Single(Factory.StripeGateway.CallsTo<string>(nameof(IStripeGateway.GetPaymentIntentAsync))));
        Assert.False(openResponse.HasErrors);
        Assert.Equal(TournamentStatus.Open, tournament.Status);
    }

    [Fact]
    public async Task CheckoutSessionCompleted_ChangesNothingAndLogsAnError_WhenReplayedUnderANewEventId()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);
        var tournamentId = await CreateTournamentAsync(AliceEmail, prizes: [5000]);
        await FundAsync(client, tournamentId);
        var sessionId = (await GetTournamentAsync(tournamentId)).FundingSessionId!;
        using var webhookClient = Factory.CreateClient();
        await webhookClient.SendAsync(StripeWebhookPayloads.CheckoutSessionCompleted(sessionId, "pi_first").ToRequest());

        // Act
        var response = await webhookClient.SendAsync(StripeWebhookPayloads.CheckoutSessionCompleted(sessionId, "pi_second").ToRequest());

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("ch_test_pi_first", (await GetTournamentAsync(tournamentId)).FundingChargeId);
        AssertErrorLogged(sessionId);
    }

    [Fact]
    public async Task CheckoutSessionCompleted_ChangesNothingAndLogsAnError_WhenSessionIsNotTheStoredOne()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);
        var tournamentId = await CreateTournamentAsync(AliceEmail, prizes: [5000]);
        await FundAsync(client, tournamentId);
        using var webhookClient = Factory.CreateClient();

        // Act
        var response = await webhookClient.SendAsync(StripeWebhookPayloads.CheckoutSessionCompleted("cs_test_older").ToRequest());

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tournament = await GetTournamentAsync(tournamentId);
        Assert.Equal(PrizePoolStatus.Unfunded, tournament.PrizePoolStatus);
        Assert.Null(tournament.FundingChargeId);
        AssertErrorLogged("cs_test_older");
    }

    [Fact]
    public async Task CheckoutSessionCompleted_LogsAnError_WhenTheTournamentWasSoftDeleted()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);
        var tournamentId = await CreateTournamentAsync(AliceEmail, prizes: [5000]);
        await FundAsync(client, tournamentId);
        var sessionId = (await GetTournamentAsync(tournamentId)).FundingSessionId!;
        await DbContext.Tournaments.IgnoreQueryFilters()
            .Where(t => t.Id == tournamentId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsDeleted, true));
        using var webhookClient = Factory.CreateClient();

        // Act
        var response = await webhookClient.SendAsync(StripeWebhookPayloads.CheckoutSessionCompleted(sessionId).ToRequest());

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertErrorLogged(sessionId);
    }

    [Fact]
    public async Task CheckoutSessionCompleted_IgnoresSessionsThatAreNotPrizePoolFunding()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);
        var tournamentId = await CreateTournamentAsync(AliceEmail, prizes: [5000]);
        await FundAsync(client, tournamentId);
        var sessionId = (await GetTournamentAsync(tournamentId)).FundingSessionId!;
        using var webhookClient = Factory.CreateClient();

        // Act
        await webhookClient.SendAsync(StripeWebhookPayloads.CheckoutSessionCompleted(sessionId, sessionType: "tournament_entry").ToRequest());

        // Assert
        Assert.Equal(PrizePoolStatus.Unfunded, (await GetTournamentAsync(tournamentId)).PrizePoolStatus);
        Assert.DoesNotContain(Factory.LogCapture.Logs, l => l.Level == LogLevel.Error);
    }

    [Fact]
    public async Task CheckoutSessionExpired_ClearsTheSession_AndTheOwnerCanStartANewOne()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);
        var tournamentId = await CreateTournamentAsync(AliceEmail, prizes: [5000]);
        var first = await FundAsync(client, tournamentId);
        var sessionId = (await GetTournamentAsync(tournamentId)).FundingSessionId!;
        using var webhookClient = Factory.CreateClient();

        // Act
        var webhookResponse = await webhookClient.SendAsync(StripeWebhookPayloads.CheckoutSessionExpired(sessionId).ToRequest());
        var cleared = await GetTournamentAsync(tournamentId);
        var second = await FundAsync(client, tournamentId);

        // Assert
        Assert.Equal(HttpStatusCode.OK, webhookResponse.StatusCode);
        Assert.Null(cleared.FundingSessionId);
        Assert.Null(cleared.FundingSessionExpiresAt);
        Assert.False(second.HasErrors);
        Assert.NotEqual(first.Data!.FundTournamentPrizePool!.PrizePoolFundingLink!.Url, second.Data!.FundTournamentPrizePool!.PrizePoolFundingLink!.Url);
    }

    [Fact]
    public async Task CheckoutSessionExpired_DoesNotClearADifferentStoredSession()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);
        var tournamentId = await CreateTournamentAsync(AliceEmail, prizes: [5000]);
        await FundAsync(client, tournamentId);
        var sessionId = (await GetTournamentAsync(tournamentId)).FundingSessionId;
        using var webhookClient = Factory.CreateClient();

        // Act
        await webhookClient.SendAsync(StripeWebhookPayloads.CheckoutSessionExpired("cs_test_older").ToRequest());

        // Assert
        Assert.Equal(sessionId, (await GetTournamentAsync(tournamentId)).FundingSessionId);
    }

    [Fact]
    public async Task Fund_ReturnsTheStoredSession_WhenStripeReportsTheRequestStillInFlight()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);
        var tournamentId = await CreateTournamentAsync(AliceEmail, prizes: [5000]);
        Factory.StripeGateway.CreateCheckoutSessionResult = _ =>
        {
            DbContext.Tournaments
                .Where(t => t.Id == tournamentId)
                .ExecuteUpdate(s => s.SetProperty(t => t.FundingSessionId, "cs_test_winner"));

            throw new StripeException(HttpStatusCode.Conflict, new StripeError(), "A request with this idempotency key is in progress.");
        };
        Factory.StripeGateway.CheckoutSessionResult = sessionId => OpenSession(sessionId);

        // Act
        var response = await FundAsync(client, tournamentId);

        // Assert
        Assert.False(response.HasErrors);
        Assert.Equal("https://checkout.stripe.com/c/pay/cs_test_winner", response.Data!.FundTournamentPrizePool!.PrizePoolFundingLink!.Url);
        Assert.Equal(0, (await GetTournamentAsync(tournamentId)).FundingSessionAttempt);
    }

    [Fact]
    public async Task Fund_SendsTheSameKeyAndExpiry_WhenAnInFlightConflictLeavesNoSession()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);
        var tournamentId = await CreateTournamentAsync(AliceEmail, prizes: [5000]);
        Factory.StripeGateway.CreateCheckoutSessionResult = _ =>
            throw new StripeException(HttpStatusCode.Conflict, new StripeError(), "A request with this idempotency key is in progress.");
        var first = await FundAsync(client, tournamentId);
        Factory.StripeGateway.CreateCheckoutSessionResult = request => OpenSession($"cs_test_{request.IdempotencyKey}");

        // Act
        var second = await FundAsync(client, tournamentId);

        // Assert
        AssertErrorCode(first, PaymentErrorCodes.StripeRequestFailed);
        Assert.False(second.HasErrors);
        var requests = Factory.StripeGateway.CallsTo<CheckoutSessionRequest>(nameof(IStripeGateway.CreateCheckoutSessionAsync));
        Assert.Equal(2, requests.Count);
        Assert.Equal(requests[0].IdempotencyKey, requests[1].IdempotencyKey);
        Assert.Equal(requests[0].ExpiresAt, requests[1].ExpiresAt);
    }

    [Fact]
    public async Task Fund_AdvancesTheAttemptAndReportsNotFundable_WhenTheTermsChangeWhileTheSessionIsCreated()
    {
        // Arrange
        using var client = await CreateClientLoggedInAsAsync(AliceEmail);
        var tournamentId = await CreateTournamentAsync(AliceEmail, prizes: [5000]);
        Factory.StripeGateway.CreateCheckoutSessionResult = request =>
        {
            DbContext.Set<TournamentPrize>()
                .Where(p => p.TournamentId == tournamentId)
                .ExecuteUpdate(s => s.SetProperty(p => p.Amount, 9000L));

            return OpenSession($"cs_test_{request.IdempotencyKey}");
        };

        // Act
        var response = await FundAsync(client, tournamentId);

        // Assert
        AssertErrorCode(response, PaymentErrorCodes.PrizePoolNotFundable);
        var tournament = await GetTournamentAsync(tournamentId);
        Assert.Null(tournament.FundingSessionId);
        Assert.Null(tournament.FundingSessionExpiresAt);
        Assert.Equal(1, tournament.FundingSessionAttempt);
    }

    private void AssertErrorLogged(string sessionId) =>
        Assert.Contains(Factory.LogCapture.Logs, l => l.Level == LogLevel.Error && l.Message.Contains(sessionId));

    private static Session OpenSession(string sessionId) => new()
    {
        Id = sessionId,
        Status = "open",
        Url = $"https://checkout.stripe.com/c/pay/{sessionId}"
    };

    private static void AssertErrorCode<T>(GraphQLResponse<T> response, string expectedCode)
    {
        Assert.True(response.HasErrors);
        Assert.NotNull(response.Errors);
        Assert.Equal(expectedCode, response.Errors.First().Extensions!["code"]?.ToString());
    }

    private static Task<GraphQLResponse<FundTournamentPrizePoolResponse>> FundAsync(TestClient client, int tournamentId)
        => client.ExecuteMutationAsync<FundTournamentPrizePoolResponse>(
            Shared.MutationExamples.Mutations.Payments.FundTournamentPrizePool,
            new { input = new { tournamentId } });

    private static Task<GraphQLResponse<UpdateTournamentResponse>> UpdateAsync(TestClient client, object input)
        => client.ExecuteMutationAsync<UpdateTournamentResponse>(
            Shared.MutationExamples.Mutations.Tournaments.UpdateTournamentWithPaidTermsReturn,
            new { input });

    private async Task<int> CreateTournamentAsync(
        string ownerEmail,
        long[] prizes,
        long entryFee = 0,
        TournamentStatus status = TournamentStatus.Closed,
        PrizePoolStatus poolStatus = PrizePoolStatus.Unfunded,
        int startsInDays = 7)
    {
        var ownerId = await GetUserIdAsync(ownerEmail);
        var tournament = new Tournament
        {
            Name = "Funded Cup",
            StartDate = DateTime.UtcNow.AddDays(startsInDays),
            Status = status,
            OwnerId = ownerId,
            MaxParticipants = 8,
            EntryFeeAmount = entryFee,
            PrizePoolStatus = poolStatus,
            Prizes = prizes.Select((amount, index) => new TournamentPrize { Placement = index + 1, Amount = amount }).ToList()
        };

        DbContext.Tournaments.Add(tournament);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        return tournament.Id;
    }

    private Task<int> GetUserIdAsync(string email)
        => DbContext.Users.AsNoTracking().Where(u => u.Email == email).Select(u => u.Id).FirstAsync();

    private Task<Tournament> GetTournamentAsync(int tournamentId)
        => DbContext.Tournaments.AsNoTracking().FirstAsync(t => t.Id == tournamentId);

    private Task<List<long>> GetPrizesAsync(int tournamentId)
        => DbContext.Set<TournamentPrize>().AsNoTracking()
            .Where(p => p.TournamentId == tournamentId)
            .OrderBy(p => p.Placement)
            .Select(p => p.Amount)
            .ToListAsync();

    private Task EnablePayoutAccountAsync(string email)
        => DbContext.Users.Where(u => u.Email == email)
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
