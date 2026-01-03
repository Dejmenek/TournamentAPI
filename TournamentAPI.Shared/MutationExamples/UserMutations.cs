namespace TournamentAPI.Shared.MutationExamples;
public static partial class Mutations
{
    public static class Users
    {
        public const string LoginUser = """
            mutation LoginUser($input: LoginUserInput!) {
              loginUser(input: $input) {
                string
              }
            }
            """;

        public const string RegisterUser = """
            mutation RegisterUser($input: RegisterUserInput!) {
              registerUser(input: $input) {
                boolean
              }
            }
            """;
    }
}
