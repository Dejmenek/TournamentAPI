using TournamentAPI.Shared.Helpers;
using TournamentAPI.Shared.Models;

namespace TournamentAPI.LoadTests;

internal sealed record VirtualUserSession(HttpClient Client, string AccessToken);

internal static class VirtualUserSessions
{
    public static async Task<VirtualUserSession[]> LoginAsync(
        Func<TestClient> createClient,
        IReadOnlyList<string> emails,
        string password,
        int count)
    {
        var sessions = new List<VirtualUserSession>();

        foreach (var email in emails.Take(count))
        {
            var client = createClient();

            var response = await client.ExecuteMutationAsync<LoginResponse>(
                Shared.MutationExamples.Mutations.Users.LoginUser,
                new { input = new { email, password } });

            var accessToken = response.Data?.LoginUser?.String
                ?? throw new InvalidOperationException($"Login failed for {email}: {response.Errors?.FirstOrDefault()?.Message}");

            sessions.Add(new VirtualUserSession(client.HttpClient, accessToken));
        }

        return sessions.ToArray();
    }

    public static VirtualUserSession Pick(VirtualUserSession[] sessions, long invocationNumber)
        => sessions[(int)(invocationNumber % sessions.Length)];
}
