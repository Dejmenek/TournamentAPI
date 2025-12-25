namespace TournamentAPI.IntegrationTests.GraphQL.MutationExamples;
public static partial class TournamentMutations
{
    public static class Tournaments
    {
        public const string JoinTournament = """
            mutation JoinTournament($input: JoinTournamentInput!) {
              joinTournament(input: $input) {
                boolean
                errors {
                  ... on TournamentClosedError {
                    message
                  }
                  ... on TournamentJoinFailedError {
                    message
                  }
                  ... on TournamentNotFoundError {
                    message
                  }
                }
              }
            }
            """;

        public const string CreateTournamentWithBasicFieldsReturn = """
            mutation CreateTournament($input: CreateTournamentInput!) {
              createTournament(input: $input) {
              errors {
                ... on TournamentNameEmptyError {
                  message
                }
              }
              tournament {
                id
                name
                ownerId
                startDate
                status
              }
            }
            }
            """;

        public const string CreateTournamentWithOwnerReturn = """
            mutation CreateTournament($input: CreateTournamentInput!) {
              createTournament(input: $input) {
                errors {
                  ... on TournamentNameEmptyError {
                    message
                  }
                }
                tournament {
                  id
                  name
                  startDate
                  status
                  ownerId
                  owner {
                    email
                    firstName
                    id
                    lastName
                  }
                }
              }
            }
            """;

        public const string UpdateTournamentWithBasicFieldsReturn = """
            mutation UpdateTournament($input: UpdateTournamentInput!) {
              updateTournament(
                input: $input
              ) {
                errors {
                  ... on TournamentNameEmptyError {
                    message
                  }
                  ... on TournamentNotFoundError {
                    message
                  }
                  ... on TournamentNotOwnerError {
                    message
                  }
                }
                tournament {
                  id
                  name
                  ownerId
                  startDate
                  status
                }
              }
            }
            """;

        public const string UpdateTournamentWithOwnerReturn = """
            mutation UpdateTournament($input: UpdateTournamentInput!) {
              updateTournament(
                input: $input
              ) {
                errors {
                  ... on TournamentNameEmptyError {
                    message
                  }
                  ... on TournamentNotFoundError {
                    message
                  }
                  ... on TournamentNotOwnerError {
                    message
                  }
                }
                tournament {
                  id
                  name
                  startDate
                  status
                  ownerId
                  owner {
                    email
                    firstName
                    id
                    lastName
                  }
                }
              }
            }
            """;

        public const string DeleteTournament = """
            mutation DeleteTournament($input: DeleteTournamentInput!) {
              deleteTournament(input: $input) {
                boolean
              }
            }
            """;
    }
}
