import { setupServer } from 'msw/node'

// Each test registers the handlers it needs with server.use(...)
export const server = setupServer()

export const user = {
  id: 'user-1',
  fullName: 'Jane Doe',
  userName: 'jane@example.com',
  email: 'jane@example.com',
  city: 'Colombo',
}
