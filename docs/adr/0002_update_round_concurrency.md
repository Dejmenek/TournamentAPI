# Title
Eliminate Racing Conditions When Updating Round.

# Date
19/01/2026

## Status
Accepted

## Context
In our current implementation, we don't have a mechanism to prevent racing conditions when updating the tournament round.
This can lead to potential inconsistencies in the round data, especially when multiple updates occur simultaneously.

## Considered Options
1. Pessimistic Concurrency Control with Locking
   - Pros: Ensures that only one update can occur at a time, preventing racing conditions.
   - Cons: May introduce performance bottlenecks if not managed properly.
2. Using Database Transactions
   - Pros: Leverages existing database capabilities to ensure atomicity of updates.
   - Cons: Can block other operations and may lead to deadlocks if not handled carefully.
3. Optimistic Concurrency Control implemented via Versioning in EF Core
   - Pros: Allows multiple updates to occur simultaneously while ensuring data integrity through version checks.
   - Cons: Requires additional handling for concurrency exceptions. Accepting some wasted work when conflicts occur.

## Decision
We have decided to implement Optimistic Concurrency Control using Versioning in EF Core.
The conflicts won't be frequent, and this approach provides a good balance between performance and data integrity. Also we can accept some wasted work in case of conflicts because updates to rounds are not time-critical operations.
By adding a version field to the round entity, we can detect and handle concurrency conflicts effectively, prompting retries when necessary.