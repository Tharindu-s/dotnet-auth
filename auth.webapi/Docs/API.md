# Auth API reference

Every endpoint of the auth service, with request and response examples. For setup, configuration and the design behind it, see the [root README](../../README.md).

- **Base URL (development):** `http://localhost:5183` (or `https://localhost:7023` with the `https` profile)
- **Content type:** `application/json`
- **Swagger UI:** `/swagger`, with fields for the custom headers

## Contents

1. [Authentication methods](#authentication-methods)
2. [Application clients](#application-clients)
3. [Register](#register)
4. [Login](#login)
5. [Refresh](#refresh)
6. [Logout](#logout)
7. [Current user](#current-user)
8. [Sessions](#sessions)
9. [JWKS](#jwks)
10. [Errors](#errors)
11. [Rate limits and lockout](#rate-limits-and-lockout)
12. [Validating tokens in your own API](#validating-tokens-in-your-own-api)

## Authentication methods

| Name | How | Used by |
| --- | --- | --- |
| Admin key | `X-Admin-Key: <Security:AdminApiKey>` | Creating application clients |
| App credentials, confidential client | `X-App-Id: <id>` and `X-Api-Key: <rawApiKey>` | Register, login, logout from a server |
| App credentials, public client | `X-App-Id: <id>`, and the browser's `Origin` header must be one of the app's `allowedOrigins` | Register, login, logout from a browser app |
| Access token | `Authorization: Bearer <accessToken>` | Logout, me, sessions |
| Refresh cookie | `refreshToken` cookie (`HttpOnly`, `Secure`, `SameSite=Strict`), set by register, login and refresh | Refresh, and marking the current session |

Browsers send the cookie automatically on same-origin requests. From another origin, send requests with credentials (`credentials: 'include'` or `withCredentials: true`) and add the origin to `Cors:AllowedOrigins`.

## Application clients

### Create an application client

`POST /api/application-client`

**Requires:** `X-Admin-Key`

| Field | Type | Notes |
| --- | --- | --- |
| `name` | string, required | Up to 100 characters |
| `audience` | string, optional | The `aud` your API will check, such as `notes-api`. Defaults to the app ID. |
| `isPublic` | boolean, optional | `true` for browser apps. Default `false`. |
| `allowedOrigins` | string[] | Required when `isPublic` is `true`. `scheme://host[:port]`, no path. |

Confidential client:

```bash
curl -X POST http://localhost:5183/api/application-client \
  -H "Content-Type: application/json" -H "X-Admin-Key: <admin key>" \
  -d '{ "name": "Notes backend", "audience": "notes-api" }'
```

```json
{
  "id": "6f1c1c1e-6b2a-4c11-9a39-3c3f7d2b9a10",
  "audience": "notes-api",
  "isPublic": false,
  "allowedOrigins": [],
  "rawApiKey": "q0V8...base64...="
}
```

Public client:

```bash
curl -X POST http://localhost:5183/api/application-client \
  -H "Content-Type: application/json" -H "X-Admin-Key: <admin key>" \
  -d '{ "name": "Notes web", "audience": "notes-api", "isPublic": true, "allowedOrigins": ["http://localhost:5173"] }'
```

```json
{
  "id": "0b8d9a5e-2f7e-4c55-8d0a-1f3b2a7c9e44",
  "audience": "notes-api",
  "isPublic": true,
  "allowedOrigins": ["http://localhost:5173"],
  "rawApiKey": null
}
```

**Notes**

- `rawApiKey` is shown once. Only its HMAC hash is stored, so a lost key can't be recovered; create a new app.
- Origins are normalized to lowercase without a trailing slash.
- **Errors:** 401 if the admin key is missing or wrong, or if `Security:AdminApiKey` isn't configured. 400 for a public client without origins, or an invalid origin.

## Register

`POST /api/auth/register`

**Requires:** app credentials

```json
{
  "fullName": "Jane Doe",
  "email": "jane@example.com",
  "city": "Colombo",
  "password": "Passw0rd!"
}
```

**Response:** `200 OK`, plus the `refreshToken` cookie

```json
{
  "id": "4c4939c5-0f70-4ac2-8a24-9d33cd45f773",
  "fullName": "Jane Doe",
  "userName": "jane@example.com",
  "email": "jane@example.com",
  "city": "Colombo",
  "accessToken": "eyJhbGciOiJSUzI1NiIs..."
}
```

**Rules**

- `fullName` and `city`: required, up to 100 characters. `email`: required, valid, up to 256 characters.
- Password: at least 6 characters, with a digit, a lowercase letter, an uppercase letter, and a character that is neither a letter nor a digit.
- Emails are unique per app and compared case-insensitively. The same email can register in another app.

**Errors:** 400 with the failed password rules in `message`, 401 for bad app credentials, 409 if the email is taken in this app, 429 when rate limited.

## Login

`POST /api/auth/login`

**Requires:** app credentials

```json
{ "email": "jane@example.com", "password": "Passw0rd!" }
```

**Response:** same as register, `200 OK` plus the `refreshToken` cookie. Each login starts a new session.

**Errors:** 401 for a wrong email or password (the message doesn't say which), 401 for bad app credentials, 423 when the account is locked, 429 when rate limited.

## Refresh

`POST /api/auth/refresh`

**Requires:** the `refreshToken` cookie. No body, no app credentials, no access token.

```bash
curl -X POST http://localhost:5183/api/auth/refresh -b cookies.txt -c cookies.txt
```

**Response:** `200 OK`, plus a new `refreshToken` cookie

```json
{ "accessToken": "eyJhbGciOiJSUzI1NiIs..." }
```

**Notes**

- The presented token is revoked and replaced. Store the new cookie: the old one won't work again.
- If an already-rotated token is presented more than 30 seconds after its rotation, every token from that login is revoked (reuse detection). Within 30 seconds the request just fails with 401, which covers two tabs refreshing at once.
- Don't send two refreshes with the same cookie in parallel; only one succeeds.

**Errors:** 401 if the cookie is missing, unknown, revoked or expired. 429 when rate limited.

## Logout

`POST /api/auth/logout`

**Requires:** access token and app credentials. Send the cookie too.

```bash
curl -X POST http://localhost:5183/api/auth/logout -b cookies.txt \
  -H "Authorization: Bearer <accessToken>" -H "X-App-Id: <id>" -H "X-Api-Key: <rawApiKey>"
```

**Response:** `204 No Content`. The current session's refresh token is revoked and the cookie is cleared. Other devices stay signed in. Logging out twice, or without a cookie, also returns 204.

**Errors:** 401 for a missing or expired access token, or bad app credentials.

## Current user

`GET /api/auth/me`

**Requires:** access token

```json
{
  "id": "4c4939c5-0f70-4ac2-8a24-9d33cd45f773",
  "fullName": "Jane Doe",
  "userName": "jane@example.com",
  "email": "jane@example.com",
  "city": "Colombo",
  "appId": "0b8d9a5e-2f7e-4c55-8d0a-1f3b2a7c9e44",
  "roles": ["User"]
}
```

A browser app calls this after a page reload: refresh gives it an access token, and `/me` gives it the profile.

**Errors:** 401 without a valid access token, 404 if the user was deleted.

## Sessions

### List sessions

`GET /api/auth/sessions`

**Requires:** access token. Send the cookie to get `isCurrent`.

```json
[
  {
    "tokenId": 12,
    "device": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) ... Chrome/140.0 Safari/537.36",
    "ipAddress": "203.0.113.7",
    "issuedAt": "2026-10-25T08:00:00Z",
    "expiresAt": "2026-11-01T08:00:00Z",
    "isCurrent": true
  }
]
```

Newest first. Only unexpired, unrevoked sessions are listed. `tokenId` changes each time a session refreshes, because it identifies the session's current token.

### Revoke a session

`DELETE /api/auth/sessions/{tokenId}`

**Requires:** access token

**Response:** `204 No Content`. That device can't refresh again. Its current access token stays valid until it expires (at most 15 minutes).

To end the current session, use [logout](#logout) instead so the cookie is cleared.

**Errors:** 404 if the session doesn't exist, is already revoked, or belongs to another user.

## JWKS

`GET /.well-known/jwks.json`

```json
{
  "keys": [
    {
      "kty": "RSA",
      "use": "sig",
      "kid": "urk1B6XCC9deOFnd8OV3RDC7kM4TXUJmIvzUEBOaEwc",
      "alg": "RS256",
      "n": "...",
      "e": "AQAB"
    }
  ]
}
```

The `kid` is the key's RFC 7638 thumbprint, so it changes whenever the key does. Cache the key set and refetch when a token's `kid` isn't in it.

## Errors

Errors thrown by the service return:

```json
{ "message": "Invalid username or password." }
```

Debug builds add a `detail` field to 500 responses. Invalid request bodies are rejected by ASP.NET's model validation with a standard problem-details response:

```json
{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": { "Email": ["The Email field is not a valid e-mail address."] }
}
```

| Status | Meaning |
| --- | --- |
| 400 | Invalid input, or password rules not met |
| 401 | Not authenticated: wrong password, bad app credentials or origin, bad admin key, missing/expired access token, invalid refresh token |
| 404 | User or session not found |
| 409 | Email already registered in this app |
| 423 | Account locked after too many failed logins |
| 429 | Rate limit exceeded |
| 500 | Unexpected server error |

## Rate limits and lockout

- **Rate limits** are per client IP, in fixed windows. Login and register share one budget (default 10 per minute); refresh has its own (default 30 per minute). Exceeding it returns 429 with `{ "message": "Too many requests. Try again later." }`. Configure with `RateLimiting:Credentials` and `RateLimiting:Refresh`.
- **Lockout:** 5 wrong passwords in a row lock the account for 15 minutes. While locked, login returns 423 even with the right password.

Behind a reverse proxy, set `ForwardedHeaders:KnownNetworks` so the client's IP is used instead of the proxy's.

## Validating tokens in your own API

Access tokens are RS256 JWTs with a 15-minute lifetime. Validate:

- the signature, against the key in the JWKS whose `kid` matches the token header;
- `iss` equals the auth service's `JWT:Issuer`;
- `aud` contains **your** app's audience (such as `notes-api`);
- `exp`, with little or no clock skew.

Useful claims: `nameid` (user ID), `email`, `unique_name`, `role` (one per role), `app_id`.

ASP.NET Core example:

```csharp
var jwks = new JsonWebKeySet(await new HttpClient().GetStringAsync("http://localhost:5183/.well-known/jwks.json"));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = "http://localhost:5183",
            ValidAudience = "notes-api",
            IssuerSigningKeys = jwks.GetSigningKeys(),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
```

This fetches the keys once at startup. Until the service publishes an OpenID discovery document, restart your API after the signing key changes.
