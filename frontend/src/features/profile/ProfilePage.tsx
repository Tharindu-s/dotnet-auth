import { KeyRound } from 'lucide-react'
import { Link } from 'react-router'
import { Page, PageHeader } from '@/components/AppLayout'
import { CopyInput } from '@/components/CopyInput'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { FieldGroup } from '@/components/ui/field'
import { Item, ItemActions, ItemContent, ItemDescription, ItemMedia, ItemTitle } from '@/components/ui/item'
import { initials } from '@/lib/format'
import { useAuthStore } from '@/stores/auth'

export function ProfilePage() {
  const user = useAuthStore((s) => s.user)
  if (!user) return null

  return (
    <Page className="max-w-3xl">
      <PageHeader tabTitle="Profile" title="Profile" description="The admin account you sign in to this console with." />
      <div className="grid gap-6">
        <Card>
          <CardContent>
            <Item className="p-0">
              <ItemMedia>
                <Avatar className="size-14">
                  <AvatarFallback className="text-lg">{initials(user.fullName)}</AvatarFallback>
                </Avatar>
              </ItemMedia>
              <ItemContent>
                <ItemTitle className="text-lg">{user.fullName}</ItemTitle>
                <ItemDescription>{user.email}</ItemDescription>
                <div className="flex gap-1.5">
                  {user.roles.map((role) => (
                    <Badge key={role} variant="secondary">
                      {role}
                    </Badge>
                  ))}
                </div>
              </ItemContent>
              <ItemActions>
                <Button asChild variant="outline" className="rounded-full bg-white">
                  <Link to="/change-password">
                    <KeyRound /> Change password
                  </Link>
                </Button>
              </ItemActions>
            </Item>
          </CardContent>
        </Card>
        <Card>
          <CardContent>
            <FieldGroup>
              <CopyInput label="Argus app ID" value={user.appId} description="The app this console signs you in through." />
              <CopyInput label="User ID" value={user.id} />
            </FieldGroup>
          </CardContent>
        </Card>
      </div>
    </Page>
  )
}
