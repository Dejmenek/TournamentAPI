namespace TournamentAPI.IntegrationTests.GraphQL.QueryExamples;
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
              }
            }
            """;
    }
}
