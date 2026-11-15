import { Navigate, Outlet, useLocation } from 'react-router'
import { FullPageSpinner } from '@/components/FullPageSpinner'
import { useAuthStore } from '@/stores/auth'

/** Pages for signed-in users. Sends everyone else to /login and back here afterwards. */
export function ProtectedRoute() {
  const status = useAuthStore((s) => s.status)
  const location = useLocation()

  if (status === 'loading') return <FullPageSpinner />
  if (status === 'anonymous') return <Navigate to="/login" replace state={{ from: location }} />
  return <Outlet />
}

/** Login and register. Signed-in users go to the home page instead. */
export function PublicOnlyRoute() {
  const status = useAuthStore((s) => s.status)

  if (status === 'loading') return <FullPageSpinner />
  if (status === 'authenticated') return <Navigate to="/" replace />
  return <Outlet />
}
