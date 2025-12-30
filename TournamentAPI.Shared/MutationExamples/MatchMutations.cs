namespace TournamentAPI.IntegrationTests.GraphQL.MutationExamples;
public static partial class Mutations
{
    public static class Match
    {
        public const string Play = """
            mutation Play($input: PlayInput!) {
              play(input: $input) {
                boolean
                errors {
                  ... on InvalidMatchWinnerError {
                    message
                  }
                  ... on MatchAlreadyPlayedError {
                    message
                  }
                  ... on MatchNotFoundError {
                    message
                  }
                  ... on TournamentNotClosedError {
                    message
                  }
                  ... on TournamentNotOwnerError {
                    message
                  }
                }
              }
            }
            """;
    }
}
