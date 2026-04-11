import type { PropsWithChildren } from 'react'
import { ClerkProvider } from '@clerk/clerk-react'
import { getClerkPublishableKey } from './auth/clerk'

const clerkPublishableKey = getClerkPublishableKey()

export function AppProviders({ children }: PropsWithChildren) {
  if (!clerkPublishableKey) {
    return children
  }

  return <ClerkProvider publishableKey={clerkPublishableKey}>{children}</ClerkProvider>
}
