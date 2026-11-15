const browsers: [RegExp, string][] = [
  [/Edg\//, 'Edge'],
  [/OPR\//, 'Opera'],
  [/Firefox\//, 'Firefox'],
  [/Chrome\//, 'Chrome'],
  [/Safari\//, 'Safari'],
  [/curl\//, 'curl'],
  [/PostmanRuntime\//, 'Postman'],
]

const systems: [RegExp, string][] = [
  [/Windows/, 'Windows'],
  [/Android/, 'Android'],
  [/iPhone|iPad/, 'iOS'],
  [/Mac OS X/, 'macOS'],
  [/Linux/, 'Linux'],
]

/** "Mozilla/5.0 (Windows NT 10.0; ...) Chrome/140..." -> "Chrome on Windows" */
export function describeDevice(userAgent: string | null): string {
  if (!userAgent || userAgent === 'Unknown') return 'Unknown device'
  const browser = browsers.find(([pattern]) => pattern.test(userAgent))?.[1]
  const system = systems.find(([pattern]) => pattern.test(userAgent))?.[1]
  if (browser && system) return `${browser} on ${system}`
  return browser ?? system ?? userAgent.slice(0, 40)
}
