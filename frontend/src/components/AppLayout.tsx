import type { ReactNode } from 'react'
import { Outlet } from 'react-router'
import { AppSidebar } from '@/components/AppSidebar'
import { ArgusLogo } from '@/components/ArgusLogo'
import { SidebarInset, SidebarProvider, SidebarTrigger } from '@/components/ui/sidebar'
import { cn } from '@/lib/utils'

/** Signed-in shell: the app sits in a rounded frame over the backdrop, with the sidebar on the left */
export function AppLayout() {
  return (
    <div className="app-backdrop h-svh md:p-4 lg:p-6">
      <SidebarProvider className="h-full min-h-0 overflow-hidden bg-frame md:rounded-[2rem] md:shadow-[0_40px_90px_-40px_rgb(16_18_24/0.45)] md:ring-1 md:ring-white/70">
        <AppSidebar />
        <SidebarInset className="min-h-0 bg-transparent">
          <header className="flex h-14 shrink-0 items-center gap-2 border-b px-3 md:hidden">
            <SidebarTrigger />
            <ArgusLogo />
          </header>
          <div className="min-h-0 flex-1">
            <Outlet />
          </div>
        </SidebarInset>
      </SidebarProvider>
    </div>
  )
}

/** Scrolling content area for a page inside the shell */
export function Page({ children, className }: { children: ReactNode; className?: string }) {
  return (
    <div className="h-full overflow-y-auto">
      <div className={cn('mx-auto w-full max-w-5xl px-4 py-8 sm:px-8 sm:py-10', className)}>{children}</div>
    </div>
  )
}

/** Title, description and actions at the top of a page. `above` holds breadcrumbs. */
export function PageHeader({
  tabTitle,
  title,
  description,
  actions,
  above,
}: {
  tabTitle: string
  title: ReactNode
  description?: ReactNode
  actions?: ReactNode
  above?: ReactNode
}) {
  return (
    <div className="mb-8 grid gap-3">
      <title>{`${tabTitle} · Argus`}</title>
      {above}
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div className="grid min-w-0 gap-1.5">
          <h1 className="text-3xl font-semibold tracking-tight wrap-break-word text-ink sm:text-4xl">{title}</h1>
          {description && <div className="text-[0.9375rem] text-muted-foreground">{description}</div>}
        </div>
        {actions && <div className="flex shrink-0 gap-2">{actions}</div>}
      </div>
    </div>
  )
}
