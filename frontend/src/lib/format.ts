const dateTimeFormat = new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' })
const dateFormat = new Intl.DateTimeFormat(undefined, { dateStyle: 'medium' })
const relativeFormat = new Intl.RelativeTimeFormat(undefined, { numeric: 'auto', style: 'narrow' })

export const formatDateTime = (value: string) => dateTimeFormat.format(new Date(value))
export const formatDate = (value: string) => dateFormat.format(new Date(value))

const units: [Intl.RelativeTimeFormatUnit, number][] = [
  ['year', 365 * 24 * 3600],
  ['month', 30 * 24 * 3600],
  ['week', 7 * 24 * 3600],
  ['day', 24 * 3600],
  ['hour', 3600],
  ['minute', 60],
]

/** "2h ago", "yesterday", "just now" */
export function formatRelative(value: string, now = Date.now()) {
  const seconds = (new Date(value).getTime() - now) / 1000
  for (const [unit, size] of units) {
    if (Math.abs(seconds) >= size) return relativeFormat.format(Math.round(seconds / size), unit)
  }
  return 'just now'
}

/** "1 user", "3 users" */
export const plural = (count: number, noun: string) => `${count} ${noun}${count === 1 ? '' : 's'}`

/** "Jane Doe" -> "JD" */
export const initials = (name: string) =>
  name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase())
    .join('') || '?'
