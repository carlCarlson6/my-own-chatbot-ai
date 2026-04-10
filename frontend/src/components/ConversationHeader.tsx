import { ModelSelector } from './ModelSelector'
import type { ModelSummary } from '../types/chat'

interface ConversationHeaderProps {
  models: ModelSummary[]
  selectedModel: string
  onModelChange: (model: string) => void
  isSending: boolean
}

export function ConversationHeader({
  models,
  selectedModel,
  onModelChange,
  isSending,
}: ConversationHeaderProps) {
  return (
    <header className="flex items-center justify-between px-4 py-3 border-b border-gray-800 bg-gray-950 shrink-0">
      <h1 className="text-sm font-semibold text-gray-100">New Conversation</h1>
      <ModelSelector
        models={models}
        selectedModel={selectedModel}
        onSelect={onModelChange}
        disabled={isSending}
      />
    </header>
  )
}
