import { Lock, LockOpen, Search, Trash2, Users } from 'lucide-react'
import { useState } from 'react'
import { toast } from 'sonner'
import { Alert, AlertDescription } from '@/components/ui/alert'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from '@/components/ui/alert-dialog'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Empty, EmptyDescription, EmptyHeader, EmptyMedia, EmptyTitle } from '@/components/ui/empty'
import { InputGroup, InputGroupAddon, InputGroupButton, InputGroupInput } from '@/components/ui/input-group'
import { Pagination, PaginationContent, PaginationItem, PaginationNext, PaginationPrevious } from '@/components/ui/pagination'
import { Skeleton } from '@/components/ui/skeleton'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { getErrorMessage } from '@/lib/errors'
import { formatRelative, initials } from '@/lib/format'
import type { AdminUser, App } from '@/lib/types'
import { useAppUsers, useUserAction } from './queries'

const PAGE_SIZE = 25

export function AppUsersTab({ app }: { app: App }) {
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const users = useAppUsers(app.id, search, page, PAGE_SIZE)

  const pageCount = users.data ? Math.max(1, Math.ceil(users.data.total / PAGE_SIZE)) : 1

  return (
    <Card>
      <CardContent className="grid gap-4">
        <form
          role="search"
          className="max-w-sm"
          onSubmit={(event) => {
            event.preventDefault()
            setSearch(searchInput.trim())
            setPage(1)
          }}
        >
          <InputGroup className="h-10 rounded-full bg-white">
            <InputGroupAddon>
              <Search />
            </InputGroupAddon>
            <InputGroupInput
              aria-label="Search users"
              placeholder="Search by email or name"
              value={searchInput}
              onChange={(event) => setSearchInput(event.target.value)}
            />
            <InputGroupAddon align="inline-end">
              <InputGroupButton type="submit" className="rounded-full">
                Search
              </InputGroupButton>
            </InputGroupAddon>
          </InputGroup>
        </form>

        {users.isPending ? (
          <div className="grid gap-2" aria-label="Loading users">
            <Skeleton className="h-12" />
            <Skeleton className="h-12" />
          </div>
        ) : users.isError ? (
          <Alert variant="destructive">
            <AlertDescription>{getErrorMessage(users.error)}</AlertDescription>
          </Alert>
        ) : users.data.items.length === 0 ? (
          <Empty>
            <EmptyHeader>
              <EmptyMedia variant="icon">
                <Users />
              </EmptyMedia>
              <EmptyTitle>{search ? 'No matches' : 'No users yet'}</EmptyTitle>
              <EmptyDescription>{search ? `No users match “${search}”.` : 'No one has signed up to this app yet.'}</EmptyDescription>
            </EmptyHeader>
          </Empty>
        ) : (
          <>
            <UsersTable appId={app.id} users={users.data.items} />
            {pageCount > 1 && (
              <Pagination>
                <PaginationContent>
                  <PaginationItem>
                    <PaginationPrevious
                      href="#"
                      aria-disabled={page <= 1}
                      className={page <= 1 ? 'pointer-events-none opacity-50' : undefined}
                      onClick={(event) => {
                        event.preventDefault()
                        setPage((p) => Math.max(1, p - 1))
                      }}
                    />
                  </PaginationItem>
                  <PaginationItem className="px-3 text-sm text-muted-foreground">
                    Page {page} of {pageCount}
                  </PaginationItem>
                  <PaginationItem>
                    <PaginationNext
                      href="#"
                      aria-disabled={page >= pageCount}
                      className={page >= pageCount ? 'pointer-events-none opacity-50' : undefined}
                      onClick={(event) => {
                        event.preventDefault()
                        setPage((p) => Math.min(pageCount, p + 1))
                      }}
                    />
                  </PaginationItem>
                </PaginationContent>
              </Pagination>
            )}
            <p className="text-sm text-muted-foreground">
              {users.data.total} {users.data.total === 1 ? 'user' : 'users'}
            </p>
          </>
        )}
      </CardContent>
    </Card>
  )
}

