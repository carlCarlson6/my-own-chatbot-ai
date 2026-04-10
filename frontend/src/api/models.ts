import { apiClient } from './client'
import { listModelsResponseSchema } from './schemas'
import type { ListModelsResponse } from './schemas'

export async function listModels(): Promise<ListModelsResponse> {
  const response = await apiClient.get('/api/models')
  return listModelsResponseSchema.parse(response.data)
}
