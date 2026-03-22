# Title
Refresh Token Storage Strategy

# Date
22/03/2026

## Status
Accepted

## Context
When implementing refresh tokens for JWT authentication, a decision was needed on how to deliver and store the refresh token on the client side. The access token (short-lived, 10-minute expiration) is returned in the response body. The refresh token must be stored somewhere accessible to the client for subsequent token renewal requests, while minimizing exposure to theft.

## Considered Options
1. Return both access token and refresh token in the response body
   - Pros: Simple to implement; client has full control over token storage.
   - Cons: Refresh token is accessible to JavaScript, making it vulnerable to XSS attacks. Client must manage secure storage (e.g., localStorage is insecure, in-memory storage is lost on page refresh).
2. Return access token in the response body and refresh token in an HTTP-only cookie
   - Pros: HTTP-only cookies are inaccessible to JavaScript, significantly reducing XSS attack surface. Browser handles storage and automatically sends the cookie on requests to the same origin.
   - Cons: Requires CSRF protection considerations; slightly more complex to implement server-side cookie handling.

## Decision

We decided to return the access token in the response body and the refresh token in an HTTP-only cookie. The HTTP-only flag prevents JavaScript from reading the cookie, protecting the refresh token from XSS-based token theft. This is the more secure approach and aligns with security best practices for token storage.
