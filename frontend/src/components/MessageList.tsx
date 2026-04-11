import { useEffect, useRef } from 'react'
import type { ChatMessage } from '../types/chat'
import { MessageBubble } from './MessageBubble'

interface MessageListProps {
  messages: ChatMessage[]
  isSending: boolean
  isLoadingHistory?: boolean
  emptyStateTitle?: string
  emptyStateDescription?: string
}

export function MessageList({
  messages,
  isSending,
  isLoadingHistory = false,
  emptyStateTitle = 'Start a conversation',
  emptyStateDescription = 'Type a message below to begin chatting with your local AI.',
}: MessageListProps) {
  const bottomRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [isLoadingHistory, isSending, messages])

  return (
    <div className="flex-1 overflow-y-auto px-4 py-4" aria-busy={isSending}>
      {isLoadingHistory ? (
        <div className="flex h-full items-center justify-center">
          <div className="rounded-2xl border border-gray-800 bg-gray-900/70 px-5 py-4 text-center text-sm text-gray-300">
            Loading conversation history…
          </div>
        </div>
      ) : messages.length === 0 && !isSending ? (
        <div className="flex items-center justify-center h-full">
          <div className="text-center text-gray-500 select-none">
            <p className="text-4xl mb-3">💬</p>
            <p className="text-lg font-semibold text-gray-400">{emptyStateTitle}</p>
            <p className="text-sm mt-1 text-gray-500">{emptyStateDescription}</p>
          </div>
        </div>
      ) : (
        <>
          {messages.map((message) => (
            <MessageBubble key={message.messageId} message={message} />
          ))}

          {/* Typing indicator while assistant is responding */}
          {isSending && (
            <div className="flex justify-start mb-3">
              <div
                className="bg-gray-800 text-gray-100 rounded-2xl px-4 py-3"
                role="status"
                aria-live="polite"
                aria-atomic="true"
              >
                <div className="flex gap-2 items-center min-h-5">
                  <span className="w-2 h-2 bg-gray-400 rounded-full animate-bounce [animation-delay:-0.3s]" />
                  <span className="w-2 h-2 bg-gray-400 rounded-full animate-bounce [animation-delay:-0.15s]" />
                  <span className="w-2 h-2 bg-gray-400 rounded-full animate-bounce" />
                  <span className="text-sm text-gray-300">Assistant is responding…</span>
                </div>
              </div>
            </div>
          )}
        </>
      )}

      {/* Scroll anchor */}
      <div ref={bottomRef} />
    </div>
  )
}
