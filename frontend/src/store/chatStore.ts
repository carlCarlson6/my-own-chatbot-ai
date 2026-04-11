import { create } from 'zustand'
import {
  deleteConversation as apiDeleteConversation,
  getConversationHistory,
  listConversations,
  renameConversation as apiRenameConversation,
  sendMessage as apiSendMessage,
  streamMessage as apiStreamMessage,
} from '../api/conversations'
import { ChatApiError } from '../api/client'
import type { ChatMessage, ConversationSummary } from '../types/chat'
import type {
  ConversationStreamChunkEvent,
  ConversationStreamCompletedEvent,
  ConversationStreamStartedEvent,
} from '../api/schemas'

type ChatMode = 'anonymous' | 'authenticated'
type SendStatus = 'idle' | 'sending' | 'streaming' | 'error'
type SidebarStatus = 'idle' | 'loading' | 'error'
type HistoryStatus = 'idle' | 'loading' | 'error'

const STREAMING_SEND_ENABLED = true

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
  const assistantMessageIndex = nextMessages.findIndex(
    (message) => message.messageId === assistantMessage.messageId,
  )

  if (assistantMessageIndex >= 0) {
    nextMessages.splice(assistantMessageIndex, 1, assistantMessage)
  } else {
    const assistantInsertIndex =
      userMessageIndex >= 0 ? userMessageIndex + 1 : nextMessages.length
    nextMessages.splice(assistantInsertIndex, 0, assistantMessage)
  }

  return nextMessages
}

function reconcileStreamingStart(
  messages: ChatMessage[],
  optimisticMessageId: string,
  userMessage: ChatMessage,
  assistantMessageId: string,
): ChatMessage[] {
  return reconcileMessages(
    messages,
    optimisticMessageId,
    userMessage,
    {
      messageId: assistantMessageId,
      role: 'assistant',
      content: '',
      createdAtUtc: new Date().toISOString(),
      isStreaming: true,
    },
  )
}

function appendStreamingAssistantDelta(
  messages: ChatMessage[],
  event: ConversationStreamChunkEvent,
): ChatMessage[] {
  const nextMessages = [...messages]
  const assistantIndex = nextMessages.findIndex(
    (message) => message.messageId === event.assistantMessageId,
  )

  if (assistantIndex >= 0) {
    const assistantMessage = nextMessages[assistantIndex]

    nextMessages.splice(assistantIndex, 1, {
      ...assistantMessage,
      content: assistantMessage.content + event.delta,
      isStreaming: true,
    })

    return nextMessages
  }

  nextMessages.push({
    messageId: event.assistantMessageId,
    role: 'assistant',
    content: event.delta,
    createdAtUtc: new Date().toISOString(),
    isStreaming: true,
  })

  return nextMessages
}

