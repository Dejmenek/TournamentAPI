using System.Net;
using TournamentAPI.Shared.Models;

namespace TournamentAPI.IntegrationTests.GraphQL.Tests.Authentication;

public class ApiKeyAuthenticationTests : BaseIntegrationTest
{
    public ApiKeyAuthenticationTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task PublicQuery_WithoutApiKey_ReturnsApiKeyError()
    {
        using var client = CreateClient();
        client.ClearApiKey();

        var response = await client.ExecuteQueryAsync<TournamentsResponse>(
            Shared.QueryExamples.Queries.Tournaments.GetAllWithTotalCount);

        Assert.True(response.HasErrors);
        Assert.Null(response.Data?.Tournaments);

        var error = response.Errors!.First();
        Assert.NotNull(error.Extensions);
        Assert.Equal("AUTH_API_KEY_INVALID", error.Extensions["code"]?.ToString());
        Assert.Equal(HttpStatusCode.Unauthorized, client.LastStatusCode);
    }

    [Fact]
    public async Task PublicQuery_WithInvalidApiKey_ReturnsApiKeyError()
    {
        using var client = CreateClient();
        client.SetApiKey("this-is-not-the-configured-key");

        var response = await client.ExecuteQueryAsync<TournamentsResponse>(
            Shared.QueryExamples.Queries.Tournaments.GetAllWithTotalCount);

        Assert.True(response.HasErrors);
        Assert.Null(response.Data?.Tournaments);

        var error = response.Errors!.First();
        Assert.Equal("AUTH_API_KEY_INVALID", error.Extensions?["code"]?.ToString());
        Assert.Equal(HttpStatusCode.Unauthorized, client.LastStatusCode);
    }

    [Fact]
    public async Task PublicQuery_WithValidApiKey_AndNoJwt_Succeeds()
    {
        using var client = CreateClient();

        var response = await client.ExecuteQueryAsync<TournamentsResponse>(
            Shared.QueryExamples.Queries.Tournaments.GetAllWithTotalCount);

        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data?.Tournaments);
    }

    [Fact]
    public async Task ProtectedQuery_WithValidApiKey_ButNoJwt_ReturnsAuthenticationError_NotApiKeyError()
    {
        using var client = CreateClient();

        var response = await client.ExecuteQueryAsync<MeResponse>(
            Shared.QueryExamples.Queries.Users.GetMe);

        Assert.True(response.HasErrors);
        Assert.Null(response.Data?.Me);

        var error = response.Errors!.First();
        Assert.Equal("AUTH_NOT_AUTHENTICATED", error.Extensions?["code"]?.ToString());
    }

    [Fact]
    public async Task ProtectedQuery_WithValidApiKeyAndValidJwt_Succeeds()
    {
        var email = "alice@example.com";
        using var client = CreateClient();

        var token = await client.ExecuteMutationAsync<LoginResponse>(
            Shared.MutationExamples.Mutations.Users.LoginUser,
            new { input = new { email, password = "Password123!" } });
        client.SetAuthToken(token.Data!.LoginUser!.String!);

        var response = await client.ExecuteQueryAsync<MeResponse>(
            Shared.QueryExamples.Queries.Users.GetMe);

        Assert.False(response.HasErrors);
        Assert.NotNull(response.Data?.Me);
        Assert.Equal(email, response.Data.Me.Email);
    }

    [Fact]
    public async Task ProtectedQuery_WithValidJwt_ButNoApiKey_ReturnsApiKeyError_JwtDoesNotBypassApiKeyGate()
    {
        using var client = CreateClient();

        var token = await client.ExecuteMutationAsync<LoginResponse>(
            Shared.MutationExamples.Mutations.Users.LoginUser,
            new { input = new { email = "alice@example.com", password = "Password123!" } });
        client.SetAuthToken(token.Data!.LoginUser!.String!);
        client.ClearApiKey();

        var response = await client.ExecuteQueryAsync<MeResponse>(
            Shared.QueryExamples.Queries.Users.GetMe);

        Assert.True(response.HasErrors);
        Assert.Null(response.Data?.Me);

        var error = response.Errors!.First();
        Assert.Equal("AUTH_API_KEY_INVALID", error.Extensions?["code"]?.ToString());
    }
}
