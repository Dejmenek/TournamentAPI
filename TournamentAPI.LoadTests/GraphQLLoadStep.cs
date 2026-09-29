using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using NBomber.Contracts;
using NBomber.CSharp;

namespace TournamentAPI.LoadTests;

internal static class GraphQLLoadStep
{
    public const string BodyCheckFailedCode = "BODY_CHECK_FAILED";
    public const string InvalidJsonCode = "INVALID_JSON";
    public const string GraphQLErrorCode = "GRAPHQL_ERROR";

    public static Task<Response<object>> RunAsync(
        string stepName,
        IScenarioContext context,
        HttpClient client,
        string query,
        object? variables = null,
        Func<JsonElement, bool>? bodyCheck = null,
        IReadOnlySet<string>? expectedRejections = null,
        string? bearerToken = null)
    {
        return Step.Run(stepName, context, async () =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/graphql")
            {
                Content = JsonContent.Create(new { query, variables })
            };

            if (bearerToken is not null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            }

            using var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsByteArrayAsync();
            var (code, isSuccess) = Classify(response.IsSuccessStatusCode, (int)response.StatusCode, body, bodyCheck);

            return isSuccess || expectedRejections?.Contains(code) == true
                ? Response.Ok(statusCode: code, sizeBytes: body.Length)
                : Response.Fail(statusCode: code, sizeBytes: body.Length);
        });
    }

    public static Func<JsonElement, bool> NonEmptyConnection(string field)
    {
        return data => data.TryGetProperty(field, out var connection)
            && connection.TryGetProperty("edges", out var edges)
            && edges.ValueKind == JsonValueKind.Array
            && edges.GetArrayLength() > 0;
    }

    private static (string Code, bool IsSuccess) Classify(
        bool isSuccessStatusCode,
        int statusCode,
        byte[] body,
        Func<JsonElement, bool>? bodyCheck)
    {
        var httpCode = statusCode.ToString();

        if (!isSuccessStatusCode)
        {
            return (httpCode, false);
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            if (root.TryGetProperty("errors", out var errors)
                && errors.ValueKind == JsonValueKind.Array
                && errors.GetArrayLength() > 0)
            {
                return (ReadErrorCode(errors[0]), false);
            }

            var hasData = root.TryGetProperty("data", out var data);
            if (bodyCheck is not null && (!hasData || !bodyCheck(data)))
            {
                return (BodyCheckFailedCode, false);
            }

            return (httpCode, true);
        }
        catch (JsonException)
        {
            return (InvalidJsonCode, false);
        }
    }

    private static string ReadErrorCode(JsonElement error)
    {
        if (error.TryGetProperty("extensions", out var extensions)
            && extensions.TryGetProperty("code", out var code)
            && code.ValueKind == JsonValueKind.String)
        {
            return code.GetString() ?? GraphQLErrorCode;
        }

        return GraphQLErrorCode;
    }
}
