# TODO

Sections are in priority order. Section 1 is urgent; the frontend (section 6) works best after sections 2–4, because it depends on a stable, tested API.

## 1. Rotate leaked secrets (urgent)

[auth.webapi/appsettings.json](auth.webapi/appsettings.json) is committed with real credentials. They stay in git history even after the file changes, so rotating them is what matters.

- [ ] Reset the Neon Postgres password.
- [ ] Revoke the Gmail app password and create a new one.
- [ ] Generate a new `Security:ApiKeySalt`. This invalidates existing API keys, so recreate any application clients.
- [ ] Replace the values in `appsettings.json` with placeholders. Use user secrets locally and environment variables in Docker/Kubernetes.
- [ ] Delete or rebuild the `tharindu007/dotnet-auth` Docker Hub image, which contains the old `appsettings.json`.
- [ ] Optional: purge the old values from history with `git filter-repo`.

## 2. Security and bug fixes

- [ ] **Persist and share the RSA signing key.** Load it from a Kubernetes secret or key vault so it survives restarts and is the same on every replica. Load it in one place; today both [ServiceExtentions.cs](auth.webapi/Extentions/ServiceExtentions.cs) and [TokenService.cs](auth.webapi/Services/TokenService.cs) load or generate it.
- [ ] **Stop logging API keys.** [AccountController.cs](auth.webapi/Controllers/AccountController.cs) logs the raw `X-Api-Key`, and [AuthService.cs](auth.webapi/Services/AuthService.cs) logs the hash.
- [ ] **Hash refresh tokens** before storing them, the same way API keys are hashed.
- [ ] **Add refresh-token reuse detection.** Group rotated tokens into a family. If a revoked token is presented again, revoke the whole family, because it means a token was stolen.
- [ ] **Fix the login email case bug.** Compare `NormalizedEmail` with `_userManager.NormalizeEmail(input)` instead of `Email == input.ToLower()`.
- [ ] **Allow the same email in different apps.** Identity's username index is unique across the whole database. Either make usernames app-scoped, or replace that index with a composite `(ApplicationClientId, NormalizedUserName)` index plus a custom `UserValidator`.
- [ ] **Protect `POST /api/application-client`**, for example with an `Admin` role or a bootstrap secret.
- [ ] **Verify the API key on logout**, or stop requiring the app headers there.
- [ ] **Map `ApplicationClientAuthenticationException` to 401** in [ExceptionStatusCodeMapper.cs](auth.webapi/Helpers/ExceptionStatusCodeMapper.cs). It currently falls back to 400.
- [ ] **Add role claims to the JWT** so `[Authorize(Roles = "...")]` works.
- [ ] **Add rate limiting** to login, register, and refresh with the built-in `Microsoft.AspNetCore.RateLimiting` middleware.
- [ ] **Turn on account lockout** by passing `lockoutOnFailure: true` to `CheckPasswordSignInAsync`.
- [ ] **Add a `Created` column to `RefreshToken`** and use it to fill `issuedAt` in the sessions response, which is always `0001-01-01` today. This needs a migration, so fix the model snapshot first (section 4).

## 3. Tests

- [ ] Rewrite [AuthServiceTests.cs](auth.unitTest/Auth/AuthServiceTests.cs) and delete the empty `UnitTest1`. `Mock<ApplicationDbContext>` can't work, because the context has no parameterless constructor, so use SQLite in-memory or a real Postgres.
- [ ] Add integration tests with `WebApplicationFactory` and Testcontainers Postgres covering register → login → refresh → reuse of the old refresh token (expect 401) → logout → sessions.
- [ ] Cover the failure paths: wrong app credentials, duplicate email, wrong password, expired or revoked refresh token.
- [ ] Run build and tests in GitHub Actions. The `.github/` folder exists but is empty.

## 4. Code cleanup

