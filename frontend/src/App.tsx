import { ClerkLoaded, ClerkLoading, SignedIn, SignedOut } from '@clerk/clerk-react'
import { isClerkEnabled } from './auth/clerk'
import { AuthAccessBanner } from './components/AuthAccessBanner'
import { ChatLayout } from './components/ChatLayout'

export default function App() {
  const clerkEnabled = isClerkEnabled()

  if (!clerkEnabled) {
    return (
      <div className="flex h-screen flex-col bg-gray-950 text-gray-100">
        <AuthAccessBanner clerkEnabled={false} />
        <ChatLayout multiConversationEnabled={false} />
      </div>
    )
  }

  return (
    <div className="flex h-screen flex-col bg-gray-950 text-gray-100">
      <AuthAccessBanner clerkEnabled />

      <ClerkLoading>
        <ChatLayout multiConversationEnabled={false} />
      </ClerkLoading>

      <ClerkLoaded>
        <SignedIn>
          <ChatLayout multiConversationEnabled />
        </SignedIn>

        <SignedOut>
          <ChatLayout multiConversationEnabled={false} />
        </SignedOut>
      </ClerkLoaded>
    </div>
  )
}
