import axios from 'axios'
import type { AxiosError } from 'axios'
import type { ApiError } from '../types/chat'
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

// ── Axios instance ─────────────────────────────────────────────────────────────
//
// baseURL is intentionally empty — in development Vite proxies /api/* to the
// backend at http://localhost:5050 (see vite.config.ts).  In production nginx
// does the same proxying.

export const apiClient = axios.create({
  headers: {
    'Content-Type': 'application/json',
  },
})

// ── Response interceptor — extract typed ApiError from 4xx/5xx ───────────────

apiClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError) => {
    if (error.response?.data) {
      const parsed = apiErrorSchema.safeParse(error.response.data)
      if (parsed.success) {
        throw new ChatApiError(parsed.data)
      }
    }
    throw new ChatApiError({
      code: 'UNKNOWN_ERROR',
      message: (error.message as string | undefined) ?? 'An unexpected error occurred',
    })
  },
)
