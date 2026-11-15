import type { ReactNode } from 'react'
import { Link } from 'react-router'
import { Eyebrow, GradientBlocks, SiteHeader } from '@/components/brand'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { config } from '@/lib/config'

interface AuthCardProps {
  /** Browser tab title, such as "Sign in" (shown as "Sign in · Argus") */
  tabTitle: string
  eyebrow: string
  title: string
  description: string
  /** Link to the other auth page, shown as a pill in the header */
  headerLink: { to: string; label: string }
  footer: ReactNode
  children: ReactNode
}

/** Hero-style layout shared by the login and register pages: centered column framed by color blocks */
export function AuthCard({ tabTitle, eyebrow, title, description, headerLink, footer, children }: AuthCardProps) {
  return (
    <div className="flex min-h-svh flex-col bg-background">
      <title>{`${tabTitle} · Argus`}</title>
      <SiteHeader
        actions={
          <Button asChild>
            <Link to={headerLink.to}>{headerLink.label}</Link>
          </Button>
        }
      />
      <main className="relative flex flex-1 justify-center px-4 py-12 sm:py-16 lg:mx-10 lg:mb-10">
        <GradientBlocks />
        <div className="relative z-10 grid w-full max-w-xl content-start justify-items-center gap-8">
          <div className="grid justify-items-center gap-5 text-center">
            <Eyebrow>{eyebrow}</Eyebrow>
            <h1 className="display-title text-5xl sm:text-[4rem]">{title}</h1>
            <p className="max-w-sm text-[0.9375rem] text-muted-foreground">{description}</p>
          </div>
          <div className="grid w-full max-w-sm gap-8">
            {!config.appId && <MissingAppIdNotice />}
            {children}
            <div className="text-center text-sm text-muted-foreground">{footer}</div>
          </div>
        </div>
      </main>
    </div>
  )
}

function MissingAppIdNotice() {
  return (
    <Alert variant="destructive">
      <AlertTitle>VITE_APP_ID is not set</AlertTitle>
      <AlertDescription>
        <p>
          Create a public application client
          {import.meta.env.DEV && (
            <>
              {' '}
              on the{' '}
              <Link to="/dev/apps" className="underline">
                dev page
              </Link>
            </>
          )}
          , put its ID in <code>frontend/.env.local</code>, and restart <code>npm run dev</code>.
        </p>
      </AlertDescription>
    </Alert>
  )
}
