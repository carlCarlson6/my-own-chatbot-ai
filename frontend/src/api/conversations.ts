import { apiGet, apiPost } from './client'
import {
  createConversationResponseSchema,
  sendMessageResponseSchema,
  getConversationHistoryResponseSchema,
} from './schemas'
import type {
  CreateConversationResponse,
  SendMessageResponse,
  GetConversationHistoryResponse,
} from './schemas'

// ── Request shapes (mirror OpenAPI contract) ──────────────────────────────────

export interface CreateConversationRequest {
  title?: string
  model?: string
}

export interface ChatMessageInput {
  content: string
  clientMessageId?: string
}

export interface SendMessageRequest {
  conversationId?: string | null
  model?: string
  message: ChatMessageInput
}

// ── API functions ─────────────────────────────────────────────────────────────

export async function createConversation(
  req?: CreateConversationRequest,
): Promise<CreateConversationResponse> {
  const data = await apiPost('/api/conversations', req ?? {})
  return createConversationResponseSchema.parse(data)
}

export async function sendMessage(req: SendMessageRequest): Promise<SendMessageResponse> {
  const data = await apiPost('/api/conversations/send', req)
  return sendMessageResponseSchema.parse(data)
}

export async function getConversationHistory(
  conversationId: string,
): Promise<GetConversationHistoryResponse> {
  const data = await apiGet(`/api/conversations/${conversationId}/history`)
  return getConversationHistoryResponseSchema.parse(data)
}
