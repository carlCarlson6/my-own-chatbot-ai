import { isClerkEnabled } from './auth/clerk'
import { AuthAccessBanner } from './components/AuthAccessBanner'
import { ChatLayout } from './components/ChatLayout'

export default function App() {
  return (
    <div className="flex h-screen flex-col bg-gray-950 text-gray-100">
      <AuthAccessBanner clerkEnabled={isClerkEnabled()} />
      <ChatLayout />
    </div>
  )
}
