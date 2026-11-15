import type { ReactNode } from 'react'
import { ArgusGlowMark } from '@/components/ArgusLogo'
import { Card, CardContent } from '@/components/ui/card'

interface AuthLayoutProps {
  /** Browser tab title, such as "Sign in" (shown as "Sign in · Argus") */
  tabTitle: string
  title: string
  description: ReactNode
  /** Top-right corner, such as a log out button */
  actions?: ReactNode
  footer?: ReactNode
  children: ReactNode
}

/** Sign-in and password screens: the glowing mark, a welcome heading and a card on the backdrop */
export function AuthLayout({ tabTitle, title, description, actions, footer, children }: AuthLayoutProps) {
  return (
    <div className="app-backdrop relative flex min-h-svh flex-col items-center justify-center px-4 py-12">
      <title>{`${tabTitle} · Argus`}</title>
      {actions && <div className="absolute top-4 right-4">{actions}</div>}
      <main className="w-full max-w-md">
        <div className="mb-6 flex flex-col items-center text-center">
          <ArgusGlowMark size={44} />
          <h1 className="text-4xl font-semibold tracking-tight text-ink">{title}</h1>
          <p className="mt-2 max-w-sm text-[0.9375rem] text-muted-foreground">{description}</p>
        </div>
        <Card>
          <CardContent>{children}</CardContent>
        </Card>
        {footer && <div className="mt-6 text-center text-sm text-muted-foreground">{footer}</div>}
      </main>
    </div>
  )
}
