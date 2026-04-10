export type MessageRole = 'system' | 'user' | 'assistant'

export type ConversationStatus = 'active' | 'idle' | 'error'

export interface ChatMessage {
  messageId: string
  role: MessageRole
  content: string
  createdAtUtc: string
}

export interface Conversation {
  conversationId: string
  title: string
  model: string
  status: ConversationStatus
  messages: ChatMessage[]
}

export interface ModelSummary {
  name: string
  displayName: string
  isDefault: boolean
  description?: string | null
}

export interface ApiError {
  code: string
  message: string
  target?: string
  details?: string[]
}
