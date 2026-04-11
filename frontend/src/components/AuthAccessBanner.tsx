import {
  ClerkLoaded,
  ClerkLoading,
  SignInButton,
  SignOutButton,
  SignedIn,
  SignedOut,
  useAuth,
  useUser,
} from '@clerk/clerk-react'
import { useEffect } from 'react'
import { setAuthTokenGetter } from '../auth/clerk'

interface AuthAccessBannerProps {
  clerkEnabled: boolean
}

function ClerkTokenBridge() {
  const { getToken, isLoaded, isSignedIn } = useAuth()

  useEffect(() => {
    if (!isLoaded || !isSignedIn) {
      setAuthTokenGetter(null)
      return
    }

    setAuthTokenGetter(async () => (await getToken()) ?? null)

    return () => {
      setAuthTokenGetter(null)
    }
  }, [getToken, isLoaded, isSignedIn])

  return null
}

function SignedInSummary() {
  const { user } = useUser()

  const identity =
    user?.primaryEmailAddress?.emailAddress ?? user?.fullName ?? 'your Clerk account'

  return (
    <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
      <div>
        <p className="text-sm font-semibold text-emerald-200">Signed in</p>
        <p className="text-sm text-gray-200">
          Connected as <span className="font-medium text-white">{identity}</span>. Saved
          multi-conversation history and management will use this account as later phases
          ship.
        </p>
      </div>

      <SignOutButton>
        <button
          type="button"
          className="inline-flex items-center justify-center rounded-lg border border-gray-700 px-3 py-2 text-sm font-medium text-gray-100 transition hover:border-gray-500 hover:bg-gray-800"
        >
          Sign out
        </button>
      </SignOutButton>
    </div>
  )
}

export function AuthAccessBanner({ clerkEnabled }: AuthAccessBannerProps) {
  if (!clerkEnabled) {
    return (
      <section className="border-b border-indigo-900/50 bg-indigo-950/40 px-4 py-3">
        <p className="text-sm font-semibold text-indigo-100">Anonymous chat is available</p>
        <p className="text-sm text-indigo-200/90">
          Sign-in is optional. Configure Clerk to unlock saved multi-conversation history
          and management without blocking the current single-chat flow.
        </p>
      </section>
    )
  }

  return (
    <section className="border-b border-gray-800 bg-gray-900/80 px-4 py-3">
      <ClerkTokenBridge />

      <ClerkLoading>
        <p className="text-sm text-gray-300">Loading sign-in status…</p>
      </ClerkLoading>

      <ClerkLoaded>
        <SignedOut>
          <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
            <div>
              <p className="text-sm font-semibold text-indigo-200">
                Continue anonymously or sign in
              </p>
              <p className="text-sm text-gray-300">
                Anonymous single-chat stays open. Sign in to unlock saved
                multi-conversation history and management as those features arrive.
              </p>
            </div>

            <SignInButton mode="modal">
              <button
                type="button"
                className="inline-flex items-center justify-center rounded-lg bg-indigo-500 px-4 py-2 text-sm font-semibold text-white transition hover:bg-indigo-400"
              >
                Sign in for saved conversations
              </button>
            </SignInButton>
          </div>
        </SignedOut>

        <SignedIn>
          <SignedInSummary />
        </SignedIn>
      </ClerkLoaded>
    </section>
  )
}
