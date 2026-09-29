using System.Text.Json;
using NBomber.CSharp;
using UserMutations = TournamentAPI.Shared.MutationExamples.Mutations.Users;
using UserQueries = TournamentAPI.Shared.QueryExamples.Queries.Users;

namespace TournamentAPI.LoadTests;

[Trait("Category", "Load")]
public class AuthenticationLoadTests : BaseLoadTest, IClassFixture<MediumDatasetWebAppFactory>
{
    private const string RefreshTokenCookieName = "refreshToken=";

    private readonly MediumDatasetWebAppFactory _factory;

    public AuthenticationLoadTests(MediumDatasetWebAppFactory factory) : base(factory)
    {
        _factory = factory;
    }

    [Fact]
    public void AuthenticationFlow_ShouldMeetBudgets_UnderSteadyLogins()
    {
        // Arrange
        using var client = CreateClient();
        var http = client.HttpClient;
        var emails = _factory.UserEmails;
        var password = _factory.UserPassword;

        var scenario = GraphQLLoadScenario.Create("authenticate_and_refresh_session", async context =>
        {
            var email = emails[(int)(context.InvocationNumber % emails.Count)];
            string? accessToken = null;
            string? refreshCookie = null;

            var login = await GraphQLLoadStep.RunAsync(
                StepNames.Login,
                context,
                http,
                UserMutations.LoginUser,
                new { input = new { email, password } },
                HasStringResult("loginUser"),
                onResponse: (response, body) =>
                {
                    accessToken = ReadStringResult(body, "loginUser");
                    refreshCookie = ReadRefreshTokenCookie(response);
                });

            if (login.IsError)
            {
                return login;
            }

            var profile = await GraphQLLoadStep.RunAsync(
                StepNames.ViewProfile,
                context,
                http,
                UserQueries.GetMe,
                bodyCheck: GraphQLLoadStep.NonNullField("me"),
                bearerToken: accessToken);

            if (profile.IsError)
            {
                return profile;
            }

            var refresh = await GraphQLLoadStep.RunAsync(
                StepNames.RefreshToken,
                context,
                http,
                UserMutations.RefreshToken,
                bodyCheck: HasStringResult("refreshToken"),
                refreshTokenCookie: refreshCookie,
                onResponse: (response, body) =>
                {
                    accessToken = ReadStringResult(body, "refreshToken");
                    refreshCookie = ReadRefreshTokenCookie(response);
                });

            if (refresh.IsError)
            {
                return refresh;
            }

            return await GraphQLLoadStep.RunAsync(
                StepNames.Logout,
                context,
                http,
                UserMutations.LogoutUser,
                bodyCheck: HasTrueResult("logoutUser"),
                bearerToken: accessToken,
                refreshTokenCookie: refreshCookie);
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(10))
        .WithLoadSimulations(Simulation.Inject(
            rate: LoadTestBudgets.Rates.AuthFlowPerSecond,
            interval: TimeSpan.FromSeconds(1),
            during: TimeSpan.FromSeconds(30)));

        // Act & Assert
        LoadTestRun.Execute(
            scenario,
            LoadThresholds.ForSteps(
                LoadTestDataSize.Medium,
                "200",
                StepNames.Login,
                StepNames.ViewProfile,
                StepNames.RefreshToken,
                StepNames.Logout));
    }

    private static Func<JsonElement, bool> HasStringResult(string mutation)
    {
        return data => data.TryGetProperty(mutation, out var result)
            && result.TryGetProperty("string", out var value)
            && value.ValueKind == JsonValueKind.String;
    }

    private static Func<JsonElement, bool> HasTrueResult(string mutation)
    {
        return data => data.TryGetProperty(mutation, out var result)
            && result.TryGetProperty("boolean", out var value)
            && value.ValueKind == JsonValueKind.True;
    }

    private static string? ReadStringResult(byte[] body, string mutation)
    {
        using var document = JsonDocument.Parse(body);

        return document.RootElement.TryGetProperty("data", out var data)
            && data.TryGetProperty(mutation, out var result)
            && result.TryGetProperty("string", out var value)
            && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
    }

    private static string? ReadRefreshTokenCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            return null;
        }

        var cookie = cookies.FirstOrDefault(c => c.StartsWith(RefreshTokenCookieName, StringComparison.Ordinal));

        return cookie?.Split(';')[0][RefreshTokenCookieName.Length..];
    }
}
