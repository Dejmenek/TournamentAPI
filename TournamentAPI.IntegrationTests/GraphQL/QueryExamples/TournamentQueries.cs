namespace TournamentAPI.IntegrationTests.GraphQL.QueryExamples;
public static partial class Queries
{
    public static class Tournaments
    {
        public const string GetAllWithTotalCount = @"
            query {
                tournaments(first: 10) {
                    totalCount
                    edges {
                        cursor
                        node {
                            id
                            name
                            ownerId
                            startDate
                            status
                        }
                    }
                }
            }
        ";

        public const string GetWithoutPaging = """
            query {
              tournaments {
                totalCount
                edges {
                  cursor
                  node {
                    id
                    name
                    ownerId
                    startDate
                    status
                  }
                }
              }
            }
            """;

        public const string GetAllWithExcessivePageSize = """
            query {
              tournaments(first: 150) {
                totalCount
                edges {
                  cursor
                  node {
                    id
                    name
                    ownerId
                    startDate
                    status
                  }
                }
              }
            }
            """;

        public const string GetAllWithNameFilter = """
            query($nameFilter: String!) {
              tournaments(first: 10, where: { name: { contains: $nameFilter } }) {
                totalCount
                edges {
                  cursor
                  node {
                    id
                    name
                    ownerId
                    startDate
                    status
                  }
                }
              }
            }
            """;

        public const string GetAllWithParticipants = """
            query {
              tournaments(first: 10) {
                totalCount
                edges {
                  cursor
                  node {
                    id
                    name
                    ownerId
                    startDate
                    status
                    participants {
                      participantId
                      tournamentId
                      participant {
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
            """;

        public const string GetAllWithBracketAndMatches = """
            query {
              tournaments(first: 10) {
                totalCount
                edges {
                  cursor
                  node {
                    id
                    name
                    ownerId
                    startDate
                    status
                    bracket {
                      id
                      tournamentId
                      matches {
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

        public const string GetAllWithSorting = """
            query {
              tournaments(first: 10, order: { name: DESC }) {
                totalCount
                edges {
                  cursor
                  node {
                    id
                    name
                    ownerId
                    startDate
                    status
                    bracket {
                      id
                      tournamentId
                      matches {
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

        public const string GetAllWithOwner = """
            query {
              tournaments(first: 10) {
                totalCount
                edges {
                  cursor
                  node {
                    id
                    name
                    ownerId
                    startDate
                    status
                    owner {
                      email
                      firstName
                      id
                      lastName
                    }
                  }
                }
              }
            }
            """;
    }
}
