import { apiGet } from './client'
import { listModelsResponseSchema } from './schemas'
import type { ListModelsResponse } from './schemas'

export async function listModels(): Promise<ListModelsResponse> {
  const data = await apiGet('/api/models')
  return listModelsResponseSchema.parse(data)
}
