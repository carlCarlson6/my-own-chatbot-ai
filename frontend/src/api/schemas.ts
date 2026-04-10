import { z } from 'zod'

// ── Shared enums ─────────────────────────────────────────────────────────────

const messageRoleSchema = z.enum(['system', 'user', 'assistant'])
const conversationStatusSchema = z.enum(['active', 'idle', 'error'])

// ── Error ─────────────────────────────────────────────────────────────────────

export const apiErrorSchema = z.object({
  code: z.string(),
  message: z.string(),
  target: z.string().optional(),
  details: z.array(z.string()).optional(),
})

// ── Shared message ────────────────────────────────────────────────────────────

export const chatMessageSchema = z.object({
  messageId: z.string(),
  role: messageRoleSchema,
  content: z.string(),
  createdAtUtc: z.string(),
})

// ── Conversation endpoints ────────────────────────────────────────────────────

export const createConversationResponseSchema = z.object({
  conversationId: z.string(),
  title: z.string(),
  model: z.string(),
  createdAtUtc: z.string(),
  status: conversationStatusSchema,
})

export const sendMessageResponseSchema = z.object({
  conversationId: z.string(),
  userMessage: chatMessageSchema,
  assistantMessage: chatMessageSchema,
  model: z.string(),
  status: z.enum(['completed', 'failed']),
  latencyMs: z.number().int().optional(),
})

export const getConversationHistoryResponseSchema = z.object({
  conversationId: z.string(),
  title: z.string(),
  model: z.string(),
  status: conversationStatusSchema,
  messages: z.array(chatMessageSchema),
})

// ── Models endpoint ───────────────────────────────────────────────────────────

export const modelSummarySchema = z.object({
  name: z.string(),
  displayName: z.string(),
  isDefault: z.boolean(),
  description: z.string().optional(),
})

export const listModelsResponseSchema = z.object({
  models: z.array(modelSummarySchema),
})

// ── Inferred types (exported for use across the API layer) ────────────────────

export type CreateConversationResponse = z.infer<typeof createConversationResponseSchema>
export type SendMessageResponse = z.infer<typeof sendMessageResponseSchema>
export type GetConversationHistoryResponse = z.infer<typeof getConversationHistoryResponseSchema>
export type ListModelsResponse = z.infer<typeof listModelsResponseSchema>
