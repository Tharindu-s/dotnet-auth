import axios from 'axios'

export const config = {
  apiBaseUrl: import.meta.env.VITE_API_BASE_URL ?? '',
}

/** GET /api/argus/config */
export interface ArgusConfig {
  /** ID of the Argus app: the public client this console signs admins into */
  appId: string
  /** The iss claim of every token the service issues */
  issuer: string
}

let argusConfig: Promise<ArgusConfig> | null = null

/**
 * Read once from the API, so the app ID never has to be baked into the build.
 * A failed request isn't cached: the next call tries again.
 */
export function getArgusConfig(): Promise<ArgusConfig> {
  argusConfig ??= axios
    .get<ArgusConfig>(`${config.apiBaseUrl}/api/argus/config`)
    .then((response) => response.data)
    .catch((error: unknown) => {
      argusConfig = null
      throw error
    })
  return argusConfig
}
