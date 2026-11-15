import { Globe, KeyRound, Plus, Search, Server, type LucideIcon } from 'lucide-react'
import { useState } from 'react'
import { Link } from 'react-router'
import { ArgusGlowMark } from '@/components/ArgusLogo'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Empty, EmptyDescription, EmptyHeader, EmptyMedia, EmptyTitle } from '@/components/ui/empty'
import { InputGroup, InputGroupAddon, InputGroupInput } from '@/components/ui/input-group'
import { Item, ItemContent, ItemDescription, ItemFooter, ItemGroup, ItemMedia, ItemSeparator, ItemTitle } from '@/components/ui/item'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Skeleton } from '@/components/ui/skeleton'
import { getErrorMessage } from '@/lib/errors'
import { formatRelative, plural } from '@/lib/format'
import type { App } from '@/lib/types'
import { useApps } from './queries'

const quickActions: { to: string; icon: LucideIcon; title: string; description: string; tint: string }[] = [
  {
    to: '/apps/new?type=server',
    icon: Server,
    title: 'New server app',
    description: 'A backend that signs users in with an API key',
    tint: 'from-[#ffd9c7] to-[#f6926f]',
  },
  {
    to: '/apps/new?type=browser',
    icon: Globe,
    title: 'New browser app',
    description: 'A web app that calls this server from the browser',
    tint: 'from-[#e3d7fb] to-[#a98be8]',
  },
  {
    to: '/change-password',
    icon: KeyRound,
    title: 'Secure your account',
    description: 'Change the password you sign in to Argus with',
    tint: 'from-[#d6e4ff] to-[#6f8ff6]',
  },
]

/** Home: a welcome with quick actions in the middle, and the apps list in a panel on the right */
export function AppsPage() {
  const apps = useApps()

  return (
    <div className="h-full overflow-y-auto xl:grid xl:grid-cols-[minmax(0,1fr)_23rem] xl:overflow-hidden">
      <title>Home · Argus</title>
      <div className="xl:overflow-y-auto">
        <div className="mx-auto flex max-w-3xl flex-col items-center px-4 pt-10 pb-6 text-center sm:px-8 sm:pt-16 xl:pb-16">
          <ArgusGlowMark size={52} />
          <h1 className="text-4xl font-semibold tracking-tight text-ink sm:text-5xl">Welcome back!</h1>
          <p className="mt-3 text-[0.9375rem] text-muted-foreground">What would you like to set up today?</p>

          <div className="mt-10 grid w-full gap-4 text-left sm:grid-cols-3">
            {quickActions.map((action) => (
              <Link
                key={action.title}
                to={action.to}
                className="group rounded-3xl outline-none focus-visible:ring-3 focus-visible:ring-ring/50"
              >
                <Card className="h-full transition-transform group-hover:-translate-y-0.5">
                  <CardContent className="grid gap-6">
                    <span className={`grid size-11 place-items-center rounded-2xl bg-linear-to-br text-white shadow-soft ${action.tint}`}>
                      <action.icon className="size-5" />
                    </span>
                    <span className="grid gap-1">
                      <span className="font-semibold text-ink">{action.title}</span>
                      <span className="text-sm text-muted-foreground">{action.description}</span>
                    </span>
                  </CardContent>
                </Card>
              </Link>
            ))}
          </div>

          {apps.data && apps.data.length > 0 && <Totals apps={apps.data} />}
        </div>
      </div>

      <aside className="px-4 pb-8 sm:px-8 xl:h-full xl:min-h-0 xl:p-3">
        <AppsPanel apps={apps} />
      </aside>
    </div>
  )
}

function Totals({ apps }: { apps: App[] }) {
  const stats = [
    { label: 'Apps', value: apps.length },
    { label: 'Users', value: apps.reduce((sum, app) => sum + app.userCount, 0) },
    { label: 'Active sessions', value: apps.reduce((sum, app) => sum + app.activeSessionCount, 0) },
  ]
  return (
    <Card className="mt-4 w-full">
      <CardContent className="grid grid-cols-3 divide-x">
        {stats.map((stat) => (
          <div key={stat.label} className="grid gap-0.5 px-2 text-center">
            <span className="text-2xl font-semibold tracking-tight text-ink">{stat.value}</span>
            <span className="text-xs text-muted-foreground">{stat.label}</span>
          </div>
        ))}
      </CardContent>
    </Card>
  )
}

