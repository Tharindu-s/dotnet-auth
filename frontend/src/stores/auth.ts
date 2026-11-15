import { create } from 'zustand'
import type { User } from '@/lib/types'

export type AuthStatus = 'loading' | 'authenticated' | 'anonymous'

interface AuthState {
  /** 'loading' until the startup refresh has finished */
  status: AuthStatus
  user: User | null
  /** Kept in memory only. A reload gets a new one from the refresh cookie. */
  accessToken: string | null
  setSession: (user: User, accessToken: string) => void
  setAccessToken: (accessToken: string) => void
  clear: () => void
}

export const useAuthStore = create<AuthState>()((set) => ({
  status: 'loading',
  user: null,
  accessToken: null,
  setSession: (user, accessToken) => set({ status: 'authenticated', user, accessToken }),
  setAccessToken: (accessToken) => set({ accessToken }),
  clear: () => set({ status: 'anonymous', user: null, accessToken: null }),
}))
