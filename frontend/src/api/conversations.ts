import { apiDelete, apiGet, apiPatch, apiPost } from './client'
import {
  createConversationRequestSchema,
  createConversationResponseSchema,
  getConversationHistoryResponseSchema,
  listConversationsResponseSchema,
  renameConversationRequestSchema,
  renameConversationResponseSchema,
  sendMessageRequestSchema,
  sendMessageResponseSchema,
} from './schemas'
import type {
  CreateConversationResponse,
  ConversationSummary,
  CreateConversationRequest,
  GetConversationHistoryResponse,
  RenameConversationRequest,
  SendMessageRequest,
  SendMessageResponse,
} from './schemas'

// ── API functions ─────────────────────────────────────────────────────────────

export async function createConversation(
  req?: CreateConversationRequest,
): Promise<CreateConversationResponse> {
  const payload = createConversationRequestSchema.parse(req ?? {})
  const data = await apiPost('/api/conversations', payload)
  return createConversationResponseSchema.parse(data)
}

export async function sendMessage(req: SendMessageRequest): Promise<SendMessageResponse> {
  const payload = sendMessageRequestSchema.parse(req)
  const data = await apiPost('/api/conversations/send', payload)
  return sendMessageResponseSchema.parse(data)
}

export async function listConversations(): Promise<ConversationSummary[]> {
  const data = await apiGet('/api/conversations')
  return listConversationsResponseSchema.parse(data)
}

export async function getConversationHistory(
  conversationId: string,
): Promise<GetConversationHistoryResponse> {
  const data = await apiGet(`/api/conversations/${conversationId}/history`)
  return getConversationHistoryResponseSchema.parse(data)
}

export async function renameConversation(
  conversationId: string,
  req: RenameConversationRequest,
): Promise<ConversationSummary> {
  const payload = renameConversationRequestSchema.parse(req)
  const data = await apiPatch(`/api/conversations/${conversationId}`, payload)
  return renameConversationResponseSchema.parse(data)
}

export async function deleteConversation(conversationId: string): Promise<void> {
  await apiDelete(`/api/conversations/${conversationId}`)
}
