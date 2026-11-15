import { zodResolver } from '@hookform/resolvers/zod'
import { Globe, Server } from 'lucide-react'
import { Controller, useForm, useWatch } from 'react-hook-form'
import { Link, useSearchParams } from 'react-router'
import { Page, PageHeader } from '@/components/AppLayout'
import { CopyInput } from '@/components/CopyInput'
import { FormField } from '@/components/FormField'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from '@/components/ui/breadcrumb'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Field, FieldContent, FieldDescription, FieldGroup, FieldLabel, FieldLegend, FieldSet, FieldTitle } from '@/components/ui/field'
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group'
import { Spinner } from '@/components/ui/spinner'
import { getErrorMessage } from '@/lib/errors'
import type { CreatedApp } from '@/lib/types'
import { OriginsField, SignUpsField } from './AppFormFields'
import { useCreateApp } from './queries'
import { newAppSchema, toAppInput, type NewAppValues } from './schemas'

const appTypes = [
  {
    value: 'server',
    icon: Server,
    title: 'Server app',
    description: 'Your backend calls this server. It gets an API key to keep secret.',
  },
  {
    value: 'browser',
    icon: Globe,
    title: 'Browser app',
    description: 'Code in the browser calls this server directly. No key; requests must come from an allowed origin.',
  },
] as const

function NewAppBreadcrumb() {
  return (
    <Breadcrumb>
      <BreadcrumbList>
        <BreadcrumbItem>
          <BreadcrumbLink asChild>
            <Link to="/">Apps</Link>
          </BreadcrumbLink>
        </BreadcrumbItem>
        <BreadcrumbSeparator />
        <BreadcrumbItem>
          <BreadcrumbPage>New app</BreadcrumbPage>
        </BreadcrumbItem>
      </BreadcrumbList>
    </Breadcrumb>
  )
}

export function NewAppPage() {
  const [params] = useSearchParams()
  const create = useCreateApp()
  const {
    register,
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<NewAppValues>({
    resolver: zodResolver(newAppSchema),
    defaultValues: {
      name: '',
      audience: '',
      isPublic: params.get('type') === 'browser',
      allowedOrigins: '',
      registrationEnabled: true,
    },
  })
  const isPublic = useWatch({ control, name: 'isPublic' })

  if (create.data) return <AppCreated app={create.data} />

  return (
    <Page className="max-w-3xl">
      <PageHeader
        tabTitle="New app"
        above={<NewAppBreadcrumb />}
        title="New app"
        description="Register one of your projects. Its users sign up and log in through this server."
      />
      <Card>
        <CardContent>
          <form onSubmit={handleSubmit(({ isPublic, ...values }) => create.mutate({ ...toAppInput(values), isPublic }))} noValidate>
            <FieldGroup>
              <FormField id="name" label="Name" placeholder="Notes" error={errors.name?.message} {...register('name')} />
              <FormField
                id="audience"
                label="Audience (optional)"
                placeholder="notes-api"
                description="The aud claim your project's API checks, so it only accepts tokens meant for it. Defaults to the app ID."
                error={errors.audience?.message}
                {...register('audience')}
              />
              <FieldSet>
                <FieldLegend variant="label">Type</FieldLegend>
                <Controller
                  control={control}
                  name="isPublic"
                  render={({ field }) => (
                    <RadioGroup
                      value={field.value ? 'browser' : 'server'}
                      onValueChange={(value) => field.onChange(value === 'browser')}
                      className="grid gap-3 sm:grid-cols-2"
                    >
                      {appTypes.map((type) => (
                        <FieldLabel key={type.value} htmlFor={`type-${type.value}`} className="rounded-2xl! bg-white">
                          <Field orientation="horizontal">
                            <FieldContent>
                              <FieldTitle>
                                <type.icon className="size-4" /> {type.title}
                              </FieldTitle>
                              <FieldDescription>{type.description}</FieldDescription>
                            </FieldContent>
                            <RadioGroupItem value={type.value} id={`type-${type.value}`} />
                          </Field>
                        </FieldLabel>
                      ))}
                    </RadioGroup>
                  )}
                />
              </FieldSet>
              {isPublic && <OriginsField error={errors.allowedOrigins?.message} {...register('allowedOrigins')} />}
              <Controller
                control={control}
                name="registrationEnabled"
                render={({ field }) => (
                  <SignUpsField
                    checked={field.value}
                    onChange={field.onChange}
                    description="Anyone can create an account in this app. Turn it off to allow only existing users."
                  />
                )}
              />
              {create.isError && (
                <Alert variant="destructive">
                  <AlertDescription>{getErrorMessage(create.error)}</AlertDescription>
                </Alert>
              )}
              <Field orientation="horizontal">
                <Button type="submit" size="lg" className="rounded-full" disabled={create.isPending}>
                  {create.isPending && <Spinner />}
                  {create.isPending ? 'Creating…' : 'Create app'}
                </Button>
                <Button asChild variant="outline" size="lg" className="rounded-full bg-white">
                  <Link to="/">Cancel</Link>
                </Button>
              </Field>
            </FieldGroup>
          </form>
        </CardContent>
      </Card>
    </Page>
  )
}

function AppCreated({ app }: { app: CreatedApp }) {
  return (
    <Page className="max-w-3xl">
      <PageHeader tabTitle={app.name} above={<NewAppBreadcrumb />} title={`${app.name} is ready.`} description="Put these values in your project's config." />
      <Card>
        <CardContent>
          <FieldGroup>
            <CopyInput label="App ID" value={app.id} description="Sent as the X-App-Id header on register, login and logout." />
            {app.rawApiKey && (
              <Alert>
                <AlertTitle>Save the API key now</AlertTitle>
                <AlertDescription className="grid gap-3">
                  <p>Only a hash is stored, so it can&apos;t be shown again. If you lose it, rotate it on the app&apos;s page.</p>
                  <CopyInput label="API key" value={app.rawApiKey} description="Sent as the X-Api-Key header. Keep it on your server." />
                </AlertDescription>
              </Alert>
            )}
            <CopyInput label="Audience" value={app.audience} description="The aud claim to check in your API." />
            <Field orientation="horizontal">
              <Button asChild size="lg" className="rounded-full">
                <Link to={`/apps/${app.id}`}>Open app</Link>
              </Button>
              <Button asChild variant="outline" size="lg" className="rounded-full bg-white">
                <Link to="/">Back to apps</Link>
              </Button>
            </Field>
          </FieldGroup>
        </CardContent>
      </Card>
    </Page>
  )
}
