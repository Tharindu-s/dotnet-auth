import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/lib/api'
import type { AdminSession, AdminUser, App, AppInput, CreatedApp, NewAppInput, PagedResult } from '@/lib/types'

// Query keys: ['apps'] for the list, ['apps', id] for one app, ['apps', id, 'users' | 'sessions', ...] below it,
// so invalidating ['apps', id] refreshes everything about that app.
const appsKey = ['apps'] as const
const appKey = (id: string) => ['apps', id] as const

export function useApps() {
  return useQuery({
    queryKey: appsKey,
    queryFn: async () => (await api.get<App[]>('/api/apps')).data,
  })
}

export function useApp(id: string) {
  return useQuery({
    queryKey: appKey(id),
    queryFn: async () => (await api.get<App>(`/api/apps/${id}`)).data,
  })
}

export function useCreateApp() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (input: NewAppInput) => (await api.post<CreatedApp>('/api/apps', input)).data,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: appsKey }),
  })
}

export function useUpdateApp(id: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (input: AppInput) => (await api.put<App>(`/api/apps/${id}`, input)).data,
    onSuccess: (app) => {
      queryClient.setQueryData(appKey(id), app)
      void queryClient.invalidateQueries({ queryKey: appsKey, exact: true })
    },
  })
}

export function useRotateApiKey(id: string) {
  return useMutation({
    mutationFn: async () => (await api.post<{ rawApiKey: string }>(`/api/apps/${id}/rotate-key`)).data.rawApiKey,
  })
}

export function useDeleteApp(id: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: () => api.delete(`/api/apps/${id}`),
    onSuccess: () => {
      queryClient.removeQueries({ queryKey: appKey(id) })
      void queryClient.invalidateQueries({ queryKey: appsKey, exact: true })
    },
  })
}

// Users

export function useAppUsers(appId: string, search: string, page: number, pageSize = 25) {
  return useQuery({
    queryKey: [...appKey(appId), 'users', { search, page, pageSize }],
    queryFn: async () =>
      (await api.get<PagedResult<AdminUser>>(`/api/apps/${appId}/users`, { params: { search: search || undefined, page, pageSize } }))
        .data,
    // Keeps the current page on screen while the next one loads
    placeholderData: keepPreviousData,
  })
}

type UserAction = 'lock' | 'unlock' | 'delete'

export function useUserAction(appId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ userId, action }: { userId: string; action: UserAction }) =>
      action === 'delete'
        ? api.delete(`/api/apps/${appId}/users/${userId}`)
        : api.post(`/api/apps/${appId}/users/${userId}/${action}`),
    // Counts on the app and the sessions list change too
    onSettled: () => queryClient.invalidateQueries({ queryKey: appKey(appId) }),
  })
}

// Sessions

export function useAppSessions(appId: string) {
  return useQuery({
    queryKey: [...appKey(appId), 'sessions'],
    queryFn: async () => (await api.get<AdminSession[]>(`/api/apps/${appId}/sessions`)).data,
  })
}

export function useRevokeAppSession(appId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (sessionId: number) => api.delete(`/api/apps/${appId}/sessions/${sessionId}`),
    onSettled: () => queryClient.invalidateQueries({ queryKey: appKey(appId) }),
  })
}
