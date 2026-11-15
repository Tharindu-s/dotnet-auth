import { QueryClientProvider } from '@tanstack/react-query'
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { RouterProvider } from 'react-router'
import { Toaster } from '@/components/ui/sonner'
import { restoreSession, scheduleTokenRefresh, syncSessionAcrossTabs } from '@/features/auth/session'
import { createQueryClient } from '@/lib/queryClient'
import { router } from '@/router'
import './index.css'

const queryClient = createQueryClient()

// Started once here rather than in effects, so StrictMode's double render can't run them twice
scheduleTokenRefresh()
syncSessionAcrossTabs(queryClient)
void restoreSession()

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
      <Toaster richColors />
    </QueryClientProvider>
  </StrictMode>,
)
