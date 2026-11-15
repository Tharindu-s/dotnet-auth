import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useLocation, useNavigate, type Location } from 'react-router'
import { FormField } from '@/components/FormField'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { getErrorMessage } from '@/lib/errors'
import { AuthCard } from './AuthCard'
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
      const from = (location.state as { from?: Location } | null)?.from?.pathname ?? '/'
      navigate(from, { replace: true })
    } catch (error) {
      setServerError(getErrorMessage(error))
    }
  })

  return (
    <AuthCard
      tabTitle="Sign in"
      eyebrow="Secure sign-in"
      title="Welcome back."
      description="Log in with your email and password to pick up where you left off."
      headerLink={{ to: '/register', label: 'Create account' }}
      footer={
        <p>
          New here?{' '}
          <Link to="/register" className="font-medium text-foreground underline underline-offset-4">
            Create an account
          </Link>
        </p>
      }
    >
      <form onSubmit={onSubmit} noValidate className="grid gap-4">
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
        <Button type="submit" size="lg" className="mt-2 w-full" disabled={isSubmitting}>
          {isSubmitting ? 'Logging in…' : 'Log in'}
        </Button>
      </form>
    </AuthCard>
  )
}
