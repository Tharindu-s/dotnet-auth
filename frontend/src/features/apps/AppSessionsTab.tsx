import { MonitorSmartphone } from 'lucide-react'
import { toast } from 'sonner'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Empty, EmptyDescription, EmptyHeader, EmptyMedia, EmptyTitle } from '@/components/ui/empty'
import { Skeleton } from '@/components/ui/skeleton'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { describeDevice } from '@/features/sessions/describeDevice'
import { getErrorMessage } from '@/lib/errors'
import { formatDateTime, formatRelative } from '@/lib/format'
import type { AdminSession, App } from '@/lib/types'
import { useAppSessions, useRevokeAppSession } from './queries'

export function AppSessionsTab({ app }: { app: App }) {
  const sessions = useAppSessions(app.id)

  return (
    <Card>
      <CardHeader>
        <CardTitle>Active sessions</CardTitle>
        <CardDescription>Every signed-in device, newest first. A revoked session ends at its next refresh, within 15 minutes.</CardDescription>
      </CardHeader>
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
              <EmptyTitle>No one is signed in</EmptyTitle>
              <EmptyDescription>Sessions appear here when this app&apos;s users sign in.</EmptyDescription>
            </EmptyHeader>
          </Empty>
        ) : (
          <SessionsTable appId={app.id} sessions={sessions.data} />
        )}
      </CardContent>
    </Card>
  )
}

function SessionsTable({ appId, sessions }: { appId: string; sessions: AdminSession[] }) {
  const revoke = useRevokeAppSession(appId)

  const onRevoke = (session: AdminSession) =>
    revoke.mutate(session.tokenId, {
      onSuccess: () => toast.success(`Signed ${session.userEmail ?? 'the user'} out of ${describeDevice(session.device)}.`),
      onError: (error) => toast.error(getErrorMessage(error)),
    })

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>User</TableHead>
          <TableHead>Device</TableHead>
          <TableHead className="hidden sm:table-cell">IP address</TableHead>
          <TableHead className="hidden md:table-cell">Signed in</TableHead>
          <TableHead className="hidden lg:table-cell">Expires</TableHead>
          <TableHead className="text-right">
            <span className="sr-only">Actions</span>
          </TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {sessions.map((session) => (
          <TableRow key={session.tokenId}>
            <TableCell className="font-medium text-ink">{session.userEmail}</TableCell>
            <TableCell>
              <span title={session.device ?? undefined}>{describeDevice(session.device)}</span>
            </TableCell>
            <TableCell className="hidden sm:table-cell">{session.ipAddress}</TableCell>
            <TableCell className="hidden md:table-cell" title={formatDateTime(session.issuedAt)}>
              {formatRelative(session.issuedAt)}
            </TableCell>
            <TableCell className="hidden lg:table-cell">{formatDateTime(session.expiresAt)}</TableCell>
            <TableCell className="text-right">
              <Button
                variant="outline"
                size="sm"
                className="rounded-full bg-white"
                disabled={revoke.isPending && revoke.variables === session.tokenId}
                onClick={() => onRevoke(session)}
              >
                Revoke
              </Button>
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  )
}
