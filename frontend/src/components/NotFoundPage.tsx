import { Link } from 'react-router'
import { Eyebrow, GradientBlocks, SiteHeader } from '@/components/brand'
import { Button } from '@/components/ui/button'

export function NotFoundPage() {
  return (
    <div className="flex min-h-svh flex-col">
      <title>Not found · Argus</title>
      <SiteHeader />
      <main className="relative flex flex-1 items-center justify-center px-4 lg:mx-10 lg:mb-10">
        <GradientBlocks />
        <div className="relative z-10 grid justify-items-center gap-5 text-center">
          <Eyebrow>404</Eyebrow>
          <h1 className="display-title text-5xl sm:text-6xl">Page not found.</h1>
          <p className="text-muted-foreground">There is nothing at this address.</p>
          <Button asChild size="lg">
            <Link to="/">Go home</Link>
          </Button>
        </div>
      </main>
    </div>
  )
}
