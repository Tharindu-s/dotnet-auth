import { useQueryClient } from '@tanstack/react-query'
import { ShieldX } from 'lucide-react'
import { Navigate, Outlet, useLocation } from 'react-router'
import { FullPageSpinner } from '@/components/FullPageSpinner'
import { MessagePage } from '@/components/NotFoundPage'
import { Button } from '@/components/ui/button'
import { useAuthStore } from '@/stores/auth'
import { signOut } from './session'

export const ADMIN_ROLE = 'Admin'

/**
 * Pages for signed-in admins. Sends everyone else to /login and back here afterwards,
 * and holds an admin on /change-password until they've replaced the initial password.
 */
export function ProtectedRoute() {
  const status = useAuthStore((s) => s.status)
  const user = useAuthStore((s) => s.user)
  const location = useLocation()

  if (status === 'loading') return <FullPageSpinner />
  if (status === 'anonymous' || !user) return <Navigate to="/login" replace state={{ from: location }} />
  if (!user.roles.includes(ADMIN_ROLE)) return <NoAccess />
  if (user.mustChangePassword && location.pathname !== '/change-password') return <Navigate to="/change-password" replace />
  return <Outlet />
}

/** The login page. Signed-in users go to the home page instead. */
export function PublicOnlyRoute() {
  const status = useAuthStore((s) => s.status)

  if (status === 'loading') return <FullPageSpinner />
  if (status === 'authenticated') return <Navigate to="/" replace />
  return <Outlet />
}

// Only reachable by an Argus user without the Admin role, which the service never creates itself
function NoAccess() {
  const queryClient = useQueryClient()
  return (
    <MessagePage
      tabTitle="No access"
      icon={<ShieldX />}
      title="No access."
      description="This account isn't an Argus admin."
      action={
        <Button className="rounded-full" onClick={() => void signOut(queryClient)}>
          Log out
        </Button>
      }
    />
  )
}
