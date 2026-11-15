/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** ID of the public application client this frontend signs users into */
  readonly VITE_APP_ID?: string
  /** Where the API lives. Empty (the default) means same origin, through the Vite proxy in dev */
  readonly VITE_API_BASE_URL?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
