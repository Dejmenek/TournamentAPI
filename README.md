# TournamentAPI

## Table of Contents

- [General Info](#general-info)
- [Technologies](#technologies)
- [Detailed API Documentation](#detailed-api-documentation)
  - [Authentication](#authentication)
  - [Tournament Management](#tournament-management)
  - [Participant Management](#participant-management)
  - [Bracket Management](#bracket-management)
  - [Queries](#queries)
- [Things Learned](#things-learned)
- [Used Resources](#used-resources)
---

## General Info

**TournamentAPI** is a GraphQL-based web API for managing tournaments, participants, and brackets. It supports user registration, authentication via JWT, tournament creation and management, participant handling, bracket generation, and match play. The API is designed for extensibility and secure access, leveraging modern .NET and GraphQL best practices.

---

## Technologies

- **.NET 9** (C# 13)
- **ASP.NET Core**
- **Entity Framework Core** (SQL Server)
- **HotChocolate** (GraphQL server)
- **JWT Authentication** (`Microsoft.AspNetCore.Authentication.JwtBearer`)
- **ASP.NET Core Identity**
- **GraphQL Filtering, Sorting, Paging**
- **TestContainers**
- **XUnit**
- **Bogus** (for test data generation)
- **NBomber** (for load testing)

---

## Detailed API Documentation

### Authentication

#### Register User

- **Mutation:** `registerUser(input: RegisterUserInput!): Boolean`
- **Request Example:**
```json
{
  "query": "mutation RegisterUser($input: RegisterUserInput!) { registerUser(input: $input) { boolean } }",
  "variables": {
    "input": {
      "userName": "alice",
      "email": "alice@example.com",
      "password": "Password123!"
    }
  }
}
```
- **Response Example:**
```json
{
  "data": {
    "registerUser": {
      "boolean": true
    }
  }
}
```
- **Notes:** Returns `true` on success. Duplicate emails are rejected.

#### Login User

- **Mutation:** `loginUser(input: LoginUserInput!): String`
- **Request Example:**
```json
{
  "query": "mutation LoginUser($input: LoginUserInput!) { loginUser(input: $input) { string } }",
  "variables": {
    "input": {
      "email": "alice@example.com",
      "password": "Password123!"
    }
  }
}
```
- **Response Example:**
```json
{
  "data": {
    "loginUser": {
      "string": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    }
  }
}
```
- **Returns:** JWT token as a string.

#### Get Current User

- **Query:** `me: User`
- **Requires:** JWT
- **Request Example:**
```json
{
  "query": "query { me { id firstName lastName email } }",
  "variables": {}
}
```
- **Response Example:**
```json
{
  "data": {
    "me": {
      "id": 1,
      "firstName": "Alice",
      "lastName": "Smith",
      "email": "alice@example.com"
    }
  }
}
```

---

### Tournament Management

#### Create Tournament

- **Mutation:** `createTournament(input: CreateTournamentInput!): CreateTournamentPayload`
- **Requires:** JWT (owner)
- **Request Example:**
```json
{
  "query": "mutation CreateTournament($input: CreateTournamentInput!) { createTournament(input: $input) { errors { ... on TournamentNameEmptyError { message } } tournament { id name startDate status ownerId } } }",
  "variables": {
    "input": {
      "name": "Summer Championship",
      "startDate": "2025-07-01T00:00:00Z",
      "status": "OPEN"
    }
  }
}
```
- **Response Example (Success):**
```json
{
  "data": {
    "createTournament": {
      "errors": null,
      "tournament": {
        "id": 1,
        "name": "Summer Championship",
        "startDate": "2025-07-01T00:00:00Z",
        "status": "OPEN",
        "ownerId": 1
      }
    }
  }
}
```

#### Update Tournament

- **Mutation:** `updateTournament(input: UpdateTournamentInput!): UpdateTournamentPayload`
- **Requires:** JWT (owner)
- **Request Example:**
```json
{
  "query": "mutation UpdateTournament($input: UpdateTournamentInput!) { updateTournament(input: $input) { errors { ... on TournamentNameEmptyError { message } ... on TournamentNotFoundError { message } ... on TournamentNotOwnerError { message } } tournament { id name startDate status } } }",
  "variables": {
    "input": {
      "tournamentId": 3,
      "name": "Updated Championship Name",
      "startDate": "2025-07-15T00:00:00Z",
      "status": "CLOSED"
    }
  }
}
```
- **Response Example (Success):**
```json
{
  "data": {
    "updateTournament": {
      "errors": null,
      "tournament": {
        "id": 3,
        "name": "Updated Championship Name",
        "startDate": "2025-07-15T00:00:00Z",
        "status": "CLOSED"
      }
    }
  }
}
```
  
#### Delete Tournament

- **Mutation:** `deleteTournament(input: DeleteTournamentInput!): DeleteTournamentPayload`
- **Requires:** JWT (owner)
- **Request Example:**
```json
{
  "query": "mutation DeleteTournament($input: DeleteTournamentInput!) { deleteTournament(input: $input) { boolean errors { ... on TournamentNotFoundError { message } ... on TournamentNotOwnerError { message } } } }",
  "variables": {
    "input": {
      "tournamentId": 3
    }
  }
}
```
- **Response Example (Success):**
```json
{
  "data": {
    "deleteTournament": {
      "boolean": true,
      "errors": null
    }
  }
}
```
---

### Participant Management

#### Add Participant

- **Mutation:** `addParticipant(input: AddParticipantInput!): AddParticipantPayload`
- **Requires:** JWT (owner)
- **Request Example:**
```json
{
  "query": "mutation AddParticipant($input: AddParticipantInput!) { addParticipant(input: $input) { errors { ... on TournamentClosedError { message } ... on TournamentNotFoundError { message } ... on TournamentNotOwnerError { message } ... on UserAlreadyParticipantError { message } ... on UserNotFoundError { message } } tournament { id name participants { participantId tournamentId participant { id firstName lastName email } } } } }",
  "variables": {
    "input": {
      "userId": 2,
      "tournamentId": 1
    }
  }
}
```
- **Response Example (Success):**
```json
{
  "data": {
    "addParticipant": {
      "errors": null,
      "tournament": {
        "id": 1,
        "name": "Summer Championship",
        "participants": [
          {
            "participantId": 2,
            "tournamentId": 1,
            "participant": {
              "id": 2,
              "firstName": "Bob",
              "lastName": "Johnson",
              "email": "bob@example.com"
            }
          }
        ]
      }
    }
  }
}
```
  
#### Join Tournament

- **Mutation:** `joinTournament(input: JoinTournamentInput!): JoinTournamentPayload`
- **Requires:** JWT (participant)
- **Request Example:**
```json
{
  "query": "mutation JoinTournament($input: JoinTournamentInput!) { joinTournament(input: $input) { boolean errors { ... on TournamentClosedError { message } ... on UserAlreadyParticipantError { message } ... on TournamentNotFoundError { message } } } }",
  "variables": {
    "input": {
      "tournamentId": 1
    }
  }
}
```
- **Response Example (Success):**
```json
{
  "data": {
    "joinTournament": {
      "boolean": true,
      "errors": null
    }
  }
}
```
  
---

### Bracket Management

#### Generate Bracket

- **Mutation:** `generateBracket(input: GenerateBracketInput!): GenerateBracketPayload`
- **Requires:** JWT (owner, tournament must be closed)
- **Request Example:**
```json
{
  "query": "mutation GenerateBracket($input: GenerateBracketInput!) { generateBracket(input: $input) { bracket { id tournamentId matches { id round player1Id player2Id winnerId } } errors { ... on BracketAlreadyExistsError { message } ... on BracketGenerationNotAllowedError { message } ... on NotEnoughParticipantsError { message } ... on TournamentNotFoundError { message } ... on TournamentNotOwnerError { message } } } }",
  "variables": {
    "input": {
      "tournamentId": 1
    }
  }
}
```
- **Response Example (Success):**
```json
{
  "data": {
    "generateBracket": {
      "errors": null,
      "bracket": {
        "id": 1,
        "tournamentId": 1,
        "matches": [
          {
            "id": 1,
            "round": 1,
            "player1Id": 2,
            "player2Id": 3,
            "winnerId": null
          },
          {
            "id": 2,
            "round": 1,
            "player1Id": 4,
            "player2Id": 5,
            "winnerId": null
          }
        ]
      }
    }
  }
}
```
  
#### Play Match

- **Mutation:** `play(input: PlayInput!): PlayPayload`
- **Requires:** JWT (owner)
- **Request Example:**
```json
{
  "query": "mutation Play($input: PlayInput!) { play(input: $input) { boolean errors { ... on InvalidMatchWinnerError { message } ... on MatchAlreadyPlayedError { message } ... on MatchNotFoundError { message } ... on TournamentNotClosedError { message } ... on TournamentNotOwnerError { message } } } }",
  "variables": {
    "input": {
      "matchId": 1,
      "winnerId": 2
    }
  }
}
```
- **Response Example (Success):**
```json
{
  "data": {
    "play": {
      "boolean": true,
      "errors": null
    }
  }
}
```
  
#### Update Round

- **Mutation:** `updateRound(input: UpdateRoundInput!): UpdateRoundPayload`
- **Requires:** JWT (owner)
- **Request Example:**
```json
{
  "query": "mutation UpdateRound($input: UpdateRoundInput!) { updateRound(input: $input) { bracket { id tournamentId matches { id round player1Id player2Id winnerId } } errors { ... on BracketNotFoundError { message } ... on NoMatchesInRoundError { message } ... on NotAllMatchesPlayedError { message } ... on TournamentNotOwnerError { message } ... on BracketAlreadyHasWinnerError { message } ... on NextRoundAlreadyGeneratedError { message } } } }",
  "variables": {
    "input": {
      "bracketId": 1,
      "roundNumber": 1
    }
  }
}
```
- **Response Example (Success):**
```json
{
  "data": {
    "updateRound": {
      "errors": null,
      "bracket": {
        "id": 1,
        "tournamentId": 1,
        "matches": [
          {
            "id": 1,
            "round": 1,
            "player1Id": 2,
            "player2Id": 3,
            "winnerId": 2
          },
          {
            "id": 2,
            "round": 1,
            "player1Id": 4,
            "player2Id": 5,
            "winnerId": 5
          },
          {
            "id": 3,
            "round": 2,
            "player1Id": 2,
            "player2Id": 5,
            "winnerId": null
          }
        ]
      }
    }
  }
}
```
---

### Queries

#### Get All Tournaments

- **Query:** `tournaments(first: Int, after: String, where: TournamentFilterInput, order: [TournamentSortInput!]): TournamentsConnection`
- **Supports:** Paging, filtering, sorting
- **Request Example (Basic):**
```json
{
  "query": "query { tournaments(first: 10) { totalCount edges { cursor node { id name startDate status ownerId } } pageInfo { hasNextPage hasPreviousPage startCursor endCursor } } }",
  "variables": {}
}
```
- **Request Example (With Filtering):**
```json
{
  "query": "query GetTournaments($nameFilter: String!) { tournaments(first: 10, where: { name: { contains: $nameFilter } }) { totalCount edges { cursor node { id name startDate status ownerId } } } }",
  "variables": {
    "nameFilter": "Championship"
  }
}
```
- **Request Example (With Sorting):**
```json
{
  "query": "query { tournaments(first: 10, order: { name: DESC }) { totalCount edges { cursor node { id name startDate status ownerId } } } }",
  "variables": {}
}
```
- **Request Example (With Owner Details):**
```json
{
  "query": "query { tournaments(first: 10) { totalCount edges { cursor node { id name startDate status ownerId owner { id firstName lastName email } } } } }",
  "variables": {}
}
```
- **Response Example:**
```json
{
  "data": {
    "tournaments": {
      "totalCount": 5,
      "edges": [
        {
          "cursor": "MA==",
          "node": {
            "id": 1,
            "name": "Summer Championship",
            "startDate": "2025-07-01T00:00:00Z",
            "status": "OPEN",
            "ownerId": 1
          }
        },
        {
          "cursor": "MQ==",
          "node": {
            "id": 2,
            "name": "Winter Tournament",
            "startDate": "2025-12-01T00:00:00Z",
            "status": "CLOSED",
            "ownerId": 2
          }
        }
      ],
      "pageInfo": {
        "hasNextPage": false,
        "hasPreviousPage": false,
        "startCursor": "MA==",
        "endCursor": "MQ=="
      }
    }
  }
}
```
  
#### Get Tournament By ID

- **Query:** `tournamentById(id: Int!): Tournament`
- **Request Example (Basic):**
```json
{
  "query": "query GetTournamentById($id: Int!) { tournamentById(id: $id) { id name startDate status ownerId } }",
  "variables": {
    "id": 1
  }
}
```
- **Request Example (With Participants and Bracket):**
```json
{
  "query": "query GetTournamentById($id: Int!) { tournamentById(id: $id) { id name startDate status ownerId bracket { id tournamentId matches { id round player1Id player2Id winnerId player1 { id firstName lastName email } player2 { id firstName lastName email } winner { id firstName lastName email } } } participants { participantId participant { id firstName lastName email } } } }",
  "variables": {
    "id": 1
  }
}
```
- **Response Example:**
```json
{
  "data": {
    "tournamentById": {
      "id": 1,
      "name": "Summer Championship",
      "startDate": "2025-07-01T00:00:00Z",
      "status": "CLOSED",
      "ownerId": 1,
      "bracket": {
        "id": 1,
        "tournamentId": 1,
        "matches": [
          {
            "id": 1,
            "round": 1,
            "player1Id": 2,
            "player2Id": 3,
            "winnerId": 2,
            "player1": {
              "id": 2,
              "firstName": "Bob",
              "lastName": "Johnson",
              "email": "bob@example.com"
            },
            "player2": {
              "id": 3,
              "firstName": "Carol",
              "lastName": "Williams",
              "email": "carol@example.com"
            },
            "winner": {
              "id": 2,
              "firstName": "Bob",
              "lastName": "Johnson",
              "email": "bob@example.com"
            }
          }
        ]
      },
      "participants": [
        {
          "participantId": 2,
          "participant": {
            "id": 2,
            "firstName": "Bob",
            "lastName": "Johnson",
            "email": "bob@example.com"
          }
        },
        {
          "participantId": 3,
          "participant": {
            "id": 3,
            "firstName": "Carol",
            "lastName": "Williams",
            "email": "carol@example.com"
          }
        }
      ]
    }
  }
}
```
  
#### Get Matches For Round

- **Query:** `matchesForRound(tournamentId: Int!, roundNumber: Int!): [Match]`
- **Request Example (Basic):**
```json
{
  "query": "query GetMatchesForRound($tournamentId: Int!, $roundNumber: Int!) { matchesForRound(tournamentId: $tournamentId, roundNumber: $roundNumber) { id round bracketId player1Id player2Id winnerId } }",
  "variables": {
    "tournamentId": 1,
    "roundNumber": 1
  }
}
```
- **Request Example (With Player Details):**
```json
{
  "query": "query GetMatchesForRound($tournamentId: Int!, $roundNumber: Int!) { matchesForRound(tournamentId: $tournamentId, roundNumber: $roundNumber) { id round bracketId player1Id player2Id winnerId player1 { id firstName lastName email } player2 { id firstName lastName email } winner { id firstName lastName email } } }",
  "variables": {
    "tournamentId": 1,
    "roundNumber": 1
  }
}
```
- **Response Example:**
```json
{
  "data": {
    "matchesForRound": [
      {
        "id": 1,
        "round": 1,
        "bracketId": 1,
        "player1Id": 2,
        "player2Id": 3,
        "winnerId": 2,
        "player1": {
          "id": 2,
          "firstName": "Bob",
          "lastName": "Johnson",
          "email": "bob@example.com"
        },
        "player2": {
          "id": 3,
          "firstName": "Carol",
          "lastName": "Williams",
          "email": "carol@example.com"
        },
        "winner": {
          "id": 2,
          "firstName": "Bob",
          "lastName": "Johnson",
          "email": "bob@example.com"
        }
      },
      {
        "id": 2,
        "round": 1,
        "bracketId": 1,
        "player1Id": 4,
        "player2Id": 5,
        "winnerId": null,
        "player1": {
          "id": 4,
          "firstName": "David",
          "lastName": "Brown",
          "email": "david@example.com"
        },
        "player2": {
          "id": 5,
          "firstName": "Eve",
          "lastName": "Davis",
          "email": "eve@example.com"
        },
        "winner": null
      }
    ]
  }
}
```
   
---

## HTTP Request Information

**Endpoint:** All GraphQL requests are sent to `/graphql`

**Method:** `POST`

**Headers:**
- `Content-Type: application/json`
- `Authorization: Bearer <jwt-token>` (for authenticated requests)

**Request Body Structure:**
```json
{
  "query": "GraphQL query or mutation string",
  "variables": {
    "variableName": "value"
  }
}
```
---

**Note:** All GraphQL requests are sent to `/graphql` endpoint. For mutations requiring authentication, include the JWT in the `Authorization` header as `Bearer <token>`.
 
## Things Learned
- Implementing JWT authentication in ASP.NET Core.
- Setting up a GraphQL server using HotChocolate.
- Implementing filtering, sorting, and paging in GraphQL queries.
- Using Postman for testing GraphQL APIs.
- Handling complex mutations and queries in GraphQL.
- Working with nested data structures in GraphQL.
- Error handling with typed errors in GraphQL mutations.
- Implementing mutation conventions with HotChocolate.
- Started learning about load and stress testing with NBomber. Right now I feel like I barely scratched the surface here. I am not sure how to pick the right scenarios and how to interpret the results properly.

## Used Resources
- [HotChocolate Documentation](https://chillicream.com/docs/hotchocolate)
- [GraphQL Official Site](https://graphql.org/)
- [Postman Documentation](https://learning.postman.com/docs/getting-started/introduction/)
- [NBomber Documentation](https://nbomber.com/docs/getting-started/overview/)
- [Bogus Repository](https://github.com/bchavez/Bogus)