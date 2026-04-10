import { useEffect } from 'react'
import { useChatStore } from './store/chatStore'
import { ChatLayout } from './components/ChatLayout'

export default function App() {
  const loadModels = useChatStore((s) => s.loadModels)

  useEffect(() => {
    void loadModels()
  }, [loadModels])

  return <ChatLayout />
}
