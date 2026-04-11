import { create } from 'zustand'
import {
  deleteConversation as apiDeleteConversation,
  getConversationHistory,
  listConversations,
  renameConversation as apiRenameConversation,
  sendMessage as apiSendMessage,
} from '../api/conversations'
import { ChatApiError } from '../api/client'
import type { ChatMessage, ConversationSummary } from '../types/chat'

type ChatMode = 'anonymous' | 'authenticated'
type SendStatus = 'idle' | 'sending' | 'error'
type SidebarStatus = 'idle' | 'loading' | 'error'
type HistoryStatus = 'idle' | 'loading' | 'error'

interface ChatState {
  chatMode: ChatMode
  conversations: ConversationSummary[]
  activeConversationId: string | null
  activeConversation: ConversationSummary | null
  messages: ChatMessage[]
  sendStatus: SendStatus
  errorMessage: string | null
  sidebarStatus: SidebarStatus
  sidebarErrorMessage: string | null
  historyStatus: HistoryStatus
  historyErrorMessage: string | null
  initializeChat: (mode: ChatMode) => void
  loadConversations: () => Promise<void>
  activateConversation: (conversationId: string) => Promise<void>
  startNewConversation: () => void
  renameConversation: (conversationId: string, title: string) => Promise<void>
  deleteConversation: (conversationId: string) => Promise<void>
  sendMessage: (content: string) => Promise<void>
  clearError: () => void
  clearSidebarError: () => void
  clearHistoryError: () => void
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

function normalizeApiError(error: unknown, fallback: string) {
  return error instanceof ChatApiError ? error.apiError.message : fallback
}

function sortConversations(conversations: ConversationSummary[]) {
  return [...conversations].sort((left, right) => right.updatedAtUtc.localeCompare(left.updatedAtUtc))
}

function upsertConversation(
  conversations: ConversationSummary[],
  conversation: ConversationSummary,
): ConversationSummary[] {
  const nextConversations = conversations.filter(
    (item) => item.conversationId !== conversation.conversationId,
  )

  nextConversations.push(conversation)
  return sortConversations(nextConversations)
}

function createConversationSummary(
  conversation: Omit<ConversationSummary, 'messages'>,
): ConversationSummary {
  return {
    conversationId: conversation.conversationId,
    title: conversation.title,
    hasManualTitle: conversation.hasManualTitle,
    model: conversation.model,
    createdAtUtc: conversation.createdAtUtc,
    updatedAtUtc: conversation.updatedAtUtc,
    status: conversation.status,
  }
}

function createBaseState(mode: ChatMode) {
  return {
    chatMode: mode,
    conversations: [] as ConversationSummary[],
    activeConversationId: null,
    activeConversation: null as ConversationSummary | null,
    messages: [] as ChatMessage[],
    sendStatus: 'idle' as SendStatus,
    errorMessage: null as string | null,
    sidebarStatus: 'idle' as SidebarStatus,
    sidebarErrorMessage: null as string | null,
    historyStatus: 'idle' as HistoryStatus,
    historyErrorMessage: null as string | null,
  }
}

export const useChatStore = create<ChatState>()((set, get) => ({
  ...createBaseState('anonymous'),

  initializeChat: (mode) => {
    const { chatMode } = get()
    if (chatMode === mode) {
      return
    }

    set(createBaseState(mode))
  },

  loadConversations: async () => {
    const { chatMode } = get()
    if (chatMode !== 'authenticated') {
      return
    }

    set({
      sidebarStatus: 'loading',
      sidebarErrorMessage: null,
    })

    try {
      const conversations = await listConversations()
      set((state) => ({
        conversations: sortConversations(conversations),
        sidebarStatus: 'idle',
        sidebarErrorMessage: null,
        activeConversation:
          state.activeConversationId === null
            ? state.activeConversation
            : conversations.find(
                (conversation) => conversation.conversationId === state.activeConversationId,
              ) ?? state.activeConversation,
      }))
    } catch (error) {
      set({
        sidebarStatus: 'error',
        sidebarErrorMessage: normalizeApiError(error, 'Failed to load conversations'),
      })
    }
  },

  activateConversation: async (conversationId) => {
    const { activeConversationId, sendStatus } = get()
    if (sendStatus === 'sending' || activeConversationId === conversationId) {
      return
    }

    const summary =
      get().conversations.find((conversation) => conversation.conversationId === conversationId) ?? null

    set({
      activeConversationId: conversationId,
      activeConversation: summary,
      messages: [],
      historyStatus: 'loading',
      historyErrorMessage: null,
      errorMessage: null,
      sendStatus: 'idle',
    })

    try {
      const history = await getConversationHistory(conversationId)
      const nextConversation = createConversationSummary(history)

      set((state) => ({
        activeConversationId: history.conversationId,
        activeConversation: nextConversation,
        messages: history.messages as ChatMessage[],
        historyStatus: 'idle',
        historyErrorMessage: null,
        conversations: upsertConversation(state.conversations, nextConversation),
      }))
    } catch (error) {
      set({
        historyStatus: 'error',
        historyErrorMessage: normalizeApiError(error, 'Failed to load conversation history'),
      })
    }
  },

  startNewConversation: () =>
    set({
      activeConversationId: null,
      activeConversation: null,
      messages: [],
      sendStatus: 'idle',
      errorMessage: null,
      sidebarStatus: 'idle',
      sidebarErrorMessage: null,
      historyStatus: 'idle',
      historyErrorMessage: null,
    }),

  renameConversation: async (conversationId, title) => {
    set({
      sidebarStatus: 'loading',
      sidebarErrorMessage: null,
    })

    try {
      const renamedConversation = await apiRenameConversation(conversationId, { title })

      set((state) => ({
        conversations: upsertConversation(state.conversations, renamedConversation),
        activeConversation:
          state.activeConversationId === renamedConversation.conversationId
            ? renamedConversation
            : state.activeConversation,
        sidebarStatus: 'idle',
        sidebarErrorMessage: null,
      }))
    } catch (error) {
      set({
        sidebarStatus: 'error',
        sidebarErrorMessage: normalizeApiError(error, 'Failed to rename conversation'),
      })
      throw error
    }
  },

  deleteConversation: async (conversationId) => {
    set({
      sidebarStatus: 'loading',
      sidebarErrorMessage: null,
    })

    try {
      await apiDeleteConversation(conversationId)

      set((state) => {
        const isActiveConversation = state.activeConversationId === conversationId

        return {
          conversations: state.conversations.filter(
            (conversation) => conversation.conversationId !== conversationId,
          ),
          activeConversationId: isActiveConversation ? null : state.activeConversationId,
          activeConversation: isActiveConversation ? null : state.activeConversation,
          messages: isActiveConversation ? [] : state.messages,
          sendStatus: isActiveConversation ? 'idle' : state.sendStatus,
          errorMessage: isActiveConversation ? null : state.errorMessage,
          historyStatus: isActiveConversation ? 'idle' : state.historyStatus,
          historyErrorMessage: isActiveConversation ? null : state.historyErrorMessage,
          sidebarStatus: 'idle',
          sidebarErrorMessage: null,
        }
      })
    } catch (error) {
      set({
        sidebarStatus: 'error',
        sidebarErrorMessage: normalizeApiError(error, 'Failed to delete conversation'),
      })
      throw error
    }
  },

  sendMessage: async (content) => {
    const { activeConversationId, messages, sendStatus, chatMode } = get()
    if (sendStatus === 'sending') {
      return
    }

    const optimisticMessageId = createClientMessageId()
    const optimisticUserMessage = createOptimisticUserMessage(content, optimisticMessageId)

    set((state) => ({
      messages: [...state.messages, optimisticUserMessage],
      sendStatus: 'sending',
      errorMessage: null,
      historyStatus: 'idle',
      historyErrorMessage: null,
    }))

    try {
      const result = await apiSendMessage({
        conversationId: activeConversationId ?? undefined,
        message: {
          content,
          clientMessageId: optimisticMessageId,
        },
      })

      set((state) => ({
        activeConversationId: result.conversationId,
        activeConversation: result.conversation,
        messages: reconcileMessages(
          messages,
          optimisticMessageId,
          result.userMessage as ChatMessage,
          result.assistantMessage as ChatMessage,
        ),
        sendStatus: 'idle',
        errorMessage: null,
        conversations:
          chatMode === 'authenticated'
            ? upsertConversation(state.conversations, result.conversation)
            : state.conversations,
      }))
    } catch (error) {
      set({
        sendStatus: 'error',
        errorMessage: normalizeApiError(error, 'Failed to send message'),
      })
    }
  },

  clearError: () => set({ sendStatus: 'idle', errorMessage: null }),
  clearSidebarError: () => set({ sidebarStatus: 'idle', sidebarErrorMessage: null }),
  clearHistoryError: () => set({ historyStatus: 'idle', historyErrorMessage: null }),
}))
