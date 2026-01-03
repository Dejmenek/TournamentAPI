namespace TournamentAPI.Shared.MutationExamples;
public static partial class Mutations
{
    public static class Match
    {
        public const string Play = """
            mutation Play($input: PlayInput!) {
              play(input: $input) {
                boolean
              }
            }
            """;
    }
}
