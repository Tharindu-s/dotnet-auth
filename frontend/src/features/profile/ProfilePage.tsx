import { useQuery } from '@tanstack/react-query'
import type { ReactNode } from 'react'
import { PageHeader } from '@/components/AppLayout'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent } from '@/components/ui/card'
import { fetchMe } from '@/features/auth/api'
import { useAuthStore } from '@/stores/auth'

export function ProfilePage() {
  const user = useAuthStore((s) => s.user)
  // The store already has the basics; /me adds roles and the app ID
  const me = useQuery({ queryKey: ['me'], queryFn: fetchMe })

  if (!user) return null

  const rows: [string, ReactNode][] = [
    ['Email', user.email],
    ['City', user.city],
    [
      'Roles',
      me.data
        ? me.data.roles.map((role) => (
            <Badge key={role} className="rounded-full bg-white px-2.5 text-ink">
              {role}
            </Badge>
          ))
        : '…',
    ],
    ['App ID', <code key="app" className="text-xs">{me.data?.appId ?? '…'}</code>],
    ['User ID', <code key="user" className="text-xs">{user.id}</code>],
  ]

  return (
    <>
      <PageHeader tabTitle="Profile" eyebrow="Your account" title={user.fullName} description="The details this service holds about you." />
      <Card>
        <CardContent>
          <dl className="grid gap-x-8 gap-y-4 sm:grid-cols-[8rem_1fr]">
            {rows.map(([label, value]) => (
              <div key={label} className="contents">
                <dt className="text-sm text-muted-foreground">{label}</dt>
                <dd className="flex flex-wrap items-center gap-1.5 text-sm break-all text-ink">{value}</dd>
              </div>
            ))}
          </dl>
        </CardContent>
      </Card>
    </>
  )
}
