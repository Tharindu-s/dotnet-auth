import { create } from 'zustand'
import type { CurrentUser } from '@/lib/types'

export type AuthStatus = 'loading' | 'authenticated' | 'anonymous'

interface AuthState {
  /** 'loading' until the startup refresh has finished */
  status: AuthStatus
  user: CurrentUser | null
  /** Kept in memory only. A reload gets a new one from the refresh cookie. */
  accessToken: string | null
  setSession: (user: CurrentUser, accessToken: string) => void
  setUser: (user: CurrentUser) => void
  setAccessToken: (accessToken: string) => void
  clear: () => void
}

export const useAuthStore = create<AuthState>()((set) => ({
  status: 'loading',
  user: null,
  accessToken: null,
  setSession: (user, accessToken) => set({ status: 'authenticated', user, accessToken }),
  setUser: (user) => set({ user }),
  setAccessToken: (accessToken) => set({ accessToken }),
  clear: () => set({ status: 'anonymous', user: null, accessToken: null }),
}))
