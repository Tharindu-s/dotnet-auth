import { screen, waitFor } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { useAuthStore } from '@/stores/auth'
import { renderApp } from '@/test/render'
import { user } from '@/test/server'

describe('route guards', () => {
  it('shows a spinner while the session is being restored', () => {
    renderApp('/sessions')

    expect(screen.getByRole('status', { name: 'Loading' })).toBeInTheDocument()
  })

  it('sends anonymous users to /login', async () => {
    useAuthStore.getState().clear()
    const { router } = renderApp('/sessions')

    await waitFor(() => expect(router.state.location.pathname).toBe('/login'))
  })

  it('sends signed-in users away from /login', async () => {
    useAuthStore.getState().setSession(user, 'token')
    const { router } = renderApp('/login')

    await waitFor(() => expect(router.state.location.pathname).toBe('/'))
  })
})
