import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { beforeEach, describe, expect, it } from 'vitest'
import { useAuthStore } from '@/stores/auth'
import { renderApp } from '@/test/render'
import { server, user } from '@/test/server'

async function fillForm(current: string, next: string, confirm = next) {
  await userEvent.type(screen.getByLabelText('Current password'), current)
  await userEvent.type(screen.getByLabelText('New password'), next)
  await userEvent.type(screen.getByLabelText('Confirm new password'), confirm)
  await userEvent.click(screen.getByRole('button', { name: 'Change password' }))
}

describe('ChangePasswordPage', () => {
  beforeEach(() => {
    useAuthStore.getState().setSession({ ...user, mustChangePassword: true }, 'token')
  })

  it('changes the password, clears the flag and opens the console', async () => {
    let body: unknown
    let changed = false
    server.use(
      http.post('/api/auth/change-password', async ({ request }) => {
        body = await request.json()
        changed = true
        return new HttpResponse(null, { status: 204 })
      }),
      http.get('/api/auth/me', () => HttpResponse.json({ ...user, mustChangePassword: !changed })),
    )
    const { router } = renderApp('/change-password')

    await fillForm('Initial-passw0rd', 'N3w-Passw0rd!')

    await waitFor(() => expect(router.state.location.pathname).toBe('/'))
    expect(body).toEqual({ currentPassword: 'Initial-passw0rd', newPassword: 'N3w-Passw0rd!' })
    expect(useAuthStore.getState().user?.mustChangePassword).toBe(false)
  })

  it('checks the password rules before calling the API', async () => {
    renderApp('/change-password')

    await fillForm('Initial-passw0rd', 'weak')

    expect(await screen.findByText(/Password needs/)).toBeInTheDocument()
  })

  it('shows the API error', async () => {
    server.use(
      http.post('/api/auth/change-password', () =>
        HttpResponse.json({ message: 'The current password is incorrect.' }, { status: 400 }),
      ),
    )
    renderApp('/change-password')

    await fillForm('Wrong-passw0rd', 'N3w-Passw0rd!')

    expect(await screen.findByText('The current password is incorrect.')).toBeInTheDocument()
  })
})
