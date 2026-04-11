export type AuthTokenGetter = () => Promise<string | null>

type GlobalWithClerkConfig = typeof globalThis & {
  __APP_CONFIG__?: {
    CLERK_PUBLISHABLE_KEY?: string
  }
  CLERK_PUBLISHABLE_KEY?: string
}

let authTokenGetter: AuthTokenGetter | null = null

export function getClerkPublishableKey(): string | null {
  const runtime = globalThis as GlobalWithClerkConfig

  return (
    import.meta.env.VITE_CLERK_PUBLISHABLE_KEY ??
    runtime.__APP_CONFIG__?.CLERK_PUBLISHABLE_KEY ??
    runtime.CLERK_PUBLISHABLE_KEY ??
    null
  )
}

export function isClerkEnabled(): boolean {
  return Boolean(getClerkPublishableKey())
}

export function setAuthTokenGetter(getter: AuthTokenGetter | null) {
  authTokenGetter = getter
}

export async function getAuthToken(): Promise<string | null> {
  if (!authTokenGetter) {
    return null
  }

  try {
    return await authTokenGetter()
  } catch {
    return null
  }
}
