import { MonitorSmartphone } from 'lucide-react'
import { toast } from 'sonner'
import { Page, PageHeader } from '@/components/AppLayout'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Empty, EmptyHeader, EmptyMedia, EmptyTitle } from '@/components/ui/empty'
import { Skeleton } from '@/components/ui/skeleton'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { getErrorMessage } from '@/lib/errors'
import { formatDateTime } from '@/lib/format'
import type { Session } from '@/lib/types'
import { describeDevice } from './describeDevice'
import { useRevokeSession, useSessions } from './queries'

export function SessionsPage() {
  const sessions = useSessions()

  return (
    <Page>
      <PageHeader
        tabTitle="Sessions"
        title="Your sessions"
        description="Every device signed in to your admin account. Revoke any you don't recognize."
      />
      <Card>
        <CardContent>
          {sessions.isPending ? (
            <div className="grid gap-2" aria-label="Loading sessions">
              <Skeleton className="h-12" />
              <Skeleton className="h-12" />
            </div>
          ) : sessions.isError ? (
            <Alert variant="destructive">
              <AlertDescription>{getErrorMessage(sessions.error)}</AlertDescription>
            </Alert>
          ) : sessions.data.length === 0 ? (
            <Empty>
              <EmptyHeader>
                <EmptyMedia variant="icon">
                  <MonitorSmartphone />
                </EmptyMedia>
                <EmptyTitle>No active sessions</EmptyTitle>
              </EmptyHeader>
            </Empty>
          ) : (
            <SessionsTable sessions={sessions.data} />
          )}
        </CardContent>
      </Card>
    </Page>
  )
}

function SessionsTable({ sessions }: { sessions: Session[] }) {
  const revoke = useRevokeSession()

  const onRevoke = (session: Session) =>
    revoke.mutate(session.tokenId, {
      onSuccess: () => toast.success(`Signed out ${describeDevice(session.device)}.`),
      onError: (error) => toast.error(getErrorMessage(error)),
    })

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Device</TableHead>
          <TableHead className="hidden sm:table-cell">IP address</TableHead>
          <TableHead className="hidden md:table-cell">Signed in</TableHead>
          <TableHead className="hidden md:table-cell">Expires</TableHead>
          <TableHead className="text-right">
            <span className="sr-only">Actions</span>
          </TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {sessions.map((session) => (
          <TableRow key={session.tokenId}>
            <TableCell>
              <div className="flex items-center gap-2">
                <span title={session.device ?? undefined}>{describeDevice(session.device)}</span>
                {session.isCurrent && <Badge>This device</Badge>}
              </div>
            </TableCell>
            <TableCell className="hidden sm:table-cell">{session.ipAddress}</TableCell>
            <TableCell className="hidden md:table-cell">{formatDateTime(session.issuedAt)}</TableCell>
            <TableCell className="hidden md:table-cell">{formatDateTime(session.expiresAt)}</TableCell>
            <TableCell className="text-right">
              {session.isCurrent ? (
                <span className="hidden text-sm whitespace-nowrap text-muted-foreground sm:inline">Use Log out</span>
              ) : (
                <Button
                  variant="outline"
                  size="sm"
                  className="rounded-full bg-white"
                  disabled={revoke.isPending && revoke.variables === session.tokenId}
                  onClick={() => onRevoke(session)}
                >
                  Revoke
                </Button>
              )}
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  )
}
