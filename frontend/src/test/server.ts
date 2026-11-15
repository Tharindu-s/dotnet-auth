import { http, HttpResponse } from 'msw'
import { setupServer } from 'msw/node'
import type { App, CurrentUser } from '@/lib/types'

export const argusConfig = { appId: 'argus-app', issuer: 'http://localhost:5183' }

// Handlers passed here survive server.resetHandlers(); tests add or override the rest with server.use(...)
export const server = setupServer(
  http.get('/api/argus/config', () => HttpResponse.json(argusConfig)),
  // The sidebar lists the apps on every signed-in page
  http.get('/api/apps', () => HttpResponse.json([])),
)

/** A signed-in Argus admin who has already changed the initial password */
export const user: CurrentUser = {
  id: 'user-1',
  fullName: 'Argus admin',
  userName: 'admin@argus.local',
  email: 'admin@argus.local',
  city: '',
  appId: argusConfig.appId,
  roles: ['Admin'],
  mustChangePassword: false,
}

export function app(overrides: Partial<App> = {}): App {
  return {
    id: 'app-1',
    name: 'Notes',
    audience: 'notes-api',
    isPublic: false,
    registrationEnabled: true,
    allowedOrigins: [],
    createdAt: '2026-11-01T08:00:00Z',
    userCount: 2,
    activeSessionCount: 3,
    ...overrides,
  }
}
