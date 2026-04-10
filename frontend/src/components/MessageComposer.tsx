import { useState } from 'react'
import type { KeyboardEvent } from 'react'

interface MessageComposerProps {
  onSend: (content: string) => void
  isSending: boolean
}

export function MessageComposer({ onSend, isSending }: MessageComposerProps) {
  const [value, setValue] = useState('')

  const handleSend = () => {
    const trimmed = value.trim()
    if (!trimmed || isSending) return
    onSend(trimmed)
    setValue('')
  }

  const handleKeyDown = (e: KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault()
      handleSend()
    }
  }

  return (
    <div className="border-t border-gray-800 px-4 py-3 bg-gray-950">
      <div className="flex gap-2 items-end max-w-4xl mx-auto">
        <textarea
          className="flex-1 bg-gray-800 text-gray-100 rounded-xl px-4 py-3 resize-none focus:outline-none focus:ring-2 focus:ring-blue-500 placeholder-gray-500 text-sm leading-relaxed disabled:opacity-50"
          rows={1}
          value={value}
          onChange={(e) => setValue(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder="Type a message… (Enter to send, Shift+Enter for new line)"
          disabled={isSending}
          aria-label="Message input"
        />
        <button
          onClick={handleSend}
          disabled={isSending || !value.trim()}
          className="bg-blue-600 hover:bg-blue-500 disabled:bg-gray-700 disabled:cursor-not-allowed text-white rounded-xl px-5 py-3 text-sm font-medium transition-colors min-w-[72px] flex items-center justify-center"
          aria-label="Send message"
        >
          {isSending ? (
            <span
              className="inline-block w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"
              aria-hidden="true"
            />
          ) : (
            'Send'
          )}
        </button>
      </div>
    </div>
  )
}
