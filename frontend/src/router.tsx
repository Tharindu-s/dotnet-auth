import { createBrowserRouter, type RouteObject } from 'react-router'
import { AppLayout } from '@/components/AppLayout'
import { NotFoundPage } from '@/components/NotFoundPage'
import { AppDetailPage } from '@/features/apps/AppDetailPage'
import { AppsPage } from '@/features/apps/AppsPage'
import { NewAppPage } from '@/features/apps/NewAppPage'
import { ChangePasswordPage } from '@/features/auth/ChangePasswordPage'
import { LoginPage } from '@/features/auth/LoginPage'
import { ProtectedRoute, PublicOnlyRoute } from '@/features/auth/RouteGuards'
import { ProfilePage } from '@/features/profile/ProfilePage'
import { SessionsPage } from '@/features/sessions/SessionsPage'

export const routes: RouteObject[] = [
  {
    element: <PublicOnlyRoute />,
    children: [{ path: '/login', element: <LoginPage /> }],
  },
  {
    element: <ProtectedRoute />,
    children: [
      { path: '/change-password', element: <ChangePasswordPage /> },
      {
        element: <AppLayout />,
        children: [
          { index: true, element: <AppsPage /> },
          { path: '/apps/new', element: <NewAppPage /> },
          { path: '/apps/:appId', element: <AppDetailPage /> },
          { path: '/profile', element: <ProfilePage /> },
          { path: '/sessions', element: <SessionsPage /> },
        ],
      },
    ],
  },
  { path: '*', element: <NotFoundPage /> },
]

export const router = createBrowserRouter(routes)
