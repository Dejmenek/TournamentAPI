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
              }
            }
            """;
    }
}
