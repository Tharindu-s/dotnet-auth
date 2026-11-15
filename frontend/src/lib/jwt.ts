/** Reads `exp` from a JWT without verifying it. Only used to schedule refreshes. */
export function getTokenExpiry(token: string): Date | null {
  try {
    const payload = token.split('.')[1]
    const json = atob(payload.replace(/-/g, '+').replace(/_/g, '/'))
    const { exp } = JSON.parse(json) as { exp?: number }
    return typeof exp === 'number' ? new Date(exp * 1000) : null
  } catch {
    return null
  }
}
