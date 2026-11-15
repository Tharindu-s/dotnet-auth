import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios'
import { config } from '@/lib/config'
import { useAuthStore } from '@/stores/auth'

declare module 'axios' {
  interface AxiosRequestConfig {
    /** Don't try to refresh on a 401 (login, register, refresh itself) */
    skipAuthRefresh?: boolean
  }
  interface InternalAxiosRequestConfig {
    _retried?: boolean
  }
}

export const api = axios.create({
  baseURL: config.apiBaseUrl,
  // Sends the refresh cookie when the API is on another origin. A no-op for same origin.
  withCredentials: true,
})

api.interceptors.request.use((request) => {
  const token = useAuthStore.getState().accessToken
  if (token && !request.headers.Authorization) request.headers.Authorization = `Bearer ${token}`
  // The app is a public client: it identifies itself with its ID and the browser's Origin header
  if (config.appId) request.headers['X-App-Id'] = config.appId
  return request
})

api.interceptors.response.use(undefined, async (error: AxiosError) => {
  const original = error.config as InternalAxiosRequestConfig | undefined
  if (error.response?.status !== 401 || !original || original._retried || original.skipAuthRefresh) {
    throw error
  }

  original._retried = true
  const token = await refreshAccessToken()
  if (!token) {
    useAuthStore.getState().clear()
    throw error
  }

  original.headers.Authorization = `Bearer ${token}`
  return api(original)
})

let inFlightRefresh: Promise<string | null> | null = null

/**
 * Exchanges the refresh cookie for a new access token. Returns null if the session is over.
 *
 * Refresh tokens rotate, so the same cookie must never be sent twice:
 * - within a tab, concurrent callers share one in-flight request;
 * - across tabs, a Web Lock makes tabs take turns, and each tab's request carries the
 *   cookie the previous tab just received.
 */
export function refreshAccessToken(): Promise<string | null> {
  inFlightRefresh ??= withRefreshLock(requestNewAccessToken).finally(() => {
    inFlightRefresh = null
  })
  return inFlightRefresh
}

async function requestNewAccessToken(): Promise<string | null> {
  try {
    const { data } = await api.post<{ accessToken: string }>('/api/auth/refresh', null, { skipAuthRefresh: true })
    useAuthStore.getState().setAccessToken(data.accessToken)
    return data.accessToken
  } catch (error) {
    // 401 means the session ended. Anything else (offline, 429, 500) isn't a reason to log out.
    if (axios.isAxiosError(error) && error.response?.status === 401) return null
    throw error
  }
}

function withRefreshLock<T>(callback: () => Promise<T>): Promise<T> {
  if (typeof navigator !== 'undefined' && navigator.locks) {
    return navigator.locks.request('auth-refresh', callback)
  }
  return callback()
}
