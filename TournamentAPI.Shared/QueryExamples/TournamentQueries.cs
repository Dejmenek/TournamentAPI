namespace TournamentAPI.Shared.QueryExamples;
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
                    participants(first: 10) {
                      totalCount
                      edges {
                        node {
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
                      matchesByBracket(first: 10) {
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
                      matchesByBracket(first: 10) {
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
                      isEmailPublic
                    }
                  }
                }
              }
            }
            """;

        public const string GetAllWithOwnerEmailOnly = """
            query {
              tournaments(first: 10) {
                edges {
                  node {
                    ownerId
                    owner {
                      email
                    }
                  }
                }
              }
            }
            """;

        public const string GetById = """
        query($id: Int!) {
          tournamentById(id: $id) {
            id
            name
            ownerId
            startDate
            status
          }
        }
        """;

        public const string GetByIdWithIsActive = """
        query($id: Int!) {
          tournamentById(id: $id) {
            id
            name
            status
            isActive
          }
        }
        """;

        public const string GetByIdWithOwner = """
        query($id: Int!) {
          tournamentById(id: $id) {
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
              isEmailPublic
            }
          }
        }
        """;

        public const string GetByIdWithOwnerEmailOnly = """
        query($id: Int!) {
          tournamentById(id: $id) {
            owner {
              email
            }
          }
        }
        """;

        public const string GetByIdWithParticipantEmailOnly = """
        query($id: Int!) {
          tournamentById(id: $id) {
            participants(first: 10) {
              edges {
                node {
                  participant {
                    id
                    email
                  }
                }
              }
            }
          }
        }
        """;

        public const string GetByIdWithParticipants = """
        query($id: Int!) {
          tournamentById(id: $id) {
            id
            name
            ownerId
            startDate
            status
            participants(first: 10) {
              totalCount
              edges {
                node {
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

        public const string GetByIdWithParticipantsSortedBySlotNumberDescending = """
        query($id: Int!) {
          tournamentById(id: $id) {
            id
            participants(first: 20, order: { slotNumber: DESC }) {
              totalCount
              edges {
                node {
                  participantId
                  tournamentId
                }
              }
            }
          }
        }
        """;

        public const string GetByIdWithParticipantsSortedBySlotNumberAscending = """
        query($id: Int!) {
          tournamentById(id: $id) {
            id
            participants(first: 20, order: { slotNumber: ASC }) {
              totalCount
              edges {
                node {
                  participantId
                  tournamentId
                }
              }
            }
          }
        }
        """;

        public const string GetByIdWithParticipantsFilteredBySlotNumberGreaterThan = """
        query($id: Int!, $minSlot: Int!) {
          tournamentById(id: $id) {
            id
            participants(first: 20, where: { slotNumber: { gt: $minSlot } }) {
              totalCount
              edges {
                node {
                  participantId
                  tournamentId
                }
              }
            }
          }
        }
        """;

        public const string GetByIdWithParticipantsFilteredByParticipantId = """
        query($id: Int!, $participantId: Int!) {
          tournamentById(id: $id) {
            id
            participants(first: 20, where: { participantId: { eq: $participantId } }) {
              totalCount
              edges {
                node {
                  participantId
                  tournamentId
                }
              }
            }
          }
        }
        """;

        public const string GetByIdWithParticipantsWonTournaments = """
        query($id: Int!) {
          tournamentById(id: $id) {
            id
            participants(first: 20) {
              totalCount
              edges {
                node {
                  participant {
                    id
                    wonTournaments(first: 5) {
                      totalCount
                    }
                  }
                }
              }
            }
          }
        }
        """;

        public const string GetByIdWithParticipantsWonMatches = """
        query($id: Int!) {
          tournamentById(id: $id) {
            id
            participants(first: 20) {
              totalCount
              edges {
                node {
                  participant {
                    id
                    wonMatches(first: 5) {
                      totalCount
                    }
                  }
                }
              }
            }
          }
        }
        """;

        public const string GetByIdWithParticipantsPlayedTournaments = """
        query($id: Int!) {
          tournamentById(id: $id) {
            id
            participants(first: 20) {
              totalCount
              edges {
                node {
                  participant {
                    id
                    playedTournaments(first: 5) {
                      totalCount
                    }
                  }
                }
              }
            }
          }
        }
        """;

        public const string GetByIdWithOwnerTournamentHistory = """
        query($id: Int!) {
          tournamentById(id: $id) {
            id
            name
            owner {
              id
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
        }
        """;

        public const string GetByIdWithChampion = """
        query($id: Int!) {
          tournamentById(id: $id) {
            id
            name
            status
            championId
            champion {
              id
              firstName
              lastName
            }
          }
        }
        """;

        public const string GetByIdWithAllMatchesAndPlayers = """
        query($id: Int!) {
          tournamentById(id: $id) {
            id
            name
            bracket {
              id
              tournamentId
              matchesByBracket(first: 20) {
                totalCount
                edges {
                  node {
                    id
                    round
                    player1Id
                    player2Id
                    winnerId
                    player1 {
                      id
                    }
                    player2 {
                      id
                    }
                    winner {
                      id
                    }
                  }
                }
              }
            }
          }
        }
        """;

        public const string GetByIdWithParticipantsAndTournamentBackReference = """
        query($id: Int!) {
          tournamentById(id: $id) {
            id
            name
            participants(first: 20) {
              totalCount
              edges {
                node {
                  participantId
                  tournamentId
                  participant {
                    id
                  }
                  tournament {
                    id
                  }
                }
              }
            }
          }
        }
        """;

        public const string GetByIdWithBracketAndMatches = """
        query($id: Int!) {
          tournamentById(id: $id) {
            id
            name
            ownerId
            startDate
            status
            bracket {
              id
              tournamentId
              matchesByBracket(first: 10) {
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
    }
}
