import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useLocation, useNavigate, type Location } from 'react-router'
import { FormField } from '@/components/FormField'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { FieldGroup } from '@/components/ui/field'
import { Spinner } from '@/components/ui/spinner'
import { getErrorMessage } from '@/lib/errors'
import { AuthLayout } from './AuthLayout'
import { loginSchema, type LoginInput } from './schemas'
import { signIn } from './session'

export function LoginPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const [serverError, setServerError] = useState<string | null>(null)
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginInput>({ resolver: zodResolver(loginSchema) })

  const onSubmit = handleSubmit(async (values) => {
    setServerError(null)
    try {
      await signIn(values)
      // A first sign-in is sent on to /change-password by the route guard
      const from = (location.state as { from?: Location } | null)?.from?.pathname ?? '/'
      navigate(from, { replace: true })
    } catch (error) {
      setServerError(getErrorMessage(error))
    }
  })

  return (
    <AuthLayout
      tabTitle="Sign in"
      title="Welcome back"
      description="Sign in to manage the apps that use this auth server."
      footer={
        <p>
          First time? Use <code>ADMIN_EMAIL</code> from your <code>.env</code>. The password is <code>ADMIN_PASSWORD</code>, or
          the one printed in the API logs if you left it empty.
        </p>
      }
    >
      <form onSubmit={onSubmit} noValidate>
        <FieldGroup>
          {serverError && (
            <Alert variant="destructive">
              <AlertDescription>{serverError}</AlertDescription>
            </Alert>
          )}
          <FormField id="email" label="Email" type="email" autoComplete="email" error={errors.email?.message} {...register('email')} />
          <FormField
            id="password"
            label="Password"
            type="password"
            autoComplete="current-password"
            error={errors.password?.message}
            {...register('password')}
          />
          <Button type="submit" size="lg" className="h-11 w-full rounded-full" disabled={isSubmitting}>
            {isSubmitting && <Spinner />}
            {isSubmitting ? 'Logging in…' : 'Log in'}
          </Button>
        </FieldGroup>
      </form>
    </AuthLayout>
  )
}
