import { z } from 'zod'

// Mirrors the API's Identity options: 6+ characters, a digit, a lowercase and an uppercase
// letter, and a character that is neither a letter nor a digit
export const passwordRules = [
  { test: (v: string) => v.length >= 6, label: 'At least 6 characters' },
  { test: (v: string) => /\p{Nd}/u.test(v), label: 'A digit' },
  { test: (v: string) => /\p{Ll}/u.test(v), label: 'A lowercase letter' },
  { test: (v: string) => /\p{Lu}/u.test(v), label: 'An uppercase letter' },
  { test: (v: string) => /[^\p{L}\p{N}]/u.test(v), label: 'A symbol' },
] as const

export const passwordSchema = z.string().superRefine((value, ctx) => {
  const missing = passwordRules.filter((rule) => !rule.test(value)).map((rule) => rule.label.toLowerCase())
  if (missing.length > 0) ctx.addIssue({ code: 'custom', message: `Password needs ${missing.join(', ')}.` })
})

export const loginSchema = z.object({
  email: z.email('Enter a valid email address.'),
  password: z.string().min(1, 'Enter your password.'),
})

export const changePasswordSchema = z
  .object({
    currentPassword: z.string().min(1, 'Enter your current password.'),
    newPassword: passwordSchema,
    confirmPassword: z.string(),
  })
  .refine((data) => data.newPassword === data.confirmPassword, {
    message: "Passwords don't match.",
    path: ['confirmPassword'],
  })
  .refine((data) => data.newPassword !== data.currentPassword, {
    message: 'Choose a password different from the current one.',
    path: ['newPassword'],
  })

export type LoginInput = z.infer<typeof loginSchema>
export type ChangePasswordInput = z.infer<typeof changePasswordSchema>
