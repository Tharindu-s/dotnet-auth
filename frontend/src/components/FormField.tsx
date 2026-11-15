import type { ComponentProps, ReactNode } from 'react'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'

interface FormFieldProps extends ComponentProps<typeof Input> {
  id: string
  label: string
  error?: string
  hint?: ReactNode
}

/** Label, input and error message, wired up for screen readers */
export function FormField({ id, label, error, hint, ...inputProps }: FormFieldProps) {
  const describedBy = error ? `${id}-error` : hint ? `${id}-hint` : undefined
  return (
    <div className="grid gap-1.5">
      <Label htmlFor={id}>{label}</Label>
      <Input id={id} aria-invalid={error ? true : undefined} aria-describedby={describedBy} {...inputProps} />
      {error ? (
        <p id={`${id}-error`} className="text-sm text-destructive">
          {error}
        </p>
      ) : (
        hint && (
          <div id={`${id}-hint`} className="text-sm text-muted-foreground">
            {hint}
          </div>
        )
      )}
    </div>
  )
}
