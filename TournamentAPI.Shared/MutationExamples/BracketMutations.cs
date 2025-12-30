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
                errors {
                  ... on BracketAlreadyExistsError {
                    message
                  }
                  ... on BracketGenerationNotAllowedError {
                    message
                  }
                  ... on NotEnoughParticipantsError {
                    message
                  }
                  ... on TournamentNotFoundError {
                    message
                  }
                  ... on TournamentNotOwnerError {
                    message
                  }
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
                errors {
                  ... on BracketAlreadyExistsError {
                    message
                  }
                  ... on BracketGenerationNotAllowedError {
                    message
                  }
                  ... on NotEnoughParticipantsError {
                    message
                  }
                  ... on TournamentNotFoundError {
                    message
                  }
                  ... on TournamentNotOwnerError {
                    message
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
                errors {
                  ... on BracketNotFoundError {
                    message
                  }
                  ... on NoMatchesInRoundError {
                    message
                  }
                  ... on NotAllMatchesPlayedError {
                    message
                  }
                  ... on TournamentNotOwnerError {
                    message
                  }
                  ... on BracketAlreadyHasWinnerError {
                    message
                  }
                  ... on NextRoundAlreadyGeneratedError {
                    message
                  }
                }
              }
            }
            """;
    }
}
