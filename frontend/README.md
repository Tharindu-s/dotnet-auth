# Argus

**Argus** is the admin console for the auth service. An admin signs in, creates an app for each project, and manages each app's settings, API key, users and sessions.

Stack: React 19, Vite, TypeScript, Zustand, TanStack Query, React Router, Axios, React Hook Form + Zod, Tailwind CSS v4 and [shadcn/ui](https://ui.shadcn.com) (style `radix-nova`). Tests use Vitest, Testing Library and MSW.

## Run it

With Docker, `docker compose up -d` from the repository root runs it at <http://localhost:8081>; see the root README.

Without Docker:

1. Start the API on its HTTP port: `dotnet run --launch-profile http` in `auth.webapi/`. On first start it creates the Argus app and the admin, and logs the admin's email (and password, if it generated one).
2. Start the console:

   ```bash
   npm install
   npm run dev          # http://localhost:5173
   ```

3. Sign in as the admin. The first sign-in asks you to choose a new password.

No app ID is configured: the console reads it from `GET /api/argus/config` when it starts.

| Script | What it does |
| --- | --- |
| `npm run dev` | Dev server with the API proxy |
| `npm test` | Run the tests once (`npm run test:watch` to watch) |
| `npm run lint` | oxlint |
| `npm run typecheck` | `tsc -b` |
| `npm run build` | Type-check and build to `dist/` |

## UI components

Everything is built from shadcn/ui components in [src/components/ui](src/components/ui), added with the shadcn CLI (`npx shadcn add <name>`), not written by hand: Sidebar, Tabs, Field, Input Group, Item, Empty, Alert Dialog, Dropdown Menu, Radio Group, Switch, Pagination, Breadcrumb, Avatar, Table, Spinner and others. To add one, run `npx shadcn add <name>`, and answer **no** when it asks to overwrite existing files.

The look lives in [src/index.css](src/index.css): a gray backdrop with a peach glow (`.app-backdrop`), the app in a rounded frame (`bg-frame`), white cards with `shadow-soft`, pill-shaped navigation and dark pill buttons. The shadcn components read the color tokens defined there.

A few small helpers compose those components: `FormField` (Field + Input), `CopyInput` (Field + Input Group with a copy button), `AppLayout` (Sidebar shell, `Page`, `PageHeader`) and `AppSidebar`.

## How it talks to the API

In development, Vite proxies `/api` and `/.well-known` to `http://localhost:5183` (override with `API_PROXY_TARGET` in `.env.local`). In Docker, nginx does the same. Either way the browser sees a single origin, so there's no CORS, and the `SameSite=Strict` refresh cookie is always sent. Browsers accept that `Secure` cookie on `http://localhost`, but anywhere else it needs HTTPS.

The console is a **public client** of the Argus app: login and logout send `X-App-Id` and no API key, and the API checks that the browser's `Origin` is in `Argus:AllowedOrigins`. The admin endpoints (`/api/apps/...`) just need the access token.

## State

| What | Where | Why |
| --- | --- | --- |
| Auth status, user (with roles and `mustChangePassword`), access token | Zustand ([src/stores/auth.ts](src/stores/auth.ts)) | Client state that the Axios interceptors and route guards read outside React |
| Server data (apps, users, sessions) | TanStack Query ([src/features/apps/queries.ts](src/features/apps/queries.ts)) | Caching, loading/error states, refetch after a mutation |
| Form state and validation | React Hook Form + Zod | The password rules match the API's |

The access token lives **only in memory**, never in `localStorage`. The refresh token is an `HttpOnly` cookie that JavaScript can't read.

## Session lifecycle

All in [src/lib/api.ts](src/lib/api.ts) and [src/features/auth/session.ts](src/features/auth/session.ts):

- **Startup:** the status starts as `loading`. `restoreSession()` calls `/api/auth/refresh`; if the cookie is valid it fetches `/me` and the user is signed in, otherwise anonymous. Route guards show a spinner until then.
- **Sign-in:** after login, `/me` is fetched before the session starts, so the guards know the roles and whether a password change is pending.
- **Guards:** signed-out users go to `/login`. An admin with `mustChangePassword` is held on `/change-password`. A non-admin sees a "No access" page.
- **401s:** the response interceptor refreshes once and retries the request. If the refresh itself returns 401, the session is over and the store is cleared. Other refresh failures (offline, 429) don't log the user out.
- **One refresh at a time.** Refresh tokens rotate, so the same cookie must never be sent twice: concurrent 401s share one in-flight refresh, and across tabs `navigator.locks` makes tabs take turns.
- **Proactive refresh:** a timer refreshes the token a minute before it expires.
- **Other tabs:** a `BroadcastChannel` tells other tabs about logins and logouts.

## Layout

```
src/
├── components/        # AppLayout (shell, Page, PageHeader), AppSidebar, ArgusLogo, FormField, CopyInput, ui/ (shadcn)
├── features/
│   ├── apps/          # Home (welcome + apps panel), new app, app detail with Overview/Users/Sessions/Settings tabs
│   ├── auth/          # Login, change password, route guards, session actions, schemas
│   ├── profile/       # Profile page
│   └── sessions/      # The admin's own sessions
├── hooks/             # use-mobile (from shadcn's sidebar)
├── lib/               # Axios client, runtime config, errors, formatting, cross-tab channel, query client
├── stores/auth.ts     # Zustand auth store
├── router.tsx
└── main.tsx           # Providers, startup refresh, cross-tab sync
```

The logo source files are in `public/Logos`. The UI uses an SVG redrawn from them ([src/components/ArgusLogo.tsx](src/components/ArgusLogo.tsx), [public/favicon.svg](public/favicon.svg)).

## Docker

```bash
docker build -t dotnet-auth-frontend .
docker run -p 8081:8080 -e API_UPSTREAM=http://host.docker.internal:5183 dotnet-auth-frontend
```

The image is nginx serving `dist/` and proxying `/api` and `/.well-known` to `API_UPSTREAM` (default `http://dotnet-auth:8080`, the Kubernetes service). Add the URL users open to the API's `Argus:AllowedOrigins`.
