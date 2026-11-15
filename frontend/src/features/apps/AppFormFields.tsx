import type { ComponentProps, ReactNode } from 'react'
import { Field, FieldContent, FieldDescription, FieldError, FieldLabel } from '@/components/ui/field'
import { Switch } from '@/components/ui/switch'
import { Textarea } from '@/components/ui/textarea'

/** Allowed origins, one per line (shadcn Field + Textarea). Spread register('allowedOrigins') into it. */
export function OriginsField({ error, ...props }: ComponentProps<typeof Textarea> & { error?: string }) {
  return (
    <Field data-invalid={error ? true : undefined}>
      <FieldLabel htmlFor="allowedOrigins">Allowed origins</FieldLabel>
      <Textarea
        id="allowedOrigins"
        rows={3}
        placeholder="http://localhost:5173"
        className="bg-white"
        aria-invalid={error ? true : undefined}
        aria-describedby="allowedOrigins-help"
        {...props}
      />
      {error ? (
        <FieldError id="allowedOrigins-help">{error}</FieldError>
      ) : (
        <FieldDescription id="allowedOrigins-help">
          One per line: the scheme, host and port the browser app is served from, such as http://localhost:5173.
        </FieldDescription>
      )}
    </Field>
  )
}

/** "Allow sign-ups" as a horizontal Field with a Switch */
export function SignUpsField({ checked, onChange, description }: { checked: boolean; onChange: (value: boolean) => void; description: ReactNode }) {
  return (
    <Field orientation="horizontal">
      <FieldContent>
        <FieldLabel htmlFor="registrationEnabled">Allow sign-ups</FieldLabel>
        <FieldDescription>{description}</FieldDescription>
      </FieldContent>
      <Switch id="registrationEnabled" checked={checked} onCheckedChange={onChange} />
    </Field>
  )
}
