import { z } from 'zod'

// ── Shared enums ─────────────────────────────────────────────────────────────

const messageRoleSchema = z.enum(['system', 'user', 'assistant'])
const conversationStatusSchema = z.enum(['active', 'idle', 'error'])
const trimmedConversationTitleSchema = z.string().trim().min(1).max(120)
const trimmedOptionalConversationTitleSchema = z.string().trim().min(1).max(120).optional()
const trimmedMessageContentSchema = z.string().trim().min(1).max(8000)

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

export const conversationSummarySchema = z.object({
  conversationId: z.string(),
  title: z.string(),
  hasManualTitle: z.boolean(),
  model: z.string(),
  createdAtUtc: z.string(),
  updatedAtUtc: z.string(),
  status: conversationStatusSchema,
})

export const createConversationRequestSchema = z
  .object({
    title: trimmedOptionalConversationTitleSchema,
  })
  .strict()

export const createConversationResponseSchema = conversationSummarySchema

export const chatMessageInputSchema = z
  .object({
    content: trimmedMessageContentSchema,
    clientMessageId: z.string().optional(),
  })
  .strict()

export const sendMessageRequestSchema = z
  .object({
    conversationId: z.string().nullable().optional(),
    message: chatMessageInputSchema,
  })
  .strict()

export const sendMessageResponseSchema = z.object({
  conversationId: z.string(),
  userMessage: chatMessageSchema,
  assistantMessage: chatMessageSchema,
  conversation: conversationSummarySchema,
  model: z.string(),
  status: z.enum(['completed', 'failed']),
  latencyMs: z.number().int().optional(),
})

export const getConversationHistoryResponseSchema = z.object({
  conversationId: z.string(),
  title: z.string(),
  hasManualTitle: z.boolean(),
  model: z.string(),
  createdAtUtc: z.string(),
  updatedAtUtc: z.string(),
  status: conversationStatusSchema,
  messages: z.array(chatMessageSchema),
})

export const listConversationsResponseSchema = z.array(conversationSummarySchema)

export const renameConversationRequestSchema = z
  .object({
    title: trimmedConversationTitleSchema,
  })
  .strict()

export const renameConversationResponseSchema = conversationSummarySchema

// ── Inferred types (exported for use across the API layer) ────────────────────

export type ConversationSummary = z.infer<typeof conversationSummarySchema>
export type CreateConversationRequest = z.infer<typeof createConversationRequestSchema>
export type CreateConversationResponse = z.infer<typeof createConversationResponseSchema>
export type ChatMessageInput = z.infer<typeof chatMessageInputSchema>
export type SendMessageRequest = z.infer<typeof sendMessageRequestSchema>
export type SendMessageResponse = z.infer<typeof sendMessageResponseSchema>
export type GetConversationHistoryResponse = z.infer<typeof getConversationHistoryResponseSchema>
export type ListConversationsResponse = z.infer<typeof listConversationsResponseSchema>
export type RenameConversationRequest = z.infer<typeof renameConversationRequestSchema>
export type RenameConversationResponse = z.infer<typeof renameConversationResponseSchema>
