import type { QueryClient } from '@tanstack/react-query'
import { refreshAccessToken } from '@/lib/api'
import { broadcastAuth, onAuthMessage } from '@/lib/authChannel'
import { getTokenExpiry } from '@/lib/jwt'
import type { CurrentUser } from '@/lib/types'
import { useAuthStore } from '@/stores/auth'
import * as authApi from './api'
import type { LoginInput } from './schemas'

/** On page load: trade the refresh cookie (if any) for an access token, then load the profile. */
export async function restoreSession() {
  const { clear, setSession } = useAuthStore.getState()
  try {
    const token = await refreshAccessToken()
    if (!token) return clear()
    setSession(await authApi.fetchMe(), token)
  } catch {
    clear()
  }
}

/** Logs in, then loads the full profile (roles, mustChangePassword) before the session starts */
export async function signIn(input: LoginInput) {
  const { accessToken } = await authApi.login(input)
  startSession(await authApi.fetchMe(accessToken), accessToken)
}

/** Reloads the profile, for example after the password change clears mustChangePassword */
export async function reloadUser() {
  useAuthStore.getState().setUser(await authApi.fetchMe())
}

export async function signOut(queryClient: QueryClient) {
  try {
    await authApi.logout()
  } finally {
    // Clear locally even if the request failed; the server-side session will expire on its own
    endSession(queryClient)
    broadcastAuth({ type: 'logout' })
  }
}

function startSession(user: CurrentUser, accessToken: string) {
  useAuthStore.getState().setSession(user, accessToken)
  broadcastAuth({ type: 'login' })
}

function endSession(queryClient: QueryClient) {
  useAuthStore.getState().clear()
  queryClient.clear()
}

/** Follows logins and logouts made in other tabs. Returns an unsubscribe function. */
export function syncSessionAcrossTabs(queryClient: QueryClient) {
  return onAuthMessage((message) => {
    if (message.type === 'logout') endSession(queryClient)
    if (message.type === 'login' && useAuthStore.getState().status !== 'authenticated') void restoreSession()
  })
}

const REFRESH_BEFORE_EXPIRY_MS = 60_000

/** Refreshes the access token a minute before it expires, so requests rarely hit a 401. */
export function scheduleTokenRefresh() {
  let timer: ReturnType<typeof setTimeout> | undefined

  const unsubscribe = useAuthStore.subscribe((state, previous) => {
    if (state.accessToken === previous.accessToken) return
    clearTimeout(timer)
    const expiry = state.accessToken ? getTokenExpiry(state.accessToken) : null
    if (!expiry) return

    const delay = Math.max(expiry.getTime() - Date.now() - REFRESH_BEFORE_EXPIRY_MS, 5_000)
    timer = setTimeout(async () => {
      try {
        if (!(await refreshAccessToken())) useAuthStore.getState().clear()
      } catch {
        // Offline or server error: the 401 interceptor will retry on the next request
      }
    }, delay)
  })

  return () => {
    clearTimeout(timer)
    unsubscribe()
  }
}
