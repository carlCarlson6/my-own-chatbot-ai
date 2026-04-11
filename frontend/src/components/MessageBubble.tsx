import type { ChatMessage } from '../types/chat'

interface MessageBubbleProps {
  message: ChatMessage
}

export function MessageBubble({ message }: MessageBubbleProps) {
  const isUser = message.role === 'user'
  const time = new Date(message.createdAtUtc).toLocaleTimeString([], {
    hour: '2-digit',
    minute: '2-digit',
  })

  return (
    <div className={`flex ${isUser ? 'justify-end' : 'justify-start'} mb-3`}>
      <div
        className={`max-w-[75%] rounded-2xl px-4 py-3 ${
          isUser ? 'bg-blue-600 text-white' : 'bg-gray-800 text-gray-100'
        }`}
      >
        <p className="text-sm leading-relaxed whitespace-pre-wrap break-words">
          {message.content || (message.isStreaming ? 'Thinking…' : '')}
          {message.isStreaming ? (
            <span
              className="ml-1 inline-block h-4 w-2 animate-pulse rounded-sm bg-current align-middle opacity-70"
              aria-hidden="true"
            />
          ) : null}
        </p>
        <div
          className={`mt-1.5 flex items-center gap-2 text-xs ${
            isUser ? 'text-blue-200' : 'text-gray-400'
          }`}
        >
          <span>{time}</span>
          {message.isStreaming ? (
            <span className="rounded-full bg-gray-700 px-2 py-0.5 text-[11px] uppercase tracking-wide text-gray-300">
              Streaming
            </span>
          ) : null}
        </div>
      </div>
    </div>
  )
}
