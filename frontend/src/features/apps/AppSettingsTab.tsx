import { zodResolver } from '@hookform/resolvers/zod'
import { Trash2 } from 'lucide-react'
import { useState } from 'react'
import { Controller, useForm, useWatch } from 'react-hook-form'
import { useNavigate } from 'react-router'
import { toast } from 'sonner'
import { FormField } from '@/components/FormField'
import { Alert, AlertDescription } from '@/components/ui/alert'
import {
  AlertDialog,
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
import { Field, FieldGroup, FieldLabel } from '@/components/ui/field'
import { Input } from '@/components/ui/input'
import { Spinner } from '@/components/ui/spinner'
import { getErrorMessage } from '@/lib/errors'
import type { App } from '@/lib/types'
import { OriginsField, SignUpsField } from './AppFormFields'
import { useDeleteApp, useUpdateApp } from './queries'
import { appSettingsSchema, toAppInput, type AppSettingsValues } from './schemas'

export function AppSettingsTab({ app }: { app: App }) {
  return (
    <div className="grid max-w-2xl gap-6">
      {/* Keyed by app so the form resets if the app changes underneath it */}
      <SettingsForm key={app.id} app={app} />
      <DangerZone app={app} />
    </div>
  )
}

function SettingsForm({ app }: { app: App }) {
  const update = useUpdateApp(app.id)
  const {
    register,
    control,
    handleSubmit,
    reset,
    formState: { errors, isDirty },
  } = useForm<AppSettingsValues>({
    resolver: zodResolver(appSettingsSchema),
    defaultValues: {
      name: app.name,
      audience: app.audience,
      allowedOrigins: app.allowedOrigins.join('\n'),
      registrationEnabled: app.registrationEnabled,
    },
  })
  const audience = useWatch({ control, name: 'audience' })

  const onSubmit = handleSubmit((values) => {
    if (app.isPublic && toAppInput(values).allowedOrigins.length === 0) {
      return toast.error('A browser app needs at least one allowed origin.')
    }
    update.mutate(toAppInput(values), {
      onSuccess: (saved) => {
        reset({
          name: saved.name,
          audience: saved.audience,
          allowedOrigins: saved.allowedOrigins.join('\n'),
          registrationEnabled: saved.registrationEnabled,
        })
        toast.success('Settings saved.')
      },
    })
  })

  return (
    <Card>
      <CardHeader>
        <CardTitle>Settings</CardTitle>
        <CardDescription>Whether the app is a server or browser app can&apos;t change after it&apos;s created.</CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={onSubmit} noValidate>
          <FieldGroup>
            <FormField id="name" label="Name" error={errors.name?.message} {...register('name')} />
            <FormField
              id="audience"
              label="Audience"
              description={
                audience.trim() !== app.audience
                  ? 'Changing it makes your API reject tokens issued before the change, until users refresh or sign in again.'
                  : 'The aud claim your API checks. Leave blank to use the app ID.'
              }
              error={errors.audience?.message}
              {...register('audience')}
            />
            {app.isPublic && <OriginsField error={errors.allowedOrigins?.message} {...register('allowedOrigins')} />}
            <Controller
              control={control}
              name="registrationEnabled"
              render={({ field }) => (
                <SignUpsField
                  checked={field.value}
                  onChange={field.onChange}
                  description="When off, POST /api/auth/register is refused and only existing users can sign in."
                />
              )}
            />
            {update.isError && (
              <Alert variant="destructive">
                <AlertDescription>{getErrorMessage(update.error)}</AlertDescription>
              </Alert>
            )}
            <Field orientation="horizontal">
              <Button type="submit" className="rounded-full" disabled={!isDirty || update.isPending}>
                {update.isPending && <Spinner />}
                {update.isPending ? 'Saving…' : 'Save changes'}
              </Button>
            </Field>
          </FieldGroup>
        </form>
      </CardContent>
    </Card>
  )
}

function DangerZone({ app }: { app: App }) {
  const navigate = useNavigate()
  const remove = useDeleteApp(app.id)
  const [confirmation, setConfirmation] = useState('')

  const onDelete = () =>
    remove.mutate(undefined, {
      onSuccess: () => {
        toast.success(`${app.name} was deleted.`)
        navigate('/', { replace: true })
      },
      onError: (error) => toast.error(getErrorMessage(error)),
    })

  return (
    <Card className="ring-destructive/20">
      <CardHeader>
        <CardTitle className="text-destructive">Delete app</CardTitle>
        <CardDescription>
          Deletes the app, its {app.userCount === 1 ? 'user' : `${app.userCount} users`} and every session. This can&apos;t be undone.
        </CardDescription>
      </CardHeader>
      <CardContent>
        <AlertDialog onOpenChange={(open) => !open && setConfirmation('')}>
          <AlertDialogTrigger asChild>
            <Button variant="destructive" className="rounded-full">
              <Trash2 /> Delete app
            </Button>
          </AlertDialogTrigger>
          <AlertDialogContent>
            <AlertDialogHeader>
              <AlertDialogTitle>Delete {app.name}?</AlertDialogTitle>
              <AlertDialogDescription>
                Its users and sessions are deleted too. Access tokens already issued keep working until they expire (at most 15 minutes).
              </AlertDialogDescription>
            </AlertDialogHeader>
            <Field>
              <FieldLabel htmlFor="confirmDelete">
                Type <strong className="text-ink">{app.name}</strong> to confirm
              </FieldLabel>
              <Input id="confirmDelete" autoComplete="off" value={confirmation} onChange={(event) => setConfirmation(event.target.value)} />
            </Field>
            <AlertDialogFooter>
              <AlertDialogCancel>Cancel</AlertDialogCancel>
              <Button variant="destructive" disabled={confirmation !== app.name || remove.isPending} onClick={onDelete}>
                {remove.isPending && <Spinner />}
                Delete this app
              </Button>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialog>
      </CardContent>
    </Card>
  )
}
