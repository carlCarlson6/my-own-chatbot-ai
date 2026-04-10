import type { ModelSummary } from '../types/chat'

interface ModelSelectorProps {
  models: ModelSummary[]
  selectedModel: string
  onSelect: (model: string) => void
  disabled: boolean
}

export function ModelSelector({ models, selectedModel, onSelect, disabled }: ModelSelectorProps) {
  return (
    <select
      value={selectedModel}
      onChange={(e) => onSelect(e.target.value)}
      disabled={disabled || models.length === 0}
      className="bg-gray-800 text-gray-100 text-sm rounded-lg px-3 py-1.5 border border-gray-700 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
      aria-label="Select AI model"
    >
      {models.length === 0 ? (
        <option value="">Loading models…</option>
      ) : (
        models.map((model) => (
          <option key={model.name} value={model.name}>
            {model.displayName}
          </option>
        ))
      )}
    </select>
  )
}
