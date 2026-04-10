import { create } from 'zustand'
import type { ChatMessage, ModelSummary } from '../types/chat'
import { sendMessage as apiSendMessage } from '../api/conversations'
import { listModels as apiListModels } from '../api/models'
import { ChatApiError } from '../api/client'

// ── State shape ───────────────────────────────────────────────────────────────

type ChatStatus = 'idle' | 'sending' | 'error'

interface ChatState {
  // Conversation
  conversationId: string | null
  messages: ChatMessage[]
  model: string
  availableModels: ModelSummary[]

  // UI status
  status: ChatStatus
  errorMessage: string | null

  // Actions
  setModel: (model: string) => void
  loadModels: () => Promise<void>
  sendMessage: (content: string) => Promise<void>
  clearError: () => void
}

// ── Store ─────────────────────────────────────────────────────────────────────

export const useChatStore = create<ChatState>()((set, get) => ({
  conversationId: null,
  messages: [],
  model: '',
  availableModels: [],
  status: 'idle',
  errorMessage: null,

  setModel: (model) => set({ model }),

  loadModels: async () => {
    try {
      const result = await apiListModels()
      const currentModel = get().model
      const defaultModel = result.models.find((m) => m.isDefault)
      set({
        availableModels: result.models,
        model: currentModel || defaultModel?.name || result.models[0]?.name || '',
      })
    } catch (error) {
      const message =
        error instanceof ChatApiError ? error.apiError.message : 'Failed to load models'
      set({ status: 'error', errorMessage: message })
    }
  },

  sendMessage: async (content) => {
    const { conversationId, model } = get()
    set({ status: 'sending' })
    try {
      const result = await apiSendMessage({
        conversationId: conversationId ?? undefined,
        model: model || undefined,
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
