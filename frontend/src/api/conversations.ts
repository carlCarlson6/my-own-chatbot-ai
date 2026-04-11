import { ChatApiError, apiDelete, apiGet, apiPatch, apiPost, createHeaders } from './client'
import {
  apiErrorSchema,
  createConversationRequestSchema,
  createConversationResponseSchema,
  conversationStreamEventSchema,
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
  ConversationStreamChunkEvent,
  ConversationStreamCompletedEvent,
  ConversationStreamErrorEvent,
  ConversationStreamStartedEvent,
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

interface StreamMessageCallbacks {
  onStarted?: (event: ConversationStreamStartedEvent) => void
  onChunk?: (event: ConversationStreamChunkEvent) => void
  onCompleted?: (event: ConversationStreamCompletedEvent) => void
  onError?: (event: ConversationStreamErrorEvent) => void
}

function parseApiErrorBody(response: Response, bodyText: string): never {
  let body: unknown = null

  try {
    body = bodyText ? JSON.parse(bodyText) : null
  } catch {
    body = null
  }

  const parsed = apiErrorSchema.safeParse(body)

  if (parsed.success) {
    throw new ChatApiError(parsed.data)
  }

  throw new ChatApiError({
    code: 'UNKNOWN_ERROR',
    message: `Request failed with status ${response.status}`,
  })
}

function parseEventFrame(frame: string) {
  let eventName: string | null = null
  const dataLines: string[] = []

  for (const line of frame.split('\n')) {
    if (!line || line.startsWith(':')) {
      continue
    }

    if (line.startsWith('event:')) {
      eventName = line.slice('event:'.length).trim()
      continue
    }

    if (line.startsWith('data:')) {
      dataLines.push(line.slice('data:'.length).trimStart())
    }
  }

  if (dataLines.length === 0) {
    return null
  }

  const payload = conversationStreamEventSchema.parse(JSON.parse(dataLines.join('\n')))

  if (eventName && eventName !== payload.type) {
    throw new Error(
      `Unexpected SSE event type mismatch: frame "${eventName}" does not match payload "${payload.type}".`,
    )
  }

  return payload
}

function dispatchStreamEvent(payload: ReturnType<typeof parseEventFrame>, callbacks: StreamMessageCallbacks) {
  if (!payload) {
    return false
  }

  switch (payload.type) {
    case 'started':
      callbacks.onStarted?.(payload)
      return false
    case 'chunk':
      callbacks.onChunk?.(payload)
      return false
    case 'completed':
      callbacks.onCompleted?.(payload)
      return true
    case 'error':
      callbacks.onError?.(payload)
      return true
  }
}

export async function streamMessage(
  req: SendMessageRequest,
  callbacks: StreamMessageCallbacks,
): Promise<void> {
  const payload = sendMessageRequestSchema.parse(req)
  const response = await fetch('/api/conversations/stream', {
    method: 'POST',
    headers: await createHeaders(),
    body: JSON.stringify(payload),
  })

  if (!response.ok) {
    parseApiErrorBody(response, await response.text())
  }

  if (!response.body) {
    throw new Error('Streaming response did not include a readable body.')
  }

  const reader = response.body.getReader()
  const decoder = new TextDecoder()
  let buffer = ''
  let sawTerminalEvent = false

  while (true) {
    const { value, done } = await reader.read()

    if (done) {
      break
    }

    buffer += decoder.decode(value, { stream: true }).replaceAll('\r\n', '\n')

    let boundaryIndex = buffer.indexOf('\n\n')

    while (boundaryIndex >= 0) {
      const frame = buffer.slice(0, boundaryIndex)
      buffer = buffer.slice(boundaryIndex + 2)

      const shouldStop = dispatchStreamEvent(parseEventFrame(frame), callbacks)

      if (shouldStop) {
        sawTerminalEvent = true
        await reader.cancel()
        return
      }

      boundaryIndex = buffer.indexOf('\n\n')
    }
  }

  buffer += decoder.decode().replaceAll('\r\n', '\n')

  if (buffer.trim()) {
    sawTerminalEvent = dispatchStreamEvent(parseEventFrame(buffer), callbacks)
  }

  if (!sawTerminalEvent) {
    throw new Error('Streaming response ended before a completed or error event was received.')
  }
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
