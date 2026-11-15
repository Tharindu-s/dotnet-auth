# TODO

Sections are in priority order. Sections 2–6 are done; sections 1 and 7 are still open.

## 1. Secrets

The leaked credentials are dead, and the history was rewritten with `git filter-repo` before the repo went public.

- [ ] Turn on secret scanning and push protection: Settings → Code security.
- [ ] Rebuild and push a new Docker image when you need one. Pass secrets as environment variables, never in `appsettings.json`.

## 2. Security and bug fixes

- [x] **Persist and share the RSA signing key.** Load it from a Kubernetes secret or key vault so it survives restarts and is the same on every replica. Load it in one place; today both [ServiceExtentions.cs](auth.webapi/Extentions/ServiceExtentions.cs) and [TokenService.cs](auth.webapi/Services/TokenService.cs) load or generate it. Give each new key a new `kid`. Every key is currently called `dev-key`, so services that cached an old public key have no way to tell the key changed.
- [x] **Hash refresh tokens** before storing them, the same way API keys are hashed.
- [x] **Add refresh-token reuse detection.** Group rotated tokens into a family. If a revoked token is presented again, revoke the whole family, because it means a token was stolen.
- [x] **Fix the login email case bug.** Compare `NormalizedEmail` with `_userManager.NormalizeEmail(input)` instead of `Email == input.ToLower()`.
- [x] **Allow the same email in different apps.** Identity's username index is unique across the whole database. Either make usernames app-scoped, or replace that index with a composite `(ApplicationClientId, NormalizedUserName)` index plus a custom `UserValidator`.
- [x] **Protect `POST /api/application-client`**, for example with an `Admin` role or a bootstrap secret.
- [x] **Verify the API key on logout**, or stop requiring the app headers there.
- [x] **Clear the cookie on logout.** `Logout` revokes the refresh token in the database but never calls `Response.Cookies.Delete("refreshToken")`, so the browser keeps a dead cookie.
- [x] **Map `ApplicationClientAuthenticationException` to 401** in [ExceptionStatusCodeMapper.cs](auth.webapi/Helpers/ExceptionStatusCodeMapper.cs). It currently falls back to 400.
- [x] **Add role claims to the JWT** so `[Authorize(Roles = "...")]` works.
- [x] **Give tokens a real audience.** `aud` is always the auth service's own URL, so any API that trusts this service accepts every token. Set `aud` to the API the token is meant for, such as `notes-api`, so each API only accepts its own tokens.
- [x] **Add an `app_id` claim to the JWT.** Tokens don't say which application client the user belongs to, so an API for app B accepts tokens issued to app A's users. With the claim, or a separate audience per app, APIs can reject tokens from other apps.
- [x] **Add rate limiting** to login, register, and refresh with the built-in `Microsoft.AspNetCore.RateLimiting` middleware.
- [x] **Turn on account lockout** by passing `lockoutOnFailure: true` to `CheckPasswordSignInAsync`.
- [x] **Add a `Created` column to `RefreshToken`** and use it to fill `issuedAt` in the sessions response, which is always `0001-01-01` today. This needs a migration, so fix the model snapshot first (section 4).

## 3. Tests

- [x] Rewrite [AuthServiceTests.cs](auth.unitTest/Auth/AuthServiceTests.cs) and delete the empty `UnitTest1`. `Mock<ApplicationDbContext>` can't work, because the context has no parameterless constructor, so use SQLite in-memory or a real Postgres.
- [x] Add integration tests with `WebApplicationFactory` and Testcontainers Postgres covering register → login → refresh → reuse of the old refresh token (expect 401) → logout → sessions.
- [x] Cover the failure paths: wrong app credentials, duplicate email, wrong password, expired or revoked refresh token.
- [x] Run build and tests in GitHub Actions. The `.github/` folder exists but is empty.

## 4. Code cleanup

- [x] Move the `X-App-Id`/`X-Api-Key` parsing into an action filter or middleware. It's copy-pasted three times in [AccountController.cs](auth.webapi/Controllers/AccountController.cs).
- [x] Pull the app credential check in [AuthService.cs](auth.webapi/Services/AuthService.cs) into one method. `LoginUserAsync` and `RegisterUserAsync` each have their own copy.
- [x] Remove the debug code: the token-expiry middleware in [Program.cs](auth.webapi/Program.cs), and the `Console.WriteLine` calls in `TokenService`, `AuthService.LogoutAsync`, and `AccountController.GetSessions`. Use `ILogger` for anything worth keeping.
- [x] Remove the manual `iat` claim in `TokenService.CreateToken`. The token handler already sets `iat`, and the manual one is a formatted date string instead of a Unix timestamp.
- [x] Rename the `AllowAllOrigins` CORS policy, since it only allows `http://localhost:3000`, and read the allowed origins from config.
- [x] Stop ignoring `Migrations/*Snapshot.cs` in [auth.webapi/.gitignore](auth.webapi/.gitignore), then regenerate and commit the model snapshot.
- [x] Change `EXPOSE 80` to `EXPOSE 8080` in the [Dockerfile](Dockerfile).
- [x] Replace the `/weatherforecast` request in [auth.webapi.http](auth.webapi/auth.webapi.http) with real requests for this API.

