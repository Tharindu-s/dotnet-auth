import axios from 'axios'
import { FileQuestion, Globe, Server } from 'lucide-react'
import { Link, useParams, useSearchParams } from 'react-router'
import { Page, PageHeader } from '@/components/AppLayout'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Badge } from '@/components/ui/badge'
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from '@/components/ui/breadcrumb'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { Empty, EmptyContent, EmptyDescription, EmptyHeader, EmptyMedia, EmptyTitle } from '@/components/ui/empty'
import { Skeleton } from '@/components/ui/skeleton'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { getErrorMessage } from '@/lib/errors'
import { plural } from '@/lib/format'
import { AppOverviewTab } from './AppOverviewTab'
import { AppSessionsTab } from './AppSessionsTab'
import { AppSettingsTab } from './AppSettingsTab'
import { AppUsersTab } from './AppUsersTab'
import { useApp } from './queries'

const tabs = ['overview', 'users', 'sessions', 'settings'] as const
type Tab = (typeof tabs)[number]

const tabLabels: Record<Tab, string> = { overview: 'Overview', users: 'Users', sessions: 'Sessions', settings: 'Settings' }

/** /apps/:appId. The open tab lives in ?tab=, so it survives reloads and can be linked to. */
export function AppDetailPage() {
  const { appId = '' } = useParams()
  const [params, setParams] = useSearchParams()
  const app = useApp(appId)
  const tab: Tab = tabs.find((t) => t === params.get('tab')) ?? 'overview'

  if (app.isPending) {
    return (
      <Page>
        <div className="grid gap-4" aria-label="Loading app">
          <Skeleton className="h-24 rounded-3xl" />
          <Skeleton className="h-72 rounded-3xl" />
        </div>
      </Page>
    )
  }

  if (app.isError) {
    if (axios.isAxiosError(app.error) && app.error.response?.status === 404) {
      return (
        <Page>
          <Card>
            <Empty>
              <EmptyHeader>
                <EmptyMedia variant="icon">
                  <FileQuestion />
                </EmptyMedia>
                <EmptyTitle>App not found</EmptyTitle>
                <EmptyDescription>It doesn&apos;t exist, or it was deleted.</EmptyDescription>
              </EmptyHeader>
              <EmptyContent>
                <Button asChild className="rounded-full">
                  <Link to="/">All apps</Link>
                </Button>
              </EmptyContent>
            </Empty>
          </Card>
        </Page>
      )
    }
    return (
      <Page>
        <Alert variant="destructive">
          <AlertDescription>{getErrorMessage(app.error)}</AlertDescription>
        </Alert>
      </Page>
    )
  }

  const Icon = app.data.isPublic ? Globe : Server

  return (
    <Page>
      <PageHeader
        tabTitle={app.data.name}
        above={
          <Breadcrumb>
            <BreadcrumbList>
              <BreadcrumbItem>
                <BreadcrumbLink asChild>
                  <Link to="/">Apps</Link>
                </BreadcrumbLink>
              </BreadcrumbItem>
              <BreadcrumbSeparator />
              <BreadcrumbItem>
                <BreadcrumbPage>{app.data.name}</BreadcrumbPage>
              </BreadcrumbItem>
            </BreadcrumbList>
          </Breadcrumb>
        }
        title={
          <span className="flex items-center gap-3">
            <span className="grid size-11 shrink-0 place-items-center rounded-2xl bg-white shadow-soft">
              <Icon className="size-5" />
            </span>
            <span className="min-w-0">{app.data.name}</span>
          </span>
        }
        description={
          <span className="flex flex-wrap items-center gap-2">
            <Badge variant="secondary">{app.data.isPublic ? 'Browser app' : 'Server app'}</Badge>
            {!app.data.registrationEnabled && <Badge variant="outline">Sign-ups off</Badge>}
            <span>
              {plural(app.data.userCount, 'user')} · {plural(app.data.activeSessionCount, 'active session')}
            </span>
          </span>
        }
      />
      <Tabs
        value={tab}
        onValueChange={(value) => setParams(value === 'overview' ? {} : { tab: value }, { replace: true })}
        className="gap-6"
      >
        <TabsList className="h-11! rounded-full bg-white p-1 shadow-soft">
          {tabs.map((t) => (
            <TabsTrigger key={t} value={t} className="rounded-full px-4 data-active:bg-ink data-active:text-white data-active:hover:text-white">
              {tabLabels[t]}
            </TabsTrigger>
          ))}
        </TabsList>
        <TabsContent value="overview">
          <AppOverviewTab app={app.data} />
        </TabsContent>
        <TabsContent value="users">
          <AppUsersTab app={app.data} />
        </TabsContent>
        <TabsContent value="sessions">
          <AppSessionsTab app={app.data} />
        </TabsContent>
        <TabsContent value="settings">
          <AppSettingsTab app={app.data} />
        </TabsContent>
      </Tabs>
    </Page>
  )
}
