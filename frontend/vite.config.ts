/// <reference types="vitest/config" />
import path from 'node:path'
import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig, loadEnv } from 'vite'

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  // Run the API with `dotnet run --launch-profile http` so it listens on this port
  const apiTarget = env.API_PROXY_TARGET || 'http://localhost:5183'

  return {
    plugins: [react(), tailwindcss()],
    resolve: {
      alias: { '@': path.resolve(import.meta.dirname, './src') },
    },
    server: {
      port: 5173,
      strictPort: true,
      // The browser only ever talks to localhost:5173, so the API is same-origin:
      // no CORS, and the SameSite=Strict refresh cookie is sent.
      proxy: {
        '/api': { target: apiTarget, changeOrigin: true },
        '/.well-known': { target: apiTarget, changeOrigin: true },
      },
    },
    test: {
      environment: 'jsdom',
      setupFiles: ['./src/test/setup.ts'],
      css: false,
    },
  }
})
