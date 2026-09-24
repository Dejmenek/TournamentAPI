namespace TournamentAPI.Shared.QueryExamples;
public static partial class Queries
{
    public static class Users
    {
        public const string GetMe = """
            query {
              me {
                id
                firstName
                lastName
                email
                isEmailPublic
              }
            }
            """;

        public const string GetMeWithTournamentHistory = """
            query {
              me {
                id
                firstName
                lastName
                email
                isEmailPublic
                wonTournaments(first: 10) {
                  totalCount
                  edges {
                    node {
                      id
                      name
                    }
                  }
                }
                playedTournaments(first: 10) {
                  totalCount
                  edges {
                    node {
                      id
                      name
                    }
                  }
                }
                wonMatches(first: 10) {
                  totalCount
                  edges {
                    node {
                      id
                      round
                      bracketId
                    }
                  }
                }
              }
            }
            """;

        public const string GetMePlayedTournamentsWithNameFilter = """
            query($nameFilter: String!) {
              me {
                playedTournaments(first: 10, where: { name: { contains: $nameFilter } }) {
                  totalCount
                  edges {
                    node {
                      id
                      name
                    }
                  }
                }
              }
            }
            """;

        public const string GetMeWonMatchesWithRoundFilter = """
            query($roundNumber: Int!) {
              me {
                wonMatches(first: 10, where: { round: { eq: $roundNumber } }) {
                  totalCount
                  edges {
                    node {
                      id
                      round
                    }
                  }
                }
              }
            }
            """;

        public const string GetMePlayedTournamentsSortedByNameDescending = """
            query {
              me {
                playedTournaments(first: 20, order: { name: DESC }) {
                  totalCount
                  edges {
                    node {
                      id
                      name
                    }
                  }
                }
              }
            }
            """;
    }
}
