namespace TournamentAPI.Shared.MutationExamples;
public static partial class Mutations
{
    public static class Payments
    {
        public const string StartStripeOnboarding = """
            mutation StartStripeOnboarding($input: StartStripeOnboardingInput!) {
              startStripeOnboarding(input: $input) {
                stripeOnboardingLink {
                  url
                  expiresAt
                }
              }
            }
            """;
    }
}
