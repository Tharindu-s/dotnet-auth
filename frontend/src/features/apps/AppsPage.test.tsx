import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { beforeEach, describe, expect, it } from 'vitest'
import type { AdminUser, CreatedApp } from '@/lib/types'
import { useAuthStore } from '@/stores/auth'
import { renderApp } from '@/test/render'
import { app, server, user } from '@/test/server'

describe('apps', () => {
  beforeEach(() => {
    useAuthStore.getState().setSession(user, 'token')
  })

  it('lists the apps in the panel and the sidebar, and opens one', async () => {
    server.use(
      http.get('/api/apps', () =>
        HttpResponse.json([app(), app({ id: 'app-2', name: 'Budget', isPublic: true, registrationEnabled: false, userCount: 1 })]),
      ),
      http.get('/api/apps/app-2', () => HttpResponse.json(app({ id: 'app-2', name: 'Budget', isPublic: true }))),
    )
    const { router } = renderApp('/')

    // The panel's item reads "Budget Browser app · notes-api ..."; the sidebar's link is just "Budget"
    const item = await screen.findByRole('link', { name: /^Budget Browser app/ })
    expect(within(item).getByText('Sign-ups off')).toBeInTheDocument()
    expect(within(item).getByText(/1 user ·/)).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Budget' })).toBeInTheDocument()
    expect(screen.getByText('Active sessions')).toBeInTheDocument()

    await userEvent.click(item)
    await waitFor(() => expect(router.state.location.pathname).toBe('/apps/app-2'))
    expect(await screen.findByRole('tab', { name: 'Overview' })).toHaveAttribute('data-state', 'active')
  })

  it('filters the panel with the search box', async () => {
    server.use(http.get('/api/apps', () => HttpResponse.json([app(), app({ id: 'app-2', name: 'Budget' })])))
    renderApp('/')

    await userEvent.type(await screen.findByRole('textbox', { name: 'Search apps' }), 'budg')

    expect(screen.getByRole('link', { name: /^Budget Server app/ })).toBeInTheDocument()
    expect(screen.queryByRole('link', { name: /^Notes Server app/ })).not.toBeInTheDocument()
  })

  it('creates a server app and shows its API key once', async () => {
    let body: unknown
    const created: CreatedApp = { ...app({ id: 'new-app', name: 'Notes', userCount: 0, activeSessionCount: 0 }), rawApiKey: 'secret-key' }
    server.use(
      http.post('/api/apps', async ({ request }) => {
        body = await request.json()
        return HttpResponse.json(created, { status: 201 })
      }),
    )
    renderApp('/apps/new')

    await userEvent.type(await screen.findByLabelText('Name'), 'Notes')
    await userEvent.type(screen.getByLabelText('Audience (optional)'), 'notes-api')
    await userEvent.click(screen.getByRole('button', { name: 'Create app' }))

    expect(await screen.findByText('Notes is ready.')).toBeInTheDocument()
    expect(screen.getByDisplayValue('secret-key')).toBeInTheDocument()
    expect(body).toEqual({ name: 'Notes', audience: 'notes-api', allowedOrigins: [], registrationEnabled: true, isPublic: false })
  })

  it('needs an allowed origin for a browser app', async () => {
    renderApp('/apps/new')

    await userEvent.type(await screen.findByLabelText('Name'), 'SPA')
    await userEvent.click(screen.getByRole('radio', { name: /Browser app/ }))
    await userEvent.click(screen.getByRole('button', { name: 'Create app' }))

    expect(await screen.findByText('A browser app needs at least one allowed origin.')).toBeInTheDocument()
  })

  it('preselects the type from the quick action link', async () => {
    renderApp('/apps/new?type=browser')

    expect(await screen.findByRole('radio', { name: /Browser app/ })).toBeChecked()
    expect(screen.getByLabelText('Allowed origins')).toBeInTheDocument()
  })

  it('locks a user after confirming', async () => {
    const jane: AdminUser = {
      id: 'user-9',
      email: 'jane@example.com',
      fullName: 'Jane Doe',
      city: 'Colombo',
      isLockedOut: false,
      lockoutEnd: null,
      activeSessionCount: 1,
      lastSignInAt: '2026-11-10T08:00:00Z',
    }
    let locked: string | undefined
    server.use(
      http.get('/api/apps/app-1', () => HttpResponse.json(app())),
      http.get('/api/apps/app-1/users', () =>
        HttpResponse.json({ items: [{ ...jane, isLockedOut: locked === jane.id }], total: 1, page: 1, pageSize: 25 }),
      ),
      http.post('/api/apps/app-1/users/:userId/lock', ({ params }) => {
        locked = params.userId as string
        return new HttpResponse(null, { status: 204 })
      }),
    )
    renderApp('/apps/app-1?tab=users')

    await userEvent.click(await screen.findByRole('button', { name: 'Lock jane@example.com' }))
    const dialog = await screen.findByRole('alertdialog')
    expect(locked).toBeUndefined()
    await userEvent.click(within(dialog).getByRole('button', { name: 'Lock user' }))

    expect(await screen.findByText('Locked')).toBeInTheDocument()
    expect(locked).toBe('user-9')
  })

  it('only deletes an app once its name is typed', async () => {
    let deleted = false
    server.use(
      http.get('/api/apps/app-1', () => HttpResponse.json(app())),
      http.delete('/api/apps/app-1', () => {
        deleted = true
        return new HttpResponse(null, { status: 204 })
      }),
    )
    const { router } = renderApp('/apps/app-1?tab=settings')

    await userEvent.click(await screen.findByRole('button', { name: 'Delete app' }))
    const dialog = await screen.findByRole('alertdialog')
    const confirm = within(dialog).getByRole('button', { name: 'Delete this app' })
    expect(confirm).toBeDisabled()
    await userEvent.type(within(dialog).getByLabelText(/to confirm/), 'Notes')
    await userEvent.click(confirm)

    await waitFor(() => expect(router.state.location.pathname).toBe('/'))
    expect(deleted).toBe(true)
  })
})