- [ ] Move the `X-App-Id`/`X-Api-Key` parsing into an action filter or middleware. It's copy-pasted three times in [AccountController.cs](auth.webapi/Controllers/AccountController.cs).
- [ ] Pull the app credential check in [AuthService.cs](auth.webapi/Services/AuthService.cs) into one method. `LoginUserAsync` and `RegisterUserAsync` each have their own copy.
- [ ] Remove the debug code: the token-expiry middleware in [Program.cs](auth.webapi/Program.cs), and the `Console.WriteLine` calls in `TokenService`, `AuthService.LogoutAsync`, and `AccountController.GetSessions`. Use `ILogger` for anything worth keeping.
- [ ] Remove the manual `iat` claim in `TokenService.CreateToken`. The token handler already sets `iat`, and the manual one is a formatted date string instead of a Unix timestamp.
- [ ] Remove the duplicate `app.UseSwagger()` call in `Program.cs`.
- [ ] Remove the unused `JWT:SigningKey` setting, which is left over from HMAC signing.
- [ ] Rename the `AllowAllOrigins` CORS policy, since it only allows `http://localhost:3000`, and read the allowed origins from config.
- [ ] Stop ignoring `Migrations/*Snapshot.cs` in [auth.webapi/.gitignore](auth.webapi/.gitignore), then regenerate and commit the model snapshot.
- [ ] Change `EXPOSE 80` to `EXPOSE 8080` in the [Dockerfile](Dockerfile).
- [ ] Replace the `/weatherforecast` request in [auth.webapi.http](auth.webapi/auth.webapi.http) with real requests for this API.
- [ ] Add a root `.gitignore` and untrack the committed `.vs/` folders (`git rm -r --cached .vs auth.unitTest/.vs`).

## 5. Docs

- [ ] Delete [auth.webapi/README.md](auth.webapi/README.md), an outdated copy of the root README, or replace it with a link to the root one.
- [ ] Update [auth.webapi/Docs/API.md](auth.webapi/Docs/API.md):
  - Refresh no longer needs an `Authorization` header.
  - The prerequisite is .NET 9, not .NET 8.
  - Tokens are signed with RSA (RS256), not with a configured signing key.
  - Error bodies have no `statusCode` field.
  - Fix the "Last Updated: January 2024" line.
- [ ] Add a `LICENSE` file. The README says MIT, but the repo has no license file.
- [ ] Update the README's Known limitations as items here get fixed.

## 6. Frontend (React + Vite)

### Setup

- [ ] Scaffold it in `frontend/` with `npm create vite@latest frontend -- --template react-ts`.
- [ ] Use Vite's `server.proxy` to forward `/api` and `/.well-known` to `http://localhost:5183`. The browser then sees a single origin, which avoids CORS and cookie problems in development. The alternative is `server.port: 3000`, which matches the current CORS policy.

### Decide first

- [ ] **Decide how a browser app identifies itself.** Anything in a JavaScript bundle is public, so an `X-Api-Key` shipped in a SPA isn't a secret. Options:
  - Add a "public client" type that needs only the App ID and is restricted by an allowed-origins list.
  - Put a small backend-for-frontend in front of the API to hold the key.

### Backend changes the frontend needs

- [ ] `GET /api/auth/me`. After a page reload, `/refresh` returns only an access token, so the frontend has no way to get the user's profile.
- [ ] `DELETE /api/auth/sessions/{id}`, so users can sign out other devices from the sessions page.

### Auth handling

- [ ] Keep the access token in memory (React context), never in `localStorage`.
- [ ] On app start, call `/api/auth/refresh` to restore the session from the cookie.
- [ ] On a 401, refresh once and retry the request. Share one in-flight refresh promise across requests. Refresh tokens rotate, so two parallel refreshes would send the same token twice, and the second would fail and log the user out (or trip reuse detection).
- [ ] If the API is called cross-origin, send `credentials: 'include'` on refresh and logout.
- [ ] Optional: refresh shortly before the 15-minute expiry instead of waiting for a 401.

### Pages

- [ ] Register and login forms, with client-side checks that match the password rules (6+ characters, a digit, an uppercase letter, a lowercase letter, a symbol).
- [ ] A sessions page that lists device, IP, and expiry, with a revoke button.
- [ ] Logout.
- [ ] A protected-route wrapper for signed-in pages.
- [ ] A dev-only page that creates an application client.
- [ ] Show API errors from the `{ "message": "..." }` response body.

### Quality and deployment

- [ ] Tests with Vitest and React Testing Library.
- [ ] Host the frontend and API on the same site, for example `app.example.com` and `api.example.com`. The refresh cookie is `SameSite=Strict`, so browsers won't send it if the two are on unrelated domains. That includes separate `*.vercel.app` or `*.netlify.app` subdomains.
- [ ] Add the frontend to Docker/Kubernetes, or deploy it as a static site.

## 7. Later / optional

- [ ] **Password reset and email confirmation.** Identity's token providers are already registered with a 2-hour lifetime, but nothing uses them yet.
- [ ] **OpenID discovery document** at `/.well-known/openid-configuration`, so downstream ASP.NET services can validate tokens by setting `options.Authority`.
- [ ] **Signing key rotation.** Publish both the old and new keys in the JWKS during a rollover.
- [ ] **CQRS**, but only if an admin or reporting side is added, such as login analytics per app or audit logs. It isn't worth the overhead for the current endpoints.
