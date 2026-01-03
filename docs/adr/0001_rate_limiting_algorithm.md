# Title
Rate Limiting Algorithm for API Requests

# Date
02/01/2026

## Status
Accepted

## Context
As our API usage continues to grow, we need to implement a robust rate limiting strategy to ensure fair usage among all clients and to protect our infrastructure from abuse.
Currently, we already have a limit based on query complexity / depth, query cost and timeouts. However, we need to add an additional layer of rate limiting to manage the overall request rate effectively.

## Considered Options
1. Concurrent Request Limiting
   - Pros: Limits the number of simultaneous requests.
   - Cons: Does not control the overall request rate effectively.
2. Ip-Based Rate Limiting
   - Pros: Targets individual clients based on their IP address.
   - Cons: Can be circumvented using proxies or VPNs.
3. User-Based Rate Limiting
   - Pros: More precise control over individual users.
   - Cons: There are some endpoints which should be available to all users regardless of their authentication status.
4. Partitioned Rate Limiting with Ip-Based as first level and concurrent request limiting as second level.
   - Pros: Allows for differentiated limits based on client type and request patterns.
   - Cons: More complex to implement and manage.

## Decision

We have decided to implement Partitioned Rate Limiting with Ip-Based as the first level and Concurrent Request Limiting as the second level.
This approach provides a balanced solution that addresses both overall request rates and simultaneous request handling, ensuring fair usage while protecting our infrastructure.