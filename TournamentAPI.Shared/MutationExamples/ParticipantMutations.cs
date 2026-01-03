namespace TournamentAPI.Shared.MutationExamples;
public static partial class Mutations
{
    public static class Participant
    {
        public const string AddParticipantWithBasicFieldsReturn = """
            mutation AddParticipant($input: AddParticipantInput!) {
              addParticipant(input: $input) {
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

        public const string AddParticipantWithOwnerDetailsReturn = """
            mutation AddParticipant($input: AddParticipantInput!) {
              addParticipant(input: $input) {
                tournament {
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
            """;

        public const string AddPartiipantWithParticipantsReturn = """
            mutation AddParticipant($input: AddParticipantInput!) {
              addParticipant(input: $input) {
                tournament {
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
            """;
    }
}
