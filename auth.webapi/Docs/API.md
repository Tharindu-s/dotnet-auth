# Auth API reference

Every endpoint of the auth service, with request and response examples. For setup, configuration and the design behind it, see the [root README](../../README.md).

- **Base URL (development):** `http://localhost:5183` (or `https://localhost:7023` with the `https` profile)
- **Content type:** `application/json`
- **Swagger UI:** `/swagger`, with fields for the custom headers

## Contents

1. [Authentication methods](#authentication-methods)
2. [Register](#register)
3. [Login](#login)
4. [Refresh](#refresh)
5. [Logout](#logout)
6. [Current user](#current-user)
7. [Change password](#change-password)
8. [Sessions](#sessions)
9. [JWKS](#jwks)
10. [Admin console API](#admin-console-api)
11. [Errors](#errors)
12. [Rate limits and lockout](#rate-limits-and-lockout)
13. [Validating tokens in your own API](#validating-tokens-in-your-own-api)

## Authentication methods

| Name | How | Used by |
| --- | --- | --- |
| App credentials, confidential client | `X-App-Id: <id>` and `X-Api-Key: <rawApiKey>` | Register, login, logout from a server |
| App credentials, public client | `X-App-Id: <id>`, and the browser's `Origin` header must be one of the app's `allowedOrigins` | Register, login, logout from a browser app |
| Access token | `Authorization: Bearer <accessToken>` | Logout, me, change password, sessions |
| Argus admin | An access token of an Argus user with the `Admin` role, checked against the database | The admin console API (`/api/apps`) |
| Refresh cookie | `refreshToken` cookie (`HttpOnly`, `Secure`, `SameSite=Strict`), set by register, login and refresh | Refresh, and marking the current session |

Browsers send the cookie automatically on same-origin requests. From another origin, send requests with credentials (`credentials: 'include'` or `withCredentials: true`) and add the origin to `Cors:AllowedOrigins`.

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

**Errors:** 400 with the failed password rules in `message`, 401 for bad app credentials, 403 if the app has sign-ups turned off, 409 if the email is taken in this app, 429 when rate limited.

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
  "roles": ["User"],
  "mustChangePassword": false
}
```

A browser app calls this after a page reload: refresh gives it an access token, and `/me` gives it the profile. `mustChangePassword` is true only for the seeded Argus admin until they choose their own password.

**Errors:** 401 without a valid access token, 404 if the user was deleted.

## Change password

`POST /api/auth/change-password`

**Requires:** access token. Send the cookie too, so the current session is kept.

```json
{ "currentPassword": "Passw0rd!", "newPassword": "N3w-Passw0rd!" }
```

**Response:** `204 No Content`. Every other session of the user is revoked; the session whose cookie came with the request stays signed in. Clears `mustChangePassword`.

**Errors:** 400 if the current password is wrong, the new one breaks the password rules, or it's the same as the current one. 401 without a valid access token. 429 when rate limited (it shares the login budget).

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

## Admin console API

These endpoints back the Argus console. They need an access token from an **Argus admin**: a user of the Argus app (the service's own app, created on first start) with the `Admin` role, not locked out, and without a pending password change. An `Admin` from any other app gets 403.

The Argus app itself never appears here and can't be changed through these endpoints (404).

### Console config

`GET /api/argus/config` (no authentication)

```json
{ "appId": "e2803400-5f7f-4baf-b4d6-139f26b16d17", "issuer": "http://localhost:5183" }
```

The console reads the Argus app ID from here, so it isn't baked into the frontend build.

### Apps

| Method | Route | Body | Response |
| --- | --- | --- | --- |
| GET | `/api/apps` | | `App[]`, oldest first |
| GET | `/api/apps/{id}` | | `App` |
| POST | `/api/apps` | `{ name, audience?, isPublic?, allowedOrigins?, registrationEnabled? }` | `201 Created`, `App` plus `rawApiKey` |
| PUT | `/api/apps/{id}` | `{ name, audience?, allowedOrigins?, registrationEnabled }` | `App` |
| POST | `/api/apps/{id}/rotate-key` | | `{ rawApiKey }` |
| DELETE | `/api/apps/{id}` | | `204`. Also deletes the app's users and sessions. |

An `App`:

```json
{
  "id": "6f1c1c1e-6b2a-4c11-9a39-3c3f7d2b9a10",
  "name": "Notes backend",
  "audience": "notes-api",
  "isPublic": false,
  "registrationEnabled": true,
  "allowedOrigins": [],
  "createdAt": "2026-11-15T10:00:00Z",
  "userCount": 12,
  "activeSessionCount": 7
}
```

Creating a confidential app (`isPublic` false, the default):

```bash
curl -X POST http://localhost:5183/api/apps \
  -H "Authorization: Bearer <admin access token>" -H "Content-Type: application/json" \
  -d '{ "name": "Notes backend", "audience": "notes-api" }'
```

**Notes**

- `rawApiKey` is returned once, on create and on rotate. Only its HMAC hash is stored. Rotating replaces the key immediately; public apps have no key (400).
- `audience` defaults to the app ID. Changing it makes the app's API reject tokens issued before the change.
- `allowedOrigins` is required for public apps. Origins are `scheme://host[:port]` with no path, normalized to lowercase without a trailing slash; blank entries are ignored.
- Whether an app is public can't be changed after it's created.
- With `registrationEnabled` false, register returns 403 and only existing users can sign in.

### Users of an app

| Method | Route | Response |
| --- | --- | --- |
| GET | `/api/apps/{id}/users?search=&page=1&pageSize=25` | `{ items, total, page, pageSize }`, sorted by email. `search` matches email or name, ignoring case. `pageSize` is at most 100. |
| POST | `/api/apps/{id}/users/{userId}/lock` | `204`. Blocks sign-in until unlocked and revokes all the user's sessions. |
| POST | `/api/apps/{id}/users/{userId}/unlock` | `204`. Also resets the failed-password count. |
| DELETE | `/api/apps/{id}/users/{userId}` | `204`. Deletes the user and their sessions. |

A user item:

```json
{
  "id": "4c4939c5-0f70-4ac2-8a24-9d33cd45f773",
  "email": "jane@example.com",
  "fullName": "Jane Doe",
  "city": "Colombo",
  "isLockedOut": false,
  "lockoutEnd": null,
  "activeSessionCount": 2,
  "lastSignInAt": "2026-11-15T09:12:00Z"
}
```

### Sessions of an app

| Method | Route | Response |
| --- | --- | --- |
| GET | `/api/apps/{id}/sessions?userId=` | Up to 200 active sessions, newest first: `{ tokenId, userId, userEmail, device, ipAddress, issuedAt, expiresAt }` |
| DELETE | `/api/apps/{id}/sessions/{tokenId}` | `204`. That device can't refresh again. |

**Errors (all admin endpoints):** 401 without a valid access token; 403 if the caller isn't an Argus admin or must change their password first; 423 if the admin is locked out; 404 for an unknown app, user or session (including the Argus app).

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
| 400 | Invalid input, password rules not met, or wrong current password |
| 401 | Not authenticated: wrong password, bad app credentials or origin, missing/expired access token, invalid refresh token |
| 403 | Not allowed: not an Argus admin, admin password change pending, or sign-ups turned off for the app |
| 404 | App, user or session not found |
| 409 | Email already registered in this app |
| 423 | Account locked (too many failed logins, or locked by an admin) |
| 429 | Rate limit exceeded |
| 500 | Unexpected server error |

## Rate limits and lockout

- **Rate limits** are per client IP, in fixed windows. Login, register and change password share one budget (default 10 per minute); refresh has its own (default 30 per minute). Exceeding it returns 429 with `{ "message": "Too many requests. Try again later." }`. Configure with `RateLimiting:Credentials` and `RateLimiting:Refresh`.
- **Lockout:** 5 wrong passwords in a row lock the account for 15 minutes. An admin can also lock an account until they unlock it. While locked, login returns 423 even with the right password.

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
