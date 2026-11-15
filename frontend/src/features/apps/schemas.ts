import { z } from 'zod'
import type { AppInput } from '@/lib/types'

// Origins are entered one per line (or separated by spaces); the API normalizes and validates each one
export const splitOrigins = (value: string) => value.split(/\s+/).filter(Boolean)

const originsField = z
  .string()
  .refine((value) => splitOrigins(value).every((origin) => /^https?:\/\/[^/\s]+\/?$/i.test(origin)), {
    message: 'Use scheme://host[:port], one per line, such as http://localhost:5173.',
  })

export const appSettingsSchema = z.object({
  name: z.string().trim().min(1, 'Enter a name.').max(100),
  audience: z.string().trim().max(200),
  allowedOrigins: originsField,
  registrationEnabled: z.boolean(),
})

export const newAppSchema = appSettingsSchema
  .extend({ isPublic: z.boolean() })
  .refine((app) => !app.isPublic || splitOrigins(app.allowedOrigins).length > 0, {
    message: 'A browser app needs at least one allowed origin.',
    path: ['allowedOrigins'],
  })

export type AppSettingsValues = z.infer<typeof appSettingsSchema>
export type NewAppValues = z.infer<typeof newAppSchema>

export function toAppInput(values: AppSettingsValues): AppInput {
  return {
    name: values.name,
    audience: values.audience || null,
    allowedOrigins: splitOrigins(values.allowedOrigins),
    registrationEnabled: values.registrationEnabled,
  }
}
