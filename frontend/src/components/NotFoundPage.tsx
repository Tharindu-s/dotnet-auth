import { Compass } from 'lucide-react'
import type { ReactNode } from 'react'
import { Link } from 'react-router'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { Empty, EmptyContent, EmptyDescription, EmptyHeader, EmptyMedia, EmptyTitle } from '@/components/ui/empty'

/** A centered message card on the backdrop, for pages outside the app shell */
export function MessagePage({
  tabTitle,
  icon,
  title,
  description,
  action,
}: {
  tabTitle: string
  icon: ReactNode
  title: string
  description: string
  action: ReactNode
}) {
  return (
    <div className="app-backdrop flex min-h-svh items-center justify-center px-4">
      <title>{`${tabTitle} · Argus`}</title>
      <Card className="w-full max-w-md">
        <Empty>
          <EmptyHeader>
            <EmptyMedia variant="icon">{icon}</EmptyMedia>
            <EmptyTitle>{title}</EmptyTitle>
            <EmptyDescription>{description}</EmptyDescription>
          </EmptyHeader>
          <EmptyContent>{action}</EmptyContent>
        </Empty>
      </Card>
    </div>
  )
}

export function NotFoundPage() {
  return (
    <MessagePage
      tabTitle="Not found"
      icon={<Compass />}
      title="Page not found"
      description="There is nothing at this address."
      action={
        <Button asChild className="rounded-full">
          <Link to="/">Go home</Link>
        </Button>
      }
    />
  )
}
