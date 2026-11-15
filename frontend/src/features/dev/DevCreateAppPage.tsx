import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation } from '@tanstack/react-query'
import { Copy } from 'lucide-react'
import { useForm, useWatch } from 'react-hook-form'
import { Link } from 'react-router'
import { toast } from 'sonner'
import { z } from 'zod'
import { Eyebrow, SiteHeader } from '@/components/brand'
import { FormField } from '@/components/FormField'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Checkbox } from '@/components/ui/checkbox'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { api } from '@/lib/api'
import { getErrorMessage } from '@/lib/errors'
import type { CreatedApp } from '@/lib/types'

// Only routed in dev builds (see router.tsx), so none of this ships to production

const schema = z.object({
  name: z.string().trim().min(1, 'Enter a name.').max(100),
  audience: z.string().trim().max(200),
  isPublic: z.boolean(),
  allowedOrigins: z.string(),
  // Sent once with the request, never stored or bundled
  adminKey: z.string().min(1, 'Enter the value of Security:AdminApiKey.'),
})

type FormValues = z.infer<typeof schema>

async function createApp({ adminKey, allowedOrigins, audience, ...values }: FormValues) {
  const { data } = await api.post<CreatedApp>(
    '/api/application-client',
    {
      ...values,
      audience: audience || null,
      allowedOrigins: allowedOrigins.split(/\s+/).filter(Boolean),
    },
    { headers: { 'X-Admin-Key': adminKey }, skipAuthRefresh: true },
  )
  return data
}

export default function DevCreateAppPage() {
  const create = useMutation({ mutationFn: createApp })
  const {
    register,
    control,
    handleSubmit,
    setValue,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      name: 'React frontend',
      audience: '',
      isPublic: true,
      allowedOrigins: window.location.origin,
      adminKey: '',
    },
  })
  const isPublic = useWatch({ control, name: 'isPublic' })

  return (
    <div className="min-h-svh bg-background">
      <title>Dev tools · Argus</title>
      <SiteHeader
        actions={
          <Button asChild variant="outline">
            <Link to="/">Back to the app</Link>
          </Button>
        }
      />
      <main className="mx-auto grid max-w-xl gap-8 px-4 py-10 sm:py-14">
        <div className="grid gap-4">
          <Eyebrow className="justify-self-start">Dev tools</Eyebrow>
          <h1 className="display-title text-4xl sm:text-5xl">New application client.</h1>
          <p className="text-[0.9375rem] text-muted-foreground">
            Dev builds only. Calls POST /api/application-client with your admin key.
          </p>
        </div>
        <Card>
          <CardContent>
            <form onSubmit={handleSubmit((values) => create.mutate(values))} noValidate className="grid gap-4">
              <FormField id="name" label="Name" error={errors.name?.message} {...register('name')} />
              <FormField
                id="audience"
                label="Audience (optional)"
                placeholder="notes-api"
                hint="The aud claim your own API will check. Defaults to the app ID."
                error={errors.audience?.message}
                {...register('audience')}
              />
              <div className="flex items-start gap-2">
                <Checkbox
                  id="isPublic"
                  checked={isPublic}
                  onCheckedChange={(checked) => setValue('isPublic', checked === true)}
                />
                <div className="grid gap-1">
                  <Label htmlFor="isPublic">Public client (browser app)</Label>
                  <p className="text-sm text-muted-foreground">
                    No API key. Requests must come from an allowed origin. Use this for this frontend.
                  </p>
                </div>
              </div>
              {isPublic && (
                <div className="grid gap-1.5">
                  <Label htmlFor="allowedOrigins">Allowed origins</Label>
                  <Textarea id="allowedOrigins" rows={2} {...register('allowedOrigins')} />
                  <p className="text-sm text-muted-foreground">One per line, such as http://localhost:5173.</p>
                </div>
              )}
              <FormField
                id="adminKey"
                label="Admin key"
                type="password"
                autoComplete="off"
                error={errors.adminKey?.message}
                {...register('adminKey')}
              />
              {create.isError && (
                <Alert variant="destructive">
                  <AlertDescription>{getErrorMessage(create.error)}</AlertDescription>
                </Alert>
              )}
              <Button type="submit" size="lg" disabled={create.isPending}>
                {create.isPending ? 'Creating…' : 'Create'}
              </Button>
            </form>
          </CardContent>
        </Card>
        {create.data && <CreatedAppDetails app={create.data} />}
      </main>
    </div>
  )
}

function CreatedAppDetails({ app }: { app: CreatedApp }) {
  return (
    <Alert>
      <AlertTitle>App created</AlertTitle>
      <AlertDescription className="grid gap-3">
        <CopyRow label="App ID" value={app.id} />
        {app.rawApiKey && <CopyRow label="API key (shown once)" value={app.rawApiKey} />}
        <p>
          Audience: <code>{app.audience}</code>
        </p>
        {app.isPublic && (
          <p>
            To use it here, put <code>VITE_APP_ID={app.id}</code> in <code>frontend/.env.local</code> and restart{' '}
            <code>npm run dev</code>.
          </p>
        )}
      </AlertDescription>
    </Alert>
  )
}

function CopyRow({ label, value }: { label: string; value: string }) {
  const copy = async () => {
    await navigator.clipboard.writeText(value)
    toast.success(`${label} copied.`)
  }
  return (
    <div className="grid gap-1">
      <span className="text-xs font-medium">{label}</span>
      <div className="flex items-center gap-2">
        <code className="min-w-0 flex-1 break-all rounded-lg bg-surface px-2 py-1 text-xs">{value}</code>
        <Button type="button" variant="ghost" size="icon-sm" onClick={copy} aria-label={`Copy ${label}`}>
          <Copy aria-hidden />
        </Button>
      </div>
    </div>
  )
}