type UserAction = 'lock' | 'unlock' | 'delete'

const messages: Record<UserAction, (email: string) => string> = {
  lock: (email) => `${email} is locked and signed out everywhere.`,
  unlock: (email) => `${email} can sign in again.`,
  delete: (email) => `${email} was deleted.`,
}

function UsersTable({ appId, users }: { appId: string; users: AdminUser[] }) {
  const action = useUserAction(appId)

  const run = (user: AdminUser, kind: UserAction) =>
    action.mutate(
      { userId: user.id, action: kind },
      {
        onSuccess: () => toast.success(messages[kind](user.email)),
        onError: (error) => toast.error(getErrorMessage(error)),
      },
    )

  const busy = (user: AdminUser) => action.isPending && action.variables?.userId === user.id

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>User</TableHead>
          <TableHead className="hidden md:table-cell">Sessions</TableHead>
          <TableHead className="hidden md:table-cell">Last sign-in</TableHead>
          <TableHead className="text-right">
            <span className="sr-only">Actions</span>
          </TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {users.map((user) => (
          <TableRow key={user.id}>
            <TableCell>
              <div className="flex items-center gap-3">
                <Avatar className="size-8">
                  <AvatarFallback className="text-xs">{initials(user.fullName || user.email)}</AvatarFallback>
                </Avatar>
                <div className="grid min-w-0">
                  <div className="flex flex-wrap items-center gap-2">
                    <span className="truncate font-medium text-ink">{user.email}</span>
                    {user.isLockedOut && <Badge variant="destructive">Locked</Badge>}
                  </div>
                  {user.fullName && <span className="truncate text-xs text-muted-foreground">{user.fullName}</span>}
                </div>
              </div>
            </TableCell>
            <TableCell className="hidden md:table-cell">{user.activeSessionCount}</TableCell>
            <TableCell className="hidden md:table-cell">{user.lastSignInAt ? formatRelative(user.lastSignInAt) : 'Never'}</TableCell>
            <TableCell>
              <div className="flex justify-end gap-2">
                {user.isLockedOut ? (
                  <Button variant="outline" size="sm" className="rounded-full bg-white" disabled={busy(user)} onClick={() => run(user, 'unlock')}>
                    <LockOpen /> Unlock
                  </Button>
                ) : (
                  <ConfirmAction
                    trigger={
                      <Button variant="outline" size="sm" className="rounded-full bg-white" disabled={busy(user)} aria-label={`Lock ${user.email}`}>
                        <Lock /> Lock
                      </Button>
                    }
                    title={`Lock ${user.email}?`}
                    description="They're signed out everywhere and can't sign in until you unlock them. Access tokens already issued work until they expire (at most 15 minutes)."
                    confirmLabel="Lock user"
                    onConfirm={() => run(user, 'lock')}
                  />
                )}
                <ConfirmAction
                  trigger={
                    <Button variant="outline" size="sm" className="rounded-full bg-white" disabled={busy(user)} aria-label={`Delete ${user.email}`}>
                      <Trash2 /> Delete
                    </Button>
                  }
                  title={`Delete ${user.email}?`}
                  description="The account and its sessions are deleted. This can't be undone."
                  confirmLabel="Delete user"
                  destructive
                  onConfirm={() => run(user, 'delete')}
                />
              </div>
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  )
}

function ConfirmAction({
  trigger,
  title,
  description,
  confirmLabel,
  destructive,
  onConfirm,
}: {
  trigger: React.ReactNode
  title: string
  description: string
  confirmLabel: string
  destructive?: boolean
  onConfirm: () => void
}) {
  return (
    <AlertDialog>
      <AlertDialogTrigger asChild>{trigger}</AlertDialogTrigger>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>{title}</AlertDialogTitle>
          <AlertDialogDescription>{description}</AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel>Cancel</AlertDialogCancel>
          <AlertDialogAction variant={destructive ? 'destructive' : 'default'} onClick={onConfirm}>
            {confirmLabel}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
