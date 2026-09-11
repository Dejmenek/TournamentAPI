namespace TournamentAPI.Shared.QueryExamples;
public static partial class Queries
{
    public static class Match
    {
        public const string GetMatchesForRoundWithBasicFields = """
            query GetMatchesForRound($tournamentId: Int!, $roundNumber: Int! ) {
              tournamentById(id: $tournamentId) {
                bracket {
                  matchesByBracket(first: 10, where: { round: { eq: $roundNumber } }) {
                    totalCount
                    edges {
                      node {
                        bracketId
                        id
                        player1Id
                        player2Id
                        round
                        winnerId
                      }
                    }
                  }
                }
              }
            }
            """;

        public const string GetMatchesForRoundWithPlayerDetails = """
            query GetMatchesForRound($tournamentId: Int!, $roundNumber: Int! ) {
              tournamentById(id: $tournamentId) {
                bracket {
                  matchesByBracket(first: 10, where: { round: { eq: $roundNumber } }) {
                    totalCount
                    edges {
                      node {
                        bracketId
                        id
                        player1Id
                        player2Id
                        round
                        winnerId
                        player2 {
                          email
                          firstName
                          id
                          lastName
                        }
                        player1 {
                          email
                          firstName
                          id
                          lastName
                        }
                        winner {
                          email
                          firstName
                          id
                          lastName
                        }
                      }
                    }
                  }
                }
              }
            }
            """;
    }
}
