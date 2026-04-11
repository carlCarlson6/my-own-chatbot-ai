export type MessageRole = 'system' | 'user' | 'assistant'

export type ConversationStatus = 'active' | 'idle' | 'error'

export interface ChatMessage {
  messageId: string
  role: MessageRole
  content: string
  createdAtUtc: string
}

export interface ConversationSummary {
  conversationId: string
  title: string
  hasManualTitle: boolean
  model: string
  createdAtUtc: string
  updatedAtUtc: string
  status: ConversationStatus
}

export interface Conversation extends ConversationSummary {
  messages: ChatMessage[]
}

export interface ApiError {
  code: string
  message: string
  target?: string
  details?: string[]
}
