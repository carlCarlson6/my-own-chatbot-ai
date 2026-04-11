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

function createClientMessageId() {
  if (globalThis.crypto?.randomUUID) {
    return globalThis.crypto.randomUUID()
  }

  return `client-${Date.now()}-${Math.random().toString(36).slice(2)}`
}

function createOptimisticUserMessage(content: string, clientMessageId: string): ChatMessage {
  return {
    messageId: clientMessageId,
    role: 'user',
    content,
    createdAtUtc: new Date().toISOString(),
  }
}

function reconcileMessages(
  messages: ChatMessage[],
  optimisticMessageId: string,
  userMessage: ChatMessage,
  assistantMessage: ChatMessage,
): ChatMessage[] {
  const nextMessages = [...messages]
  const optimisticIndex = nextMessages.findIndex((message) => message.messageId === optimisticMessageId)

  if (optimisticIndex >= 0) {
    nextMessages.splice(optimisticIndex, 1, userMessage)
  } else if (!nextMessages.some((message) => message.messageId === userMessage.messageId)) {
    nextMessages.push(userMessage)
  }

  const userMessageIndex = nextMessages.findIndex((message) => message.messageId === userMessage.messageId)
  const hasAssistantMessage = nextMessages.some(
    (message) => message.messageId === assistantMessage.messageId,
  )

  if (!hasAssistantMessage) {
    const assistantInsertIndex =
      userMessageIndex >= 0 ? userMessageIndex + 1 : nextMessages.length
    nextMessages.splice(assistantInsertIndex, 0, assistantMessage)
  }

  return nextMessages
}

// ── Store ─────────────────────────────────────────────────────────────────────

export const useChatStore = create<ChatState>()((set, get) => ({
  conversationId: null,
  messages: [],
  status: 'idle',
  errorMessage: null,

  sendMessage: async (content) => {
    const { conversationId, status } = get()
    if (status === 'sending') return

    const optimisticMessageId = createClientMessageId()
    const optimisticUserMessage = createOptimisticUserMessage(content, optimisticMessageId)

    set((state) => ({
      messages: [...state.messages, optimisticUserMessage],
      status: 'sending',
      errorMessage: null,
    }))

    try {
      const result = await apiSendMessage({
        conversationId: conversationId ?? undefined,
        message: {
          content,
        },
      })
      set((state) => ({
        conversationId: result.conversationId,
        messages: reconcileMessages(
          state.messages,
          optimisticMessageId,
          result.userMessage as ChatMessage,
          result.assistantMessage as ChatMessage,
        ),
        status: 'idle',
        errorMessage: null,
      }))
    } catch (error) {
      const message =
        error instanceof ChatApiError ? error.apiError.message : 'Failed to send message'
      set({ status: 'error', errorMessage: message })
    }
  },

  clearError: () => set({ status: 'idle', errorMessage: null }),
}))
