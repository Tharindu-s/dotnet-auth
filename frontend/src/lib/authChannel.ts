// Tells other tabs of this app when the user logs in or out
export type AuthMessage = { type: 'login' } | { type: 'logout' }

const channel = typeof BroadcastChannel === 'undefined' ? null : new BroadcastChannel('auth')

export function broadcastAuth(message: AuthMessage) {
  channel?.postMessage(message)
}

export function onAuthMessage(handler: (message: AuthMessage) => void) {
  const listener = (event: MessageEvent<AuthMessage>) => handler(event.data)
  channel?.addEventListener('message', listener)
  return () => channel?.removeEventListener('message', listener)
}