## 5. Docs

- [x] Delete [auth.webapi/README.md](auth.webapi/README.md), an outdated copy of the root README, or replace it with a link to the root one.
- [x] Update [auth.webapi/Docs/API.md](auth.webapi/Docs/API.md):
  - Refresh no longer needs an `Authorization` header.
  - The prerequisite is .NET 9, not .NET 8.
  - Tokens are signed with RSA (RS256), not with a configured signing key.
  - Error bodies have no `statusCode` field.
  - Fix the "Last Updated: January 2024" line.
- [x] Add a `LICENSE` file. The README says MIT, but the repo has no license file.
- [x] Update the README's Known limitations as items here get fixed.

## 6. Frontend (React + Vite)

### Setup

- [x] Scaffold it in `frontend/` with `npm create vite@latest frontend -- --template react-ts`.
- [x] Use Vite's `server.proxy` to forward `/api` and `/.well-known` to `http://localhost:5183`. The browser then sees a single origin, which avoids CORS and cookie problems in development. The alternative is `server.port: 3000`, which matches the current CORS policy.

### Decide first

- [x] **Decide how a browser app identifies itself.** Chose a public client type (App ID + allowed origins). Anything in a JavaScript bundle is public, so an `X-Api-Key` shipped in a SPA isn't a secret. Options:
  - Add a "public client" type that needs only the App ID and is restricted by an allowed-origins list.
  - Put a small backend-for-frontend in front of the API to hold the key.

### Backend changes the frontend needs

- [x] `GET /api/auth/me`. After a page reload, `/refresh` returns only an access token, so the frontend has no way to get the user's profile.
- [x] `DELETE /api/auth/sessions/{id}`, so users can sign out other devices from the sessions page.

### Auth handling

- [x] Keep the access token in memory (React context), never in `localStorage`.
- [x] On app start, call `/api/auth/refresh` to restore the session from the cookie.
- [x] On a 401, refresh once and retry the request. Share one in-flight refresh promise across requests. Refresh tokens rotate, so two parallel refreshes would send the same token twice, and the second would fail and log the user out (or trip reuse detection).
- [x] If the API is called cross-origin, send `credentials: 'include'` on refresh and logout.
- [x] Optional: refresh shortly before the 15-minute expiry instead of waiting for a 401.

### Pages

- [x] Register and login forms, with client-side checks that match the password rules (6+ characters, a digit, an uppercase letter, a lowercase letter, a symbol).
- [x] A sessions page that lists device, IP, and expiry, with a revoke button.
- [x] Logout.
- [x] A protected-route wrapper for signed-in pages.
- [x] A dev-only page that creates an application client.
- [x] Show API errors from the `{ "message": "..." }` response body.

### Quality and deployment

- [x] Tests with Vitest and React Testing Library.
- [x] Host the frontend and API on the same site, for example `app.example.com` and `api.example.com`. The refresh cookie is `SameSite=Strict`, so browsers won't send it if the two are on unrelated domains. That includes separate `*.vercel.app` or `*.netlify.app` subdomains.
- [x] Add the frontend to Docker/Kubernetes, or deploy it as a static site.

## 7. Later / optional

- [ ] **Password reset and email confirmation.** Identity's token providers are already registered with a 2-hour lifetime, but nothing uses them yet.
- [ ] **OpenID discovery document** at `/.well-known/openid-configuration`, so downstream ASP.NET services can validate tokens by setting `options.Authority`. Without it, a second service has to fetch the JWKS by hand (usually once, at startup), fails to start if the auth service is down, and doesn't pick up new keys. Do this as soon as a second service needs to validate tokens.
- [ ] **Signing key rotation.** Publish both the old and new keys in the JWKS during a rollover.
- [ ] **CQRS**, but only if an admin or reporting side is added, such as login analytics per app or audit logs. It isn't worth the overhead for the current endpoints.
