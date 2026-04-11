import { useChatStore } from '../store/chatStore'
import { ConversationHeader } from './ConversationHeader'
import { MessageList } from './MessageList'
import { MessageComposer } from './MessageComposer'

export function ChatLayout() {
  const messages = useChatStore((s) => s.messages)
  const status = useChatStore((s) => s.status)
  const errorMessage = useChatStore((s) => s.errorMessage)
  const sendMessage = useChatStore((s) => s.sendMessage)
  const clearError = useChatStore((s) => s.clearError)

  const isSending = status === 'sending'

  return (
    <div className="flex min-h-0 flex-1 flex-col bg-gray-950 text-gray-100">
      <ConversationHeader />

      <MessageList messages={messages} isSending={isSending} />

      {/* Inline error banner */}
      {status === 'error' && errorMessage && (
        <div
          role="alert"
          className="mx-4 mb-2 flex items-start gap-3 rounded-xl bg-red-950/60 border border-red-800 px-4 py-3 text-sm text-red-200 shrink-0"
        >
          <span className="flex-1">{errorMessage}</span>
          <button
            onClick={clearError}
            className="text-red-300 hover:text-white transition-colors leading-none mt-0.5"
            aria-label="Dismiss error"
          >
            ✕
          </button>
        </div>
      )}

      <MessageComposer onSend={sendMessage} isSending={isSending} />
    </div>
  )
}