function removeMessage(messages: ChatMessage[], messageId: string | null): ChatMessage[] {
  if (!messageId) {
    return messages
  }

  return messages.filter((message) => message.messageId !== messageId)
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

let activeViewEpoch = 0

function advanceActiveViewEpoch() {
  activeViewEpoch += 1
  return activeViewEpoch
}

function getActiveViewEpoch() {
  return activeViewEpoch
}

function shouldUpsertConversation(chatMode: ChatMode) {
  return chatMode === 'authenticated'
}

export const useChatStore = create<ChatState>()((set, get) => ({
  ...createBaseState('anonymous'),

  initializeChat: (mode) => {
    const { chatMode } = get()
    if (chatMode === mode) {
      return
    }

    advanceActiveViewEpoch()
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
    if (
      (sendStatus === 'sending' || sendStatus === 'streaming') &&
      activeConversationId === conversationId
    ) {
      return
    }

    if (sendStatus === 'sending' || sendStatus === 'streaming') {
      return
    }

    const summary =
      get().conversations.find((conversation) => conversation.conversationId === conversationId) ?? null
    const requestViewEpoch = advanceActiveViewEpoch()

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

      if (
        requestViewEpoch !== getActiveViewEpoch() ||
        get().activeConversationId !== conversationId
      ) {
        return
      }

      set((state) => ({
        activeConversationId: history.conversationId,
        activeConversation: nextConversation,
        messages: history.messages as ChatMessage[],
        historyStatus: 'idle',
        historyErrorMessage: null,
        conversations: upsertConversation(state.conversations, nextConversation),
      }))
    } catch (error) {
      if (
        requestViewEpoch !== getActiveViewEpoch() ||
        get().activeConversationId !== conversationId
      ) {
        return
      }

      set({
        historyStatus: 'error',
        historyErrorMessage: normalizeApiError(error, 'Failed to load conversation history'),
      })
    }
  },

  startNewConversation: () => {
    advanceActiveViewEpoch()

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
    })
  },

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
      const isActiveConversation = get().activeConversationId === conversationId

      if (isActiveConversation) {
        advanceActiveViewEpoch()
      }

      set((state) => {
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
    const { activeConversationId, sendStatus, chatMode } = get()
    if (sendStatus === 'sending' || sendStatus === 'streaming') {
      return
    }

    const optimisticMessageId = createClientMessageId()
    const optimisticUserMessage = createOptimisticUserMessage(content, optimisticMessageId)
    const requestViewEpoch = getActiveViewEpoch()
    let activeAssistantMessageId: string | null = null

    set((state) => ({
      messages: [...state.messages, optimisticUserMessage],
      sendStatus: 'sending',
      errorMessage: null,
      historyStatus: 'idle',
      historyErrorMessage: null,
    }))

    try {
      const request = {
        conversationId: activeConversationId ?? undefined,
        message: {
          content,
          clientMessageId: optimisticMessageId,
        },
      }

      if (STREAMING_SEND_ENABLED) {
        await apiStreamMessage(request, {
          onStarted: (event: ConversationStreamStartedEvent) => {
            if (requestViewEpoch !== getActiveViewEpoch()) {
              return
            }

            activeAssistantMessageId = event.assistantMessageId

            set((state) => ({
              activeConversationId: event.conversationId,
              activeConversation: event.conversation,
              messages: reconcileStreamingStart(
                state.messages,
                optimisticMessageId,
                event.userMessage as ChatMessage,
                event.assistantMessageId,
              ),
              sendStatus: 'streaming',
              errorMessage: null,
              conversations: shouldUpsertConversation(chatMode)
                ? upsertConversation(state.conversations, event.conversation)
                : state.conversations,
            }))
          },
          onChunk: (event: ConversationStreamChunkEvent) => {
            if (requestViewEpoch !== getActiveViewEpoch()) {
              return
            }

            activeAssistantMessageId = event.assistantMessageId

            set((state) => ({
              messages: appendStreamingAssistantDelta(state.messages, event),
              sendStatus: 'streaming',
              errorMessage: null,
            }))
          },
          onCompleted: (event: ConversationStreamCompletedEvent) => {
            if (requestViewEpoch !== getActiveViewEpoch()) {
              return
            }

            activeAssistantMessageId = event.assistantMessage.messageId

            set((state) => ({
              activeConversationId: event.conversationId,
              activeConversation: event.conversation,
              messages: reconcileMessages(
                state.messages,
                optimisticMessageId,
                event.userMessage as ChatMessage,
                event.assistantMessage as ChatMessage,
              ),
              sendStatus: 'idle',
              errorMessage: null,
              conversations: shouldUpsertConversation(chatMode)
                ? upsertConversation(state.conversations, event.conversation)
                : state.conversations,
            }))
          },
          onError: (event) => {
            if (requestViewEpoch !== getActiveViewEpoch()) {
              return
            }

            activeAssistantMessageId = event.assistantMessageId

            set((state) => ({
              messages: removeMessage(state.messages, event.assistantMessageId),
              sendStatus: 'error',
              errorMessage: event.message,
            }))
          },
        })
      } else {
        const result = await apiSendMessage(request)

        if (requestViewEpoch !== getActiveViewEpoch()) {
          return
        }

        set((state) => ({
          activeConversationId: result.conversationId,
          activeConversation: result.conversation,
          messages: reconcileMessages(
            state.messages,
            optimisticMessageId,
            result.userMessage as ChatMessage,
            result.assistantMessage as ChatMessage,
          ),
          sendStatus: 'idle',
          errorMessage: null,
          conversations: shouldUpsertConversation(chatMode)
            ? upsertConversation(state.conversations, result.conversation)
            : state.conversations,
        }))
      }
    } catch (error) {
      if (requestViewEpoch !== getActiveViewEpoch()) {
        return
      }

      set((state) => ({
        messages: removeMessage(state.messages, activeAssistantMessageId),
        sendStatus: 'error',
        errorMessage: normalizeApiError(error, 'Failed to send message'),
      }))
    }
  },

  clearError: () => set({ sendStatus: 'idle', errorMessage: null }),
  clearSidebarError: () => set({ sidebarStatus: 'idle', sidebarErrorMessage: null }),
  clearHistoryError: () => set({ historyStatus: 'idle', historyErrorMessage: null }),
}))
