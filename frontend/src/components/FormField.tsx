import type { ComponentProps, ReactNode } from 'react'
import { Field, FieldDescription, FieldError, FieldLabel } from '@/components/ui/field'
import { Input } from '@/components/ui/input'

interface FormFieldProps extends ComponentProps<typeof Input> {
  id: string
  label: string
  error?: string
  description?: ReactNode
}

/** shadcn Field + Input, with the label, description and error wired up for screen readers */
export function FormField({ id, label, error, description, ...inputProps }: FormFieldProps) {
  const describedBy = error ? `${id}-error` : description ? `${id}-description` : undefined
  return (
    <Field data-invalid={error ? true : undefined}>
      <FieldLabel htmlFor={id}>{label}</FieldLabel>
      <Input id={id} aria-invalid={error ? true : undefined} aria-describedby={describedBy} className="h-10 bg-white" {...inputProps} />
      {error ? (
        <FieldError id={`${id}-error`}>{error}</FieldError>
      ) : (
        description && <FieldDescription id={`${id}-description`}>{description}</FieldDescription>
      )}
    </Field>
  )
}
