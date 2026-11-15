import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/lib/api'
import type { Session } from '@/lib/types'

const sessionsKey = ['sessions'] as const

export function useSessions() {
  return useQuery({
    queryKey: sessionsKey,
    queryFn: async () => (await api.get<Session[]>('/api/auth/sessions')).data,
  })
}

export function useRevokeSession() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (sessionId: number) => api.delete(`/api/auth/sessions/${sessionId}`),
    onSettled: () => queryClient.invalidateQueries({ queryKey: sessionsKey }),
  })
}
