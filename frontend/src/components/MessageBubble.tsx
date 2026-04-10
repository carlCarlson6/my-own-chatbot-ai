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
          {message.content}
        </p>
        <p className={`text-xs mt-1.5 ${isUser ? 'text-blue-200' : 'text-gray-400'}`}>
          {time}
        </p>
      </div>
    </div>
  )
}
