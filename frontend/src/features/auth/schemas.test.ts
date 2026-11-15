import { describe, expect, it } from 'vitest'
import { passwordSchema, registerSchema } from './schemas'

describe('passwordSchema', () => {
  it.each(['Passw0rd!', 'Ab1!xy', 'Pässw0rd#'])('accepts %s', (password) => {
    expect(passwordSchema.safeParse(password).success).toBe(true)
  })

  it.each([
    ['Ab1!', 'at least 6 characters'],
    ['Password!', 'a digit'],
    ['PASSW0RD!', 'a lowercase letter'],
    ['passw0rd!', 'an uppercase letter'],
    ['Passw0rd', 'a symbol'],
  ])('rejects %s because it needs %s', (password, missing) => {
    const result = passwordSchema.safeParse(password)
    expect(result.success).toBe(false)
    expect(result.error?.issues[0].message).toContain(missing)
  })
})

describe('registerSchema', () => {
  const valid = {
    fullName: 'Jane Doe',
    email: 'jane@example.com',
    city: 'Colombo',
    password: 'Passw0rd!',
    confirmPassword: 'Passw0rd!',
  }

  it('accepts a valid form', () => {
    expect(registerSchema.safeParse(valid).success).toBe(true)
  })

  it('rejects mismatched passwords on the confirm field', () => {
    const result = registerSchema.safeParse({ ...valid, confirmPassword: 'Other0ne!' })
    expect(result.error?.issues[0].path).toEqual(['confirmPassword'])
  })
})
