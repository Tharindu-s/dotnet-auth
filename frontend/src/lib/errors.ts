import axios from 'axios'

/** Turns an API error into a sentence for the user. Handles `{ message }` and ASP.NET validation problems. */
export function getErrorMessage(error: unknown, fallback = 'Something went wrong. Please try again.'): string {
  if (axios.isAxiosError(error)) {
    if (!error.response) return 'Cannot reach the server. Is the API running?'

    const data: unknown = error.response.data
    if (data && typeof data === 'object') {
      if ('message' in data && typeof data.message === 'string') return data.message
      if ('errors' in data && data.errors && typeof data.errors === 'object') {
        const first = Object.values(data.errors as Record<string, string[]>).flat()[0]
        if (first) return first
      }
      if ('title' in data && typeof data.title === 'string') return data.title
    }
    return fallback
  }
  if (error instanceof Error && error.message) return error.message
  return fallback
}
