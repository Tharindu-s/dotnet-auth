import { createBrowserRouter, type RouteObject } from 'react-router'
import { AppLayout } from '@/components/AppLayout'
import { NotFoundPage } from '@/components/NotFoundPage'
import { LoginPage } from '@/features/auth/LoginPage'
import { RegisterPage } from '@/features/auth/RegisterPage'
import { ProtectedRoute, PublicOnlyRoute } from '@/features/auth/RouteGuards'
import { ProfilePage } from '@/features/profile/ProfilePage'
import { SessionsPage } from '@/features/sessions/SessionsPage'

// In production builds import.meta.env.DEV is false, so Vite drops this route and the page's code
const devRoutes: RouteObject[] = import.meta.env.DEV
  ? [{ path: '/dev/apps', lazy: async () => ({ Component: (await import('@/features/dev/DevCreateAppPage')).default }) }]
  : []

export const routes: RouteObject[] = [
  {
    element: <PublicOnlyRoute />,
    children: [
      { path: '/login', element: <LoginPage /> },
      { path: '/register', element: <RegisterPage /> },
    ],
  },
  {
    element: <ProtectedRoute />,
    children: [
      {
        element: <AppLayout />,
        children: [
          { index: true, element: <ProfilePage /> },
          { path: '/sessions', element: <SessionsPage /> },
        ],
      },
    ],
  },
  ...devRoutes,
  { path: '*', element: <NotFoundPage /> },
]

export const router = createBrowserRouter(routes)
