import { useQuery } from '@tanstack/react-query'
import { RefreshCw } from 'lucide-react'
import { toast } from 'sonner'
import { CodeBlock, CopyInput } from '@/components/CopyInput'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
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
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Field, FieldDescription, FieldGroup, FieldLabel } from '@/components/ui/field'
import { getArgusConfig } from '@/lib/config'
import { getErrorMessage } from '@/lib/errors'
import type { App } from '@/lib/types'
import { useRotateApiKey } from './queries'

export function AppOverviewTab({ app }: { app: App }) {
  const argus = useQuery({ queryKey: ['argus-config'], queryFn: getArgusConfig, staleTime: Infinity })
  // The browser reaches the API through this origin (the Vite proxy in dev, nginx in Docker)
  const baseUrl = window.location.origin
  const issuer = argus.data?.issuer ?? '…'

  return (
    <div className="grid gap-6 lg:grid-cols-[minmax(0,2fr)_minmax(0,3fr)]">
      <Card className="self-start">
        <CardHeader>
          <CardTitle>Credentials</CardTitle>
          <CardDescription>What your project needs to sign users in and check their tokens.</CardDescription>
        </CardHeader>
        <CardContent>
          <FieldGroup>
            <CopyInput label="App ID" value={app.id} description="The X-App-Id header." />
            {app.isPublic ? (
              <Field>
                <FieldLabel>Allowed origins</FieldLabel>
                <FieldDescription>Browser apps have no API key. Requests are accepted from {app.allowedOrigins.join(', ')}.</FieldDescription>
              </Field>
            ) : (
              <ApiKey appId={app.id} />
            )}
            <CopyInput label="Audience" value={app.audience} description="The aud claim on this app's tokens." />
            <CopyInput label="Issuer" value={issuer} description="The iss claim on every token." />
            <CopyInput label="JWKS URL" value={`${baseUrl}/.well-known/jwks.json`} description="Public keys that verify token signatures." />
          </FieldGroup>
        </CardContent>
      </Card>
      <Card className="self-start">
        <CardHeader>
          <CardTitle>Connect your project</CardTitle>
          <CardDescription>Sign users in by calling this server, then check the access tokens in your API.</CardDescription>
        </CardHeader>
        <CardContent className="grid gap-6">
          <CodeBlock label={app.isPublic ? 'Sign a user in (browser)' : 'Sign a user in (from your server)'} code={signInSnippet(app, baseUrl)} />
          <CodeBlock label="Validate tokens in an ASP.NET Core API" code={validationSnippet(app, baseUrl, issuer)} />
          <p className="text-sm text-muted-foreground">
            Other stacks: verify the RS256 signature with a key from the JWKS URL, then check <code>iss</code>, <code>aud</code> and{' '}
            <code>app_id</code>. Access tokens last 15 minutes; refresh them with <code>POST /api/auth/refresh</code> (the refresh
            token is an HttpOnly cookie).
          </p>
        </CardContent>
      </Card>
    </div>
  )
}

function ApiKey({ appId }: { appId: string }) {
  const rotate = useRotateApiKey(appId)

  if (rotate.data) {
    return (
      <Alert>
        <AlertTitle>New API key. Save it now.</AlertTitle>
        <AlertDescription className="grid gap-3">
          <p>It can&apos;t be shown again, and the old key has stopped working.</p>
          <CopyInput label="API key" value={rotate.data} />
        </AlertDescription>
      </Alert>
    )
  }

  return (
    <Field>
      <FieldLabel>API key</FieldLabel>
      <FieldDescription>Shown once when the app was created. Only a hash is stored.</FieldDescription>
      <AlertDialog>
        <AlertDialogTrigger asChild>
          <Button variant="outline" size="sm" className="w-fit rounded-full bg-white" disabled={rotate.isPending}>
            <RefreshCw /> Rotate API key
          </Button>
        </AlertDialogTrigger>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Rotate the API key?</AlertDialogTitle>
            <AlertDialogDescription>
              The current key stops working immediately. Update your project with the new key, which is shown once.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction onClick={() => rotate.mutate(undefined, { onError: (error) => toast.error(getErrorMessage(error)) })}>
              Rotate key
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </Field>
  )
}

function signInSnippet(app: App, baseUrl: string) {
  if (app.isPublic) {
    return `// From ${app.allowedOrigins[0] ?? 'an allowed origin'}. The browser sends the Origin header itself.
const res = await fetch('${baseUrl}/api/auth/login', {
  method: 'POST',
  credentials: 'include', // receive the refresh cookie
  headers: { 'Content-Type': 'application/json', 'X-App-Id': '${app.id}' },
  body: JSON.stringify({ email, password }),
})
const { accessToken } = await res.json()`
  }
  return `curl -X POST ${baseUrl}/api/auth/login \\
  -H "Content-Type: application/json" \\
  -H "X-App-Id: ${app.id}" \\
  -H "X-Api-Key: <your API key>" \\
  -d '{"email":"jane@example.com","password":"Passw0rd!"}'`
}

function validationSnippet(app: App, baseUrl: string, issuer: string) {
  return `// Program.cs (Microsoft.AspNetCore.Authentication.JwtBearer)
var jwks = new JsonWebKeySet(await new HttpClient().GetStringAsync("${baseUrl}/.well-known/jwks.json"));

builder.Services.AddAuthentication().AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = "${issuer}",
        ValidAudience = "${app.audience}",
        IssuerSigningKeys = jwks.GetSigningKeys(),
    };
});

// Also reject tokens from your other apps:
// User.FindFirst("app_id")?.Value == "${app.id}"`
}