/** "Your apps": search, the list, and a create button pinned to the bottom */
function AppsPanel({ apps }: { apps: ReturnType<typeof useApps> }) {
  const [search, setSearch] = useState('')
  const term = search.trim().toLowerCase()
  const visible = apps.data?.filter((app) => !term || app.name.toLowerCase().includes(term) || app.audience.toLowerCase().includes(term))

  return (
    <Card className="gap-4 xl:h-full">
      <div className="flex items-center justify-between px-6">
        <h2 className="text-xl font-semibold tracking-tight text-ink">Your apps</h2>
        <Button asChild variant="secondary" size="icon" className="rounded-full" aria-label="New app">
          <Link to="/apps/new">
            <Plus />
          </Link>
        </Button>
      </div>
      <div className="px-6">
        <InputGroup className="h-10 rounded-full bg-white">
          <InputGroupAddon>
            <Search />
          </InputGroupAddon>
          <InputGroupInput placeholder="Search…" aria-label="Search apps" value={search} onChange={(e) => setSearch(e.target.value)} />
        </InputGroup>
      </div>

      <ScrollArea className="min-h-0 xl:flex-1">
        <div className="px-3">
          {apps.isPending ? (
            <div className="grid gap-3 px-3" aria-label="Loading apps">
              <Skeleton className="h-16" />
              <Skeleton className="h-16" />
            </div>
          ) : apps.isError ? (
            <Alert variant="destructive" className="mx-3 w-auto">
              <AlertDescription>{getErrorMessage(apps.error)}</AlertDescription>
            </Alert>
          ) : apps.data.length === 0 ? (
            <Empty className="py-6">
              <EmptyHeader>
                <EmptyMedia variant="icon">
                  <Server />
                </EmptyMedia>
                <EmptyTitle>No apps yet.</EmptyTitle>
                <EmptyDescription>
                  Create one for each project that needs sign-in. You get an App ID, and an API key for server apps.
                </EmptyDescription>
              </EmptyHeader>
            </Empty>
          ) : visible?.length === 0 ? (
            <p className="px-3 py-6 text-center text-sm text-muted-foreground">No apps match “{search.trim()}”.</p>
          ) : (
            <ItemGroup>
              {visible?.map((app, index) => (
                <div key={app.id}>
                  {index > 0 && <ItemSeparator className="my-1" />}
                  <AppItem app={app} />
                </div>
              ))}
            </ItemGroup>
          )}
        </div>
      </ScrollArea>

      <div className="px-6">
        <Button asChild size="lg" className="h-11 w-full rounded-full shadow-soft">
          <Link to="/apps/new">Create new app</Link>
        </Button>
      </div>
    </Card>
  )
}

function AppItem({ app }: { app: App }) {
  const Icon = app.isPublic ? Globe : Server
  return (
    <Item asChild className="rounded-2xl">
      <Link to={`/apps/${app.id}`}>
        <ItemMedia variant="icon" className="size-9 rounded-xl bg-surface">
          <Icon />
        </ItemMedia>
        <ItemContent className="min-w-0">
          <ItemTitle className="w-full truncate">{app.name}</ItemTitle>
          <ItemDescription className="truncate">
            {app.isPublic ? 'Browser app' : 'Server app'} · {app.audience}
          </ItemDescription>
        </ItemContent>
        <ItemFooter className="text-xs text-muted-foreground">
          <span className="flex items-center gap-1.5">
            {plural(app.userCount, 'user')} · {plural(app.activeSessionCount, 'session')}
            {!app.registrationEnabled && <Badge variant="outline">Sign-ups off</Badge>}
          </span>
          <span>{formatRelative(app.createdAt)}</span>
        </ItemFooter>
      </Link>
    </Item>
  )
}
