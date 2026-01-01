namespace TournamentAPI.IntegrationTests.GraphQL.MutationExamples;
public static partial class Mutations
{
    public static class Bracket
    {
        public const string GenerateBracket = """
            mutation GenerateBracket($input: GenerateBracketInput!) {
              generateBracket(input: $input) {
                bracket {
                  id
                  tournamentId
                }
              }
            }
            """;

        public const string GenerateBracketWithMatches = """
            mutation GenerateBracketWithMatches($input: GenerateBracketInput!) {
              generateBracket(input: $input) {
                bracket {
                  id
                  tournamentId
                  matches {
                    id
                    round
                    participantAId
                    participantBId
                    scoreA
                    scoreB
                    winnerId
                  }
                }
              }
            }
            """;

        public const string UpdateRound = """
            mutation UpdateRound($input: UpdateRoundInput!) {
              updateRound(input: $input) {
                bracket {
                  id
                  tournamentId
                }
              }
            }
            """;
    }
}
