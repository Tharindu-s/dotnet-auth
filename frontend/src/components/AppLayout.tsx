import { useQueryClient } from '@tanstack/react-query'
import { useState, type ReactNode } from 'react'
import { NavLink, Outlet } from 'react-router'
import { toast } from 'sonner'
import { BlockAccent, Eyebrow, SiteHeader } from '@/components/brand'
import { Button } from '@/components/ui/button'
import { signOut } from '@/features/auth/session'
import { cn } from '@/lib/utils'
import { useAuthStore } from '@/stores/auth'

const links = [
  { to: '/', label: 'Profile', end: true },
  { to: '/sessions', label: 'Sessions', end: false },
  ...(import.meta.env.DEV ? [{ to: '/dev/apps', label: 'Dev tools', end: false }] : []),
]

export function AppLayout() {
  const queryClient = useQueryClient()
  const email = useAuthStore((s) => s.user?.email)
  const [signingOut, setSigningOut] = useState(false)

  const onSignOut = async () => {
    setSigningOut(true)
    try {
      await signOut(queryClient)
    } catch {
      toast.error('Could not reach the server, but you are signed out on this device.')
    }
  }

  return (
    <div className="min-h-svh bg-background">
      <SiteHeader
        nav={
          <nav className="flex gap-6 whitespace-nowrap" aria-label="Main">
            {links.map((link) => (
              <NavLink
                key={link.to}
                to={link.to}
                end={link.end}
                className={({ isActive }) =>
                  cn(
                    'text-sm text-muted-foreground transition-colors hover:text-ink',
                    isActive && 'font-medium text-ink',
                  )
                }
              >
                {link.label}
              </NavLink>
            ))}
          </nav>
        }
        actions={
          <>
            <span className="hidden text-sm text-muted-foreground md:inline">{email}</span>
            <Button onClick={onSignOut} disabled={signingOut}>
              Log out
            </Button>
          </>
        }
      />
      <main className="mx-auto max-w-5xl px-4 py-10 sm:px-6 sm:py-14">
        <Outlet />
      </main>
    </div>
  )
}

/** Eyebrow, big title and lead text at the top of a signed-in page */
export function PageHeader({
  tabTitle,
  eyebrow,
  title,
  description,
}: {
  tabTitle: string
  eyebrow: string
  title: string
  description: ReactNode
}) {
  return (
    <div className="mb-8 flex items-end justify-between gap-6">
      <title>{`${tabTitle} · Argus`}</title>
      <div className="grid gap-4">
        <Eyebrow className="justify-self-start">{eyebrow}</Eyebrow>
        <h1 className="display-title text-4xl sm:text-5xl">{title}</h1>
        <p className="max-w-lg text-[0.9375rem] text-muted-foreground">{description}</p>
      </div>
      <BlockAccent className="hidden shrink-0 md:flex" />
    </div>
  )
}
