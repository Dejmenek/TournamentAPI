using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace TournamentAPI.IntegrationTests.GraphQL.Helpers;
public class TestClient : IDisposable
{
    private readonly HttpClient _client;

    public TestClient(HttpClient client)
    {
        _client = client;
    }

    public void SetAuthToken(string token)
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    public void ClearAuthToken()
    {
        _client.DefaultRequestHeaders.Authorization = null;
    }

    public async Task<GraphQLResponse<T>> ExecuteQueryAsync<T>(
        string query,
        object? variables = null,
        CancellationToken cancellationToken = default)
    {
        var request = new
        {
            query,
            variables
        };

        var response = await _client.PostAsJsonAsync("/graphql", request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        var result = JsonSerializer.Deserialize<GraphQLResponse<T>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return result ?? throw new InvalidOperationException("Failed to deserialize GraphQL response");
    }

    public async Task<GraphQLResponse<T>> ExecuteMutationAsync<T>(
        string mutation,
        object variables,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteQueryAsync<T>(mutation, variables, cancellationToken);
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}

public class GraphQLResponse<T>
{
    public T? Data { get; set; }
    public List<GraphQLError>? Errors { get; set; }

    public bool HasErrors => Errors != null && Errors.Count > 0;
}

public class GraphQLError
{
    public string? Message { get; set; }
    public List<ErrorLocation>? Locations { get; set; }
    public List<object>? Path { get; set; }
    public Dictionary<string, object>? Extensions { get; set; }
}

public class ErrorLocation
{
    public int Line { get; set; }
    public int Column { get; set; }
}
