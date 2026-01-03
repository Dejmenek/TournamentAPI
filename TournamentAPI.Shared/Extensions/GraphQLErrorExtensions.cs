using System.Text.Json;
using TournamentAPI.Shared.Helpers;

namespace TournamentAPI.Shared.Extensions;

public static class GraphQLErrorExtensions
{
    public static List<string?> GetErrorsArray(this GraphQLError error)
    {
        if (error.Extensions == null || !error.Extensions.ContainsKey("Errors"))
        {
            return [];
        }

        var errorsObject = error.Extensions["Errors"];
        if (errorsObject is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
        {
            return jsonElement.EnumerateArray()
                .Select(e => e.GetString())
                .ToList();
        }

        return [];
    }
}