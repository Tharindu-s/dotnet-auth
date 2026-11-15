import { zodResolver } from '@hookform/resolvers/zod'
import { Check, X } from 'lucide-react'
import { useState } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { Link, useNavigate } from 'react-router'
import { FormField } from '@/components/FormField'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { getErrorMessage } from '@/lib/errors'
import { cn } from '@/lib/utils'
import { AuthCard } from './AuthCard'
import { passwordRules, registerSchema, type RegisterInput } from './schemas'
import { signUp } from './session'

export function RegisterPage() {
  const navigate = useNavigate()
  const [serverError, setServerError] = useState<string | null>(null)
  const {
    register,
    control,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<RegisterInput>({ resolver: zodResolver(registerSchema) })
  const password = useWatch({ control, name: 'password' }) ?? ''

  const onSubmit = handleSubmit(async (values) => {
    setServerError(null)
    try {
      await signUp(values)
      navigate('/', { replace: true })
    } catch (error) {
      setServerError(getErrorMessage(error))
    }
  })

  return (
    <AuthCard
      tabTitle="Create account"
      eyebrow="Takes under a minute"
      title="Create your account."
      description="One account, signed in on every device you use. Sessions you can see and revoke."
      headerLink={{ to: '/login', label: 'Log in' }}
      footer={
        <p>
          Already have an account?{' '}
          <Link to="/login" className="font-medium text-foreground underline underline-offset-4">
            Log in
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
        <FormField id="fullName" label="Full name" autoComplete="name" error={errors.fullName?.message} {...register('fullName')} />
        <FormField id="email" label="Email" type="email" autoComplete="email" error={errors.email?.message} {...register('email')} />
        <FormField id="city" label="City" autoComplete="address-level2" error={errors.city?.message} {...register('city')} />
        <FormField
          id="password"
          label="Password"
          type="password"
          autoComplete="new-password"
          error={errors.password?.message}
          hint={<PasswordChecklist password={password} />}
          {...register('password')}
        />
        <FormField
          id="confirmPassword"
          label="Confirm password"
          type="password"
          autoComplete="new-password"
          error={errors.confirmPassword?.message}
          {...register('confirmPassword')}
        />
        <Button type="submit" size="lg" className="mt-2 w-full" disabled={isSubmitting}>
          {isSubmitting ? 'Creating account…' : 'Create account'}
        </Button>
      </form>
    </AuthCard>
  )
}

function PasswordChecklist({ password }: { password: string }) {
  return (
    <ul className="grid gap-0.5" aria-label="Password requirements">
      {passwordRules.map((rule) => {
        const met = rule.test(password)
        const Icon = met ? Check : X
        return (
          <li key={rule.label} className={cn('flex items-center gap-1.5', met && 'text-foreground')}>
            <Icon className={cn('size-3.5', met && 'text-violet')} aria-hidden />
            <span>
              {rule.label}
              <span className="sr-only">{met ? ' (met)' : ' (not met)'}</span>
            </span>
          </li>
        )
      })}
    </ul>
  )
}
