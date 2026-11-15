export interface User {
  id: string
  fullName: string
  userName: string
  email: string
  city: string
}

/** GET /api/auth/me */
export interface CurrentUser extends User {
  appId: string
  roles: string[]
  /** True for the seeded admin until they choose their own password */
  mustChangePassword: boolean
}

/** Body of the login response. The refresh token comes back only as an HttpOnly cookie. */
export interface AuthResponse extends User {
  accessToken: string
}

export interface Session {
  tokenId: number
  device: string | null
  ipAddress: string | null
  issuedAt: string
  expiresAt: string
  isCurrent: boolean
}

/** An application client, as GET /api/apps returns it */
export interface App {
  id: string
  name: string
  audience: string
  isPublic: boolean
  registrationEnabled: boolean
  allowedOrigins: string[]
  createdAt: string
  userCount: number
  activeSessionCount: number
}

/** POST /api/apps. The raw API key is returned only here, once. Null for public clients. */
export interface CreatedApp extends App {
  rawApiKey: string | null
}

export interface AppInput {
  name: string
  audience: string | null
  allowedOrigins: string[]
  registrationEnabled: boolean
}

export interface NewAppInput extends AppInput {
  isPublic: boolean
}

/** A user of one of the apps, as GET /api/apps/{id}/users returns them */
export interface AdminUser {
  id: string
  email: string
  fullName: string | null
  city: string | null
  isLockedOut: boolean
  lockoutEnd: string | null
  activeSessionCount: number
  lastSignInAt: string | null
}

export interface AdminSession {
  tokenId: number
  userId: string
  userEmail: string | null
  device: string | null
  ipAddress: string | null
  issuedAt: string
  expiresAt: string
}

export interface PagedResult<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}
