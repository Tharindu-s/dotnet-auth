import { zodResolver } from '@hookform/resolvers/zod'
import { useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { Link, useNavigate } from 'react-router'
import { toast } from 'sonner'
import { FormField } from '@/components/FormField'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { FieldGroup } from '@/components/ui/field'
import { Spinner } from '@/components/ui/spinner'
import { getErrorMessage } from '@/lib/errors'
import { useAuthStore } from '@/stores/auth'
import { changePassword } from './api'
import { AuthLayout } from './AuthLayout'
import { PasswordChecklist } from './PasswordChecklist'
import { changePasswordSchema, type ChangePasswordInput } from './schemas'
import { reloadUser, signOut } from './session'

/** Forced on the first sign-in (mustChangePassword), and reachable from the sidebar */
export function ChangePasswordPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const forced = useAuthStore((s) => s.user?.mustChangePassword ?? false)
  const [serverError, setServerError] = useState<string | null>(null)
  const {
    register,
    control,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ChangePasswordInput>({ resolver: zodResolver(changePasswordSchema) })
  const newPassword = useWatch({ control, name: 'newPassword' }) ?? ''

  const onSubmit = handleSubmit(async ({ currentPassword, newPassword }) => {
    setServerError(null)
    try {
      await changePassword(currentPassword, newPassword)
      await reloadUser()
      toast.success('Password changed. Your other devices were signed out.')
      navigate('/', { replace: true })
    } catch (error) {
      setServerError(getErrorMessage(error))
    }
  })

  return (
    <AuthLayout
      tabTitle="Change password"
      title={forced ? 'Choose your password' : 'Change your password'}
      description={
        forced
          ? 'You signed in with the initial admin password. Replace it with your own before you continue.'
          : 'Your other devices are signed out when you change it.'
      }
      actions={
        forced ? (
          <Button variant="outline" className="rounded-full bg-white" onClick={() => void signOut(queryClient)}>
            Log out
          </Button>
        ) : (
          <Button asChild variant="outline" className="rounded-full bg-white">
            <Link to="/">Cancel</Link>
          </Button>
        )
      }
    >
      <form onSubmit={onSubmit} noValidate>
        <FieldGroup>
          {serverError && (
            <Alert variant="destructive">
              <AlertDescription>{serverError}</AlertDescription>
            </Alert>
          )}
          <FormField
            id="currentPassword"
            label="Current password"
            type="password"
            autoComplete="current-password"
            error={errors.currentPassword?.message}
            {...register('currentPassword')}
          />
          <FormField
            id="newPassword"
            label="New password"
            type="password"
            autoComplete="new-password"
            error={errors.newPassword?.message}
            description={<PasswordChecklist password={newPassword} />}
            {...register('newPassword')}
          />
          <FormField
            id="confirmPassword"
            label="Confirm new password"
            type="password"
            autoComplete="new-password"
            error={errors.confirmPassword?.message}
            {...register('confirmPassword')}
          />
          <Button type="submit" size="lg" className="h-11 w-full rounded-full" disabled={isSubmitting}>
            {isSubmitting && <Spinner />}
            {isSubmitting ? 'Saving…' : 'Change password'}
          </Button>
        </FieldGroup>
      </form>
    </AuthLayout>
  )
}
