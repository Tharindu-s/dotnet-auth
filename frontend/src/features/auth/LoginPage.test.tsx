import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { beforeEach, describe, expect, it } from 'vitest'
import { useAuthStore } from '@/stores/auth'
import { renderApp } from '@/test/render'
import { server, user } from '@/test/server'

describe('LoginPage', () => {
  beforeEach(() => {
    useAuthStore.getState().clear()
  })

  it('shows field errors without calling the API', async () => {
    renderApp('/login')

    await userEvent.click(screen.getByRole('button', { name: 'Log in' }))

    expect(await screen.findByText('Enter a valid email address.')).toBeInTheDocument()
    expect(screen.getByText('Enter your password.')).toBeInTheDocument()
  })

  it('signs in and goes to the profile page', async () => {
    server.use(
      http.post('/api/auth/login', () => HttpResponse.json({ ...user, accessToken: 'token-1' })),
      http.get('/api/auth/me', () => HttpResponse.json({ ...user, appId: 'app-1', roles: ['User'] })),
    )
    const { router } = renderApp('/login')

    await userEvent.type(screen.getByLabelText('Email'), 'jane@example.com')
    await userEvent.type(screen.getByLabelText('Password'), 'Passw0rd!')
    await userEvent.click(screen.getByRole('button', { name: 'Log in' }))

    await waitFor(() => expect(router.state.location.pathname).toBe('/'))
    expect(useAuthStore.getState()).toMatchObject({ status: 'authenticated', accessToken: 'token-1' })
    expect(await screen.findByText('Jane Doe')).toBeInTheDocument()
  })

  it('shows the API error message', async () => {
    server.use(
      http.post('/api/auth/login', () =>
        HttpResponse.json({ message: 'Invalid username or password.' }, { status: 401 }),
      ),
    )
    renderApp('/login')

    await userEvent.type(screen.getByLabelText('Email'), 'jane@example.com')
    await userEvent.type(screen.getByLabelText('Password'), 'wrong')
    await userEvent.click(screen.getByRole('button', { name: 'Log in' }))

    expect(await screen.findByText('Invalid username or password.')).toBeInTheDocument()
    expect(useAuthStore.getState().status).toBe('anonymous')
  })
})
