# TournamentAPI

## Table of Contents

- [General Info](#general-info)
- [Technologies](#technologies)
- [Running the API](#running-the-api)
- [Detailed API Documentation](#detailed-api-documentation)
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


## Running the API

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker](https://www.docker.com/) (for Prometheus, Loki, and Grafana)

### Start the Observability Stack

The API exports metrics via OpenTelemetry's OTLP exporter to Prometheus and ships structured logs via Serilog's OpenTelemetry sink to Loki. Before starting the API, bring up the observability stack with Docker Compose:

```bash
docker-compose up -d
```

This starts:

| Service | URL |
| --- | --- |
| Prometheus | http://localhost:5431 |
| Loki | http://localhost:3100 |
| Grafana | http://localhost:3000 |

- **Prometheus** is configured with `--web.enable-otlp-receiver` so it accepts OTLP pushes from the API. The scrape interval is set to 15 s globally (10 s for the Prometheus self-scrape job).
- **Loki** listens on port `3100` and accepts logs over the OTLP HTTP endpoint (`/otlp`). Logs are stored on the local filesystem.

### Start the API

```bash
dotnet run --project TournamentAPI
```

The API will push metrics to Prometheus and logs to Loki automatically once running.

### Grafana

Open `http://localhost:3000`, log in with the default credentials (`admin` / `admin`), and add the following data sources to build dashboards:

| Data source | URL |
| --- | --- |
| Prometheus | `http://prometheus:9090` |
| Loki | `http://loki:3100` |

---


## Detailed API Documentation
You can find the detailed API documentation in the published postman collection [here](https://documenter.getpostman.com/view/43726594/2sBYB1N89c).

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