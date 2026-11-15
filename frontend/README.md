# Argus frontend

**Argus** is the React app for the auth service: login, registration, profile, and a sessions page where users can sign out other devices.

Stack: React 19, Vite, TypeScript, Zustand, TanStack Query, React Router, Axios, React Hook Form + Zod, Tailwind CSS + shadcn/ui. Tests use Vitest, Testing Library and MSW.

## Run it

1. Start the API on its HTTP port: `dotnet run --launch-profile http` in `auth.webapi/`.
2. Start the frontend:

   ```bash
   npm install
   npm run dev          # http://localhost:5173
   ```

3. The first time, open <http://localhost:5173/dev/apps> and create a **public** application client. You'll need the API's `Security:AdminApiKey`; the allowed origin is pre-filled. Copy `.env.example` to `.env.local`, set `VITE_APP_ID` to the new app's ID, and restart `npm run dev`.

| Script | What it does |
| --- | --- |
| `npm run dev` | Dev server with the API proxy |
| `npm test` | Run the tests once (`npm run test:watch` to watch) |
| `npm run lint` | oxlint |
| `npm run typecheck` | `tsc -b` |
| `npm run build` | Type-check and build to `dist/` |

## How it talks to the API

In development, Vite proxies `/api` and `/.well-known` to `http://localhost:5183` (override with `API_PROXY_TARGET` in `.env.local`). In Docker, nginx does the same. Either way the browser sees a single origin, so there's no CORS, and the `SameSite=Strict` refresh cookie is always sent. Browsers accept that `Secure` cookie on `http://localhost`, but anywhere else it needs HTTPS.

The app is a **public client**: it sends `X-App-Id` and no API key, and the API checks that the browser's `Origin` is in the app's allowed origins.

## State

| What | Where | Why |
| --- | --- | --- |
| Auth status, user, access token | Zustand ([src/stores/auth.ts](src/stores/auth.ts)) | Client state that the Axios interceptors and route guards read outside React |
| Server data (profile from `/me`, sessions) | TanStack Query ([src/features/sessions/queries.ts](src/features/sessions/queries.ts)) | Caching, loading/error states, refetch after a mutation |
| Form state and validation | React Hook Form + Zod ([src/features/auth/schemas.ts](src/features/auth/schemas.ts)) | The password rules match the API's |

The access token lives **only in memory**, never in `localStorage`. The refresh token is an `HttpOnly` cookie that JavaScript can't read.

## Session lifecycle

All in [src/lib/api.ts](src/lib/api.ts) and [src/features/auth/session.ts](src/features/auth/session.ts):

- **Startup:** the status starts as `loading`. `restoreSession()` calls `/api/auth/refresh`; if the cookie is valid it fetches `/me` and the user is signed in, otherwise anonymous. Route guards show a spinner until then.
- **Requests:** an Axios request interceptor adds `Authorization: Bearer <token>` and `X-App-Id`.
- **401s:** the response interceptor refreshes once and retries the request. If the refresh itself returns 401, the session is over and the store is cleared, so the guards redirect to `/login`. Other refresh failures (offline, 429) don't log the user out.
- **One refresh at a time.** Refresh tokens rotate, so the same cookie must never be sent twice:
  - within a tab, concurrent 401s share one in-flight refresh promise;
  - across tabs, `navigator.locks` makes tabs take turns, and each tab's request carries the cookie the previous tab just received.
- **Proactive refresh:** a timer refreshes the token a minute before it expires, so most requests never see a 401.
- **Other tabs:** a `BroadcastChannel` tells other tabs about logins and logouts. Logging out in one tab signs out all of them.

## Layout

```
src/
├── components/        # AppLayout, ArgusLogo (SVG mark), brand pieces (header, color blocks), FormField, ui/ (shadcn)
├── features/
│   ├── auth/          # Login, register, route guards, session actions, schemas
│   ├── profile/       # Profile page
│   ├── sessions/      # Sessions page and queries
│   └── dev/           # Create-app page, only in dev builds
├── lib/               # Axios client, config, errors, cross-tab channel, query client
├── stores/auth.ts     # Zustand auth store
├── router.tsx
└── main.tsx           # Providers, startup refresh, cross-tab sync
```

The logo source files are in `public/Logos`. The header and favicon use an SVG redrawn from them ([src/components/ArgusLogo.tsx](src/components/ArgusLogo.tsx), [public/favicon.svg](public/favicon.svg)): the standard mark at 48 px and up, and the heavier small-size cut at 32 px and below. `public/apple-touch-icon.png` is a copy of the app icon.

The `/dev/apps` route is added only when `import.meta.env.DEV` is true, so production builds don't contain it. The admin key typed there is sent with that one request and never stored.

## Docker

```bash
docker build -t dotnet-auth-frontend --build-arg VITE_APP_ID=<public app id> .
docker run -p 8081:8080 -e API_UPSTREAM=http://host.docker.internal:5183 dotnet-auth-frontend
```

The image is nginx serving `dist/` and proxying `/api` and `/.well-known` to `API_UPSTREAM` (default `http://dotnet-auth:8080`, the Kubernetes service). Add the URL users open to the public app's `allowedOrigins`.
