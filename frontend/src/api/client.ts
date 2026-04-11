import type { ApiError } from '../types/chat'
import { getAuthToken } from '../auth/clerk'
import { apiErrorSchema } from './schemas'

// ── Typed API error ───────────────────────────────────────────────────────────

export class ChatApiError extends Error {
  readonly apiError: ApiError

  constructor(apiError: ApiError) {
    super(apiError.message)
    this.name = 'ChatApiError'
    this.apiError = apiError
  }
}

// ── Core fetch wrapper ────────────────────────────────────────────────────────
//
// baseURL is intentionally empty — in development Vite proxies /api/* to the
// backend at http://localhost:5050 (see vite.config.ts). In production nginx
// does the same proxying.

async function handleResponse(response: Response): Promise<unknown> {
  const text = await response.text()
  const body: unknown = text ? JSON.parse(text) : null

  if (!response.ok) {
    const parsed = apiErrorSchema.safeParse(body)
    if (parsed.success) throw new ChatApiError(parsed.data)
    throw new ChatApiError({
      code: 'UNKNOWN_ERROR',
      message: `Request failed with status ${response.status}`,
    })
  }

  return body
}

export async function createHeaders(): Promise<Headers> {
  const headers = new Headers({
    'Content-Type': 'application/json',
  })

  const token = await getAuthToken()
  if (token) {
    headers.set('Authorization', `Bearer ${token}`)
  }

  return headers
}

export async function apiGet(path: string): Promise<unknown> {
  const response = await fetch(path, {
    headers: await createHeaders(),
  })
  return handleResponse(response)
}

export async function apiPost(path: string, body: unknown): Promise<unknown> {
  const response = await fetch(path, {
    method: 'POST',
    headers: await createHeaders(),
    body: JSON.stringify(body),
  })
  return handleResponse(response)
}

export async function apiPatch(path: string, body: unknown): Promise<unknown> {
  const response = await fetch(path, {
    method: 'PATCH',
    headers: await createHeaders(),
    body: JSON.stringify(body),
  })
  return handleResponse(response)
}

export async function apiDelete(path: string): Promise<unknown> {
  const response = await fetch(path, {
    method: 'DELETE',
    headers: await createHeaders(),
  })
  return handleResponse(response)
}
