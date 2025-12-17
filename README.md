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
- **Postman** (for API testing)

---

## Detailed API Documentation

### Authentication

#### Register User

- **Mutation:** `registerUser(input: RegisterUserInput!): Boolean`
- **Input Example:**
```
{
  "query": "mutation RegisterUser($input: RegisterUserInput!) { registerUser(input: $input) { id userName email } }",
  "variables": {
    "input": {
      "userName": "alice",
      "email": "alice@example.com",
      "password": "Password123!"
    }
  }
}
```
- **Notes:** Returns `true` on success. Duplicate emails are rejected.

#### Login User

- **Mutation:** `loginUser(input: LoginUserInput!): String`
- **Input Example:**
```
{
  "query": "mutation($input: LoginUserInput!) { loginUser(input: $input) }",
  "variables": {
    "input": {
      "email": "alice@example.com",
      "password": "Password123!"
    }
  }
}
```

- **Returns:** JWT token as a string.

---

### Tournament Management

#### Create Tournament

- **Mutation:** `createTournament(input: CreateTournamentInput!): Tournament`
- **Requires:** JWT (owner)
- **Input Example:**
```
{
  "query": "mutation($input: CreateTournamentInput!) { createTournament(input: $input) { id name startDate status ownerId } }",
  "variables": {
    "input": {
      "name": "Bracket Test",
      "startDate": "2025-01-01T00:00:00Z",
      "status": "CLOSED"
    }
  }
}
```

#### Update Tournament

- **Mutation:** `updateTournament(input: UpdateTournamentInput!): Tournament`
- **Requires:** JWT (owner)
- **Input Example:**
```
{
  "query": "mutation($input: UpdateTournamentInput!) { updateTournament(input: $input) { id name startDate status } }",
  "variables": {
    "input": {
      "tournamentId": 3,
      "name": "Updated Name",
      "startDate": "2025-01-02T00:00:00Z",
      "status": "OPEN"
    }
  }
}
```
  
#### Delete Tournament

- **Mutation:** `deleteTournament(tournamentId: Int!): Boolean`
- **Requires:** JWT (owner)
- **Input Example:**
```
{
  "query": "mutation($tournamentId: Int!) { deleteTournament(tournamentId: $tournamentId) }",
  "variables": {
    "tournamentId": 3
  }
}
```
---

### Participant Management

#### Add Participant

- **Mutation:** `addParticipant(input: AddParticipantInput!): Tournament`
- **Requires:** JWT (owner)
- **Input Example:**
```
{
  "query": "mutation($input: AddParticipantInput!) { addParticipant(input: $input) { id name participants { participantId } } }",
  "variables": {
    "input": {
      "userId": 2,
      "tournamentId": 4
    }
  }
}
```
  
#### Join Tournament

- **Mutation:** `joinTournament(tournamentId: Int!): Boolean`
- **Requires:** JWT (participant)
- **Input Example:**
```
{
  "query": "mutation($tournamentId: Int!) { joinTournament(tournamentId: $tournamentId) }",
  "variables": {
    "tournamentId": 2
  }
}
```
  
---

### Bracket Management

#### Generate Bracket

- **Mutation:** `generateBracket(tournamentId: Int!): Bracket`
- **Requires:** JWT (owner, tournament must be closed)
- **Input Example:**
```
{
  "query": "mutation($tournamentId: Int!) { generateBracket(tournamentId: $tournamentId) { id tournamentId matches { id round player1 { id firstName lastName } player2 { id firstName lastName } winner { id firstName lastName } } } }",
  "variables": {
    "tournamentId": 2
  }
}
```
  
#### Play Match

- **Mutation:** `play(matchId: Int!, winnerId: Int!): Boolean`
- **Requires:** JWT (owner)
- **Input Example:**
```
{
  "query": "mutation($matchId: Int!, $winnerId: Int!) { play(matchId: $matchId, winnerId: $winnerId) }",
  "variables": {
    "matchId": 2,
    "winnerId": 3
  }
}
```
  
#### Update Round

- **Mutation:** `updateRound(bracketId: Int!, roundNumber: Int!): Bracket`
- **Requires:** JWT (owner)
- **Input Example:**
```
{
  "query": "mutation($bracketId: Int!, $roundNumber: Int!) { updateRound(bracketId: $bracketId, roundNumber: $roundNumber) { id tournamentId matches { id round player1 { id firstName lastName } player2 { id firstName lastName } winner { id firstName lastName } } } }",
  "variables": {
    "bracketId": 2,
    "roundNumber": 1
  }
}
```
---

### Queries

#### Get All Tournaments

- **Query:** `tournaments: [Tournament]`
- **Supports:** Paging, filtering, sorting
- **Example:**
```
{
  "query": "query { tournaments { edges { node { id name startDate status ownerId participants { participantId } } } } }",
  "variables": {}
}
```
  
#### Get Tournament By ID

- **Query:** `tournamentById(id: Int!): Tournament`
- **Example:**
```
{
  "query": "query($id: Int!) { tournamentById(id: $id) { id name startDate status ownerId bracket { id tournamentId matches { id round player1 { id firstName lastName } player2 { id firstName lastName } winner { id firstName lastName } } } participants { participantId participant { id firstName lastName email } } } }",
  "variables": {
    "id": 1
  }
}
```
  
#### Get Matches For Round

- **Query:** `matchesForRound(tournamentId: Int!, roundNumber: Int!): [Match]`
- **Example:**
```
{
  "query": "query($tournamentId: Int!, $roundNumber: Int!) { matchesForRound(tournamentId: $tournamentId, roundNumber: $roundNumber) { id round player1 { id firstName lastName } player2 { id firstName lastName } winner { id firstName lastName } } }",
  "variables": {
    "tournamentId": 1,
    "roundNumber": 1
  }
}
```
  
#### Filtering, Sorting, and Paging

- **Filtering Example:**
- **Sorting Example:**
- **Paging Example:**
   
---

**Note:** All GraphQL requests are sent to `/graphql` endpoint. For mutations requiring authentication, include the JWT in the `Authorization` header as `Bearer <token>`.
 
## Things Learned
- Implementing JWT authentication in ASP.NET Core.
- Setting up a GraphQL server using HotChocolate.
- Implementing filtering, sorting, and paging in GraphQL queries.
- Using Postman for testing GraphQL APIs.
- Handling complex mutations and queries in GraphQL.
- Working with nested data structures in GraphQL.

## Used Resources
- [HotChocolate Documentation](https://chillicream.com/docs/hotchocolate)
- [GraphQL Official Site](https://graphql.org/)
- [Postman Documentation](https://learning.postman.com/docs/getting-started/introduction/)