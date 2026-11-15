import { QueryClient } from '@tanstack/react-query'
import axios from 'axios'

export function createQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 30_000,
        // Retrying won't fix an auth or not-found error
        retry: (failureCount, error) => {
          const status = axios.isAxiosError(error) ? error.response?.status : undefined
          if (status && status >= 400 && status < 500) return false
          return failureCount < 2
        },
      },
    },
  })
}
