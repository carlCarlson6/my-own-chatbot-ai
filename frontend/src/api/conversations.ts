import { apiClient } from './client'
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
  const response = await apiClient.post('/api/conversations', req ?? {})
  return createConversationResponseSchema.parse(response.data)
}

export async function sendMessage(req: SendMessageRequest): Promise<SendMessageResponse> {
  const response = await apiClient.post('/api/conversations/send', req)
  return sendMessageResponseSchema.parse(response.data)
}

export async function getConversationHistory(
  conversationId: string,
): Promise<GetConversationHistoryResponse> {
  const response = await apiClient.get(`/api/conversations/${conversationId}/history`)
  return getConversationHistoryResponseSchema.parse(response.data)
}
