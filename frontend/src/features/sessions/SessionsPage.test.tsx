import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { beforeEach, describe, expect, it } from 'vitest'
import type { Session } from '@/lib/types'
import { useAuthStore } from '@/stores/auth'
import { renderApp } from '@/test/render'
import { server, user } from '@/test/server'

const chromeOnWindows = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/140.0 Safari/537.36'
const safariOnIphone = 'Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) AppleWebKit/605.1.15 Version/18.0 Safari/604.1'

function session(tokenId: number, device: string, isCurrent: boolean): Session {
  return {
    tokenId,
    device,
    ipAddress: '127.0.0.1',
    issuedAt: '2026-10-25T08:00:00Z',
    expiresAt: '2026-11-01T08:00:00Z',
    isCurrent,
  }
}

describe('SessionsPage', () => {
  beforeEach(() => {
    useAuthStore.getState().setSession(user, 'token')
  })

  it('lists sessions and marks this device', async () => {
    server.use(
      http.get('/api/auth/sessions', () =>
        HttpResponse.json([session(1, chromeOnWindows, true), session(2, safariOnIphone, false)]),
      ),
    )
    renderApp('/sessions')

    const current = (await screen.findByText('Chrome on Windows')).closest('tr')!
    expect(within(current).getByText('This device')).toBeInTheDocument()
    expect(within(current).queryByRole('button', { name: 'Revoke' })).not.toBeInTheDocument()
    expect(screen.getByText('Safari on iOS')).toBeInTheDocument()
  })

  it('revokes another session and refetches the list', async () => {
    let sessions = [session(1, chromeOnWindows, true), session(2, safariOnIphone, false)]
    let revokedId: string | undefined
    server.use(
      http.get('/api/auth/sessions', () => HttpResponse.json(sessions)),
      http.delete('/api/auth/sessions/:id', ({ params }) => {
        revokedId = params.id as string
        sessions = sessions.filter((s) => String(s.tokenId) !== revokedId)
        return new HttpResponse(null, { status: 204 })
      }),
    )
    renderApp('/sessions')

    await userEvent.click(await screen.findByRole('button', { name: 'Revoke' }))

    await screen.findByText('Chrome on Windows')
    expect(revokedId).toBe('2')
    await expect(screen.findByText('Safari on iOS', undefined, { timeout: 100 })).rejects.toThrow()
  })
})
