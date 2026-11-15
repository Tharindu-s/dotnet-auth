import { api } from '@/lib/api'
import type { AuthResponse, CurrentUser } from '@/lib/types'
import type { LoginInput } from './schemas'

export async function login(input: LoginInput) {
  const { data } = await api.post<AuthResponse>('/api/auth/login', input, { skipAuthRefresh: true, sendAppId: true })
  return data
}

export async function logout() {
  await api.post('/api/auth/logout', null, { sendAppId: true })
}

/** Pass accessToken to call it before the token is in the store (right after login) */
export async function fetchMe(accessToken?: string) {
  const headers = accessToken ? { Authorization: `Bearer ${accessToken}` } : undefined
  const { data } = await api.get<CurrentUser>('/api/auth/me', { headers })
  return data
}

export async function changePassword(currentPassword: string, newPassword: string) {
  await api.post('/api/auth/change-password', { currentPassword, newPassword })
}
