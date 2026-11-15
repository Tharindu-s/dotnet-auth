import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import { server, user } from '@/test/server'
import { useAuthStore } from '@/stores/auth'
import { api, refreshAccessToken } from './api'

function signedIn(token = 'expired-token') {
  useAuthStore.getState().setSession(user, token)
}

describe('api client', () => {
  it('sends the bearer token', async () => {
    signedIn('token-1')
    server.use(http.get('/api/thing', ({ request }) => HttpResponse.json({ auth: request.headers.get('Authorization') })))

    const { data } = await api.get('/api/thing')

    expect(data.auth).toBe('Bearer token-1')
  })

  it('refreshes once on a 401 and retries with the new token', async () => {
    signedIn()
    let refreshes = 0
    server.use(
      http.post('/api/auth/refresh', () => {
        refreshes++
        return HttpResponse.json({ accessToken: 'fresh-token' })
      }),
      http.get('/api/thing', ({ request }) =>
        request.headers.get('Authorization') === 'Bearer fresh-token'
          ? HttpResponse.json({ ok: true })
          : new HttpResponse(null, { status: 401 }),
      ),
    )

    const { data } = await api.get('/api/thing')

    expect(data).toEqual({ ok: true })
    expect(refreshes).toBe(1)
    expect(useAuthStore.getState().accessToken).toBe('fresh-token')
  })

  it('shares one refresh between concurrent 401s, so the rotating cookie is sent once', async () => {
    signedIn()
    let refreshes = 0
    server.use(
      http.post('/api/auth/refresh', async () => {
        refreshes++
        await new Promise((resolve) => setTimeout(resolve, 20))
        return HttpResponse.json({ accessToken: 'fresh-token' })
      }),
      http.get('/api/thing/:id', ({ request }) =>
        request.headers.get('Authorization') === 'Bearer fresh-token'
          ? HttpResponse.json({ ok: true })
          : new HttpResponse(null, { status: 401 }),
      ),
    )

    const results = await Promise.all([1, 2, 3].map((id) => api.get(`/api/thing/${id}`)))

    expect(results.every((r) => r.data.ok)).toBe(true)
    expect(refreshes).toBe(1)
  })

  it('signs out when the refresh token is rejected', async () => {
    signedIn()
    server.use(
      http.post('/api/auth/refresh', () => HttpResponse.json({ message: 'revoked' }, { status: 401 })),
      http.get('/api/thing', () => new HttpResponse(null, { status: 401 })),
    )

    await expect(api.get('/api/thing')).rejects.toMatchObject({ response: { status: 401 } })
    expect(useAuthStore.getState().status).toBe('anonymous')
  })

  it('stays signed in when the refresh fails for another reason', async () => {
    signedIn()
    server.use(
      http.post('/api/auth/refresh', () => HttpResponse.json({ message: 'slow down' }, { status: 429 })),
      http.get('/api/thing', () => new HttpResponse(null, { status: 401 })),
    )

    await expect(api.get('/api/thing')).rejects.toThrow()
    expect(useAuthStore.getState().status).toBe('authenticated')
  })

  it('does not try to refresh after a failed login', async () => {
    let refreshes = 0
    server.use(
      http.post('/api/auth/refresh', () => {
        refreshes++
        return HttpResponse.json({ accessToken: 'x' })
      }),
      http.post('/api/auth/login', () => HttpResponse.json({ message: 'Invalid username or password.' }, { status: 401 })),
    )

    await expect(api.post('/api/auth/login', {}, { skipAuthRefresh: true })).rejects.toThrow()
    expect(refreshes).toBe(0)
  })

  it('returns null from refreshAccessToken when there is no session', async () => {
    server.use(http.post('/api/auth/refresh', () => new HttpResponse(null, { status: 401 })))

    expect(await refreshAccessToken()).toBeNull()
  })
})
