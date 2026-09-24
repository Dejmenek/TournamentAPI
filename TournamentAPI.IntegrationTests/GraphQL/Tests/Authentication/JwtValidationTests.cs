using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TournamentAPI.Configuration;
using TournamentAPI.IntegrationTests.Infrastructure;
using TournamentAPI.Shared.MutationExamples;
using TournamentAPI.Shared.QueryExamples;

namespace TournamentAPI.IntegrationTests.GraphQL.Tests.Authentication;

public class JwtValidationTests : BaseIntegrationTest
{
    public JwtValidationTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    private static readonly (string Name, string Query, object? Variables, bool CallsGetUserId)[] Operations =
    [
        ("GetMe", Queries.Users.GetMe, null, true),
        ("LogoutUser", Mutations.Users.LogoutUser, null, false),
        ("AddParticipant", Mutations.Participant.AddParticipantWithBasicFieldsReturn,
            new { input = new { userId = 1, tournamentId = 1 } }, true),
        ("JoinTournament", Mutations.Tournaments.JoinTournament,
            new { input = new { tournamentId = 1 } }, true),
        ("CreateTournament", Mutations.Tournaments.CreateTournamentWithBasicFieldsReturn,
            new { input = new { name = "Auth Test Tournament", startDate = DateTime.UtcNow.AddDays(1), status = "OPEN", maxParticipants = 4 } }, true),
        ("UpdateTournament", Mutations.Tournaments.UpdateTournamentWithBasicFieldsReturn,
            new { input = new { tournamentId = 1 } }, true),
        ("DeleteTournament", Mutations.Tournaments.DeleteTournament,
            new { input = new { tournamentId = 999999 } }, true),
        ("GenerateBracket", Mutations.Bracket.GenerateBracket,
            new { input = new { tournamentId = 1 } }, true),
        ("UpdateRound", Mutations.Bracket.UpdateRound,
            new { input = new { bracketId = 1, roundNumber = 1 } }, true),
        ("Play", Mutations.Match.Play,
            new { input = new { matchId = 1, winnerId = 1, player1Score = 1, player2Score = 0 } }, true),
        ("CorrectMatchResult", Mutations.Match.CorrectMatchResult,
            new { input = new { matchId = 1, winnerId = 1, player1Score = 1, player2Score = 0, version = "" } }, true),
        ("UpdateEmailVisibility", Mutations.Users.UpdateEmailVisibility,
            new { input = new { isEmailPublic = true } }, true),
    ];

    public static TheoryData<string, string, object?> AllProtectedOperations()
    {
        var data = new TheoryData<string, string, object?>();
        foreach (var op in Operations)
            data.Add(op.Name, op.Query, op.Variables);
        return data;
    }

    public static TheoryData<string, string, object?> OperationsRequiringUserIdClaim()
    {
        var data = new TheoryData<string, string, object?>();
        foreach (var op in Operations.Where(o => o.CallsGetUserId))
            data.Add(op.Name, op.Query, op.Variables);
        return data;
    }

    private JwtOptions GetJwtOptions() => Factory.Services.GetRequiredService<IOptions<JwtOptions>>().Value;

    [Theory]
    [MemberData(nameof(AllProtectedOperations))]
    public async Task ProtectedOperation_WithExpiredToken_ReturnsAuthenticationError(string name, string query, object? variables)
    {
        using var client = CreateClient();
        var token = JwtTestTokenFactory.CreateExpiredToken(GetJwtOptions(), 1, "alice", "alice@example.com");
        client.SetAuthToken(token);

        var response = await client.ExecuteQueryAsync<object>(query, variables);

        Assert.True(response.HasErrors, $"{name} was expected to fail with an expired token.");
        var error = response.Errors!.First();
        Assert.Equal("AUTH_NOT_AUTHENTICATED", error.Extensions?["code"]?.ToString());
    }

    [Theory]
    [MemberData(nameof(AllProtectedOperations))]
    public async Task ProtectedOperation_WithTamperedSignature_ReturnsAuthenticationError(string name, string query, object? variables)
    {
        using var client = CreateClient();
        var token = JwtTestTokenFactory.CreateTokenWithWrongSignature(GetJwtOptions(), 1, "alice", "alice@example.com");
        client.SetAuthToken(token);

        var response = await client.ExecuteQueryAsync<object>(query, variables);

        Assert.True(response.HasErrors, $"{name} was expected to fail with a tampered signature.");
        var error = response.Errors!.First();
        Assert.Equal("AUTH_NOT_AUTHENTICATED", error.Extensions?["code"]?.ToString());
    }

    [Theory]
    [MemberData(nameof(AllProtectedOperations))]
    public async Task ProtectedOperation_WithMalformedTokenString_ReturnsAuthenticationError(string name, string query, object? variables)
    {
        using var client = CreateClient();
        client.SetAuthToken(JwtTestTokenFactory.CreateMalformedTokenString());

        var response = await client.ExecuteQueryAsync<object>(query, variables);

        Assert.True(response.HasErrors, $"{name} was expected to fail with a malformed token.");
        var error = response.Errors!.First();
        Assert.Equal("AUTH_NOT_AUTHENTICATED", error.Extensions?["code"]?.ToString());
    }

    [Theory]
    [MemberData(nameof(OperationsRequiringUserIdClaim))]
    public async Task ProtectedOperation_WithTokenMissingNameIdentifierClaim_ReturnsDifferentErrorThanAuthenticationFailure(string name, string query, object? variables)
    {
        using var client = CreateClient();
        var token = JwtTestTokenFactory.CreateTokenMissingNameIdentifierClaim(GetJwtOptions(), 1, "alice", "alice@example.com");
        client.SetAuthToken(token);

        var response = await client.ExecuteQueryAsync<object>(query, variables);

        Assert.True(response.HasErrors, $"{name} was expected to fail without a NameIdentifier claim.");
        var error = response.Errors!.First();
        Assert.NotEqual("AUTH_NOT_AUTHENTICATED", error.Extensions?["code"]?.ToString());
    }

    [Fact]
    public async Task LogoutUser_WithTokenMissingNameIdentifierClaim_Succeeds()
    {
        using var client = CreateClient();

        var loginResponse = await client.ExecuteMutationAsync<Shared.Models.LoginResponse>(
            Mutations.Users.LoginUser,
            new { input = new { email = "alice@example.com", password = "Password123!" } });
        var rawRefreshToken = client.GetRefreshTokenCookie();
        Assert.NotNull(rawRefreshToken);
        client.SetRefreshTokenCookie(rawRefreshToken);

        var token = JwtTestTokenFactory.CreateTokenMissingNameIdentifierClaim(GetJwtOptions(), 1, "alice", "alice@example.com");
        client.SetAuthToken(token);

        var response = await client.ExecuteMutationAsync<Shared.Models.LogoutResponse>(
            Mutations.Users.LogoutUser, new { });

        Assert.False(response.HasErrors);
        Assert.True(response.Data?.LogoutUser?.Boolean);
    }
}
