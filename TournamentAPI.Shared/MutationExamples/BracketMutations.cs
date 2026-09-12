namespace TournamentAPI.Shared.MutationExamples;
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
                  matchesByBracket(first: 10) {
                    totalCount
                    edges {
                      node {
                        id
                        bracketId
                        round
                        player1Id
                        player2Id
                        winnerId
                      }
                    }
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
