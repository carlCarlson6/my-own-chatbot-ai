import { create } from 'zustand'
import type { ChatMessage } from '../types/chat'
import { sendMessage as apiSendMessage } from '../api/conversations'
import { ChatApiError } from '../api/client'

// ── State shape ───────────────────────────────────────────────────────────────

type ChatStatus = 'idle' | 'sending' | 'error'

interface ChatState {
  // Conversation
  conversationId: string | null
  messages: ChatMessage[]

  // UI status
  status: ChatStatus
  errorMessage: string | null

  // Actions
  sendMessage: (content: string) => Promise<void>
  clearError: () => void
}

// ── Store ─────────────────────────────────────────────────────────────────────

export const useChatStore = create<ChatState>()((set, get) => ({
  conversationId: null,
  messages: [],
  status: 'idle',
  errorMessage: null,

  sendMessage: async (content) => {
    const { conversationId } = get()
    set({ status: 'sending' })
    try {
      const result = await apiSendMessage({
        conversationId: conversationId ?? undefined,
        message: { content },
      })
      set((state) => ({
        conversationId: result.conversationId,
        messages: [
          ...state.messages,
          result.userMessage as ChatMessage,
          result.assistantMessage as ChatMessage,
        ],
        status: 'idle',
      }))
    } catch (error) {
      const message =
        error instanceof ChatApiError ? error.apiError.message : 'Failed to send message'
      set({ status: 'error', errorMessage: message })
    }
  },

  clearError: () => set({ status: 'idle', errorMessage: null }),
}))
