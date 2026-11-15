import { api } from '@/lib/api'
import type { AuthResponse, CurrentUser } from '@/lib/types'
import type { LoginInput, RegisterInput } from './schemas'

export async function login(input: LoginInput) {
  const { data } = await api.post<AuthResponse>('/api/auth/login', input, { skipAuthRefresh: true })
  return data
}

export async function register({ fullName, email, city, password }: RegisterInput) {
  const { data } = await api.post<AuthResponse>(
    '/api/auth/register',
    { fullName, email, city, password },
    { skipAuthRefresh: true },
  )
  return data
}

export async function logout() {
  await api.post('/api/auth/logout')
}

export async function fetchMe() {
  const { data } = await api.get<CurrentUser>('/api/auth/me')
  return data
}
