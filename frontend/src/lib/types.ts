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
}

/** Body of login and register responses. The refresh token comes back only as an HttpOnly cookie. */
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

export interface CreatedApp {
  id: string
  audience: string
  isPublic: boolean
  allowedOrigins: string[]
  rawApiKey: string | null
}
