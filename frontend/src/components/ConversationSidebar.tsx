import { useMemo, useState } from 'react'
import type { ConversationSummary } from '../types/chat'

interface ConversationSidebarProps {
  conversations: ConversationSummary[]
  activeConversationId: string | null
  isOpen: boolean
  status: 'idle' | 'loading' | 'error'
  errorMessage: string | null
  onClose: () => void
  onSelectConversation: (conversationId: string) => void
  onNewConversation: () => void
  onRenameConversation: (conversationId: string, title: string) => Promise<void>
  onDeleteConversation: (conversationId: string) => Promise<void>
  onDismissError: () => void
}

interface ConversationSidebarItemProps {
  conversation: ConversationSummary
  isActive: boolean
  isBusy: boolean
  onSelect: () => void
  onRename: (title: string) => Promise<void>
  onDelete: () => Promise<void>
}

function formatUpdatedAt(value: string) {
  return new Date(value).toLocaleString([], {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

function ConversationSidebarItem({
  conversation,
  isActive,
  isBusy,
  onSelect,
  onRename,
  onDelete,
}: ConversationSidebarItemProps) {
  const [isEditing, setIsEditing] = useState(false)
  const [draftTitle, setDraftTitle] = useState(conversation.title)
  const trimmedDraftTitle = draftTitle.trim()

  const handleRenameSubmit = async () => {
    if (!trimmedDraftTitle || trimmedDraftTitle === conversation.title) {
      setIsEditing(false)
      setDraftTitle(conversation.title)
      return
    }

    await onRename(trimmedDraftTitle)
    setIsEditing(false)
  }

  const updatedAtLabel = useMemo(
    () => formatUpdatedAt(conversation.updatedAtUtc),
    [conversation.updatedAtUtc],
  )

  return (
    <li>
      <div
        className={`rounded-xl border transition ${
          isActive
            ? 'border-indigo-500/70 bg-indigo-500/10'
            : 'border-gray-800 bg-gray-900/70 hover:border-gray-700 hover:bg-gray-900'
        }`}
      >
        <div className="p-3">
          {isEditing ? (
            <div className="space-y-2">
              <input
                value={draftTitle}
                onChange={(event) => setDraftTitle(event.target.value)}
                className="w-full rounded-lg border border-gray-700 bg-gray-950 px-3 py-2 text-sm text-gray-100 outline-none focus:border-indigo-500"
                maxLength={120}
                autoFocus
                aria-label={`Rename ${conversation.title}`}
              />

              <div className="flex items-center justify-end gap-2">
                <button
                  type="button"
                  onClick={() => {
                    setIsEditing(false)
                    setDraftTitle(conversation.title)
                  }}
                  className="rounded-lg border border-gray-700 px-2.5 py-1.5 text-xs font-medium text-gray-200 transition hover:border-gray-500 hover:bg-gray-800"
                  disabled={isBusy}
                >
                  Cancel
                </button>
                <button
                  type="button"
                  onClick={() => void handleRenameSubmit()}
                  className="rounded-lg bg-indigo-500 px-2.5 py-1.5 text-xs font-semibold text-white transition hover:bg-indigo-400 disabled:cursor-not-allowed disabled:bg-gray-700"
                  disabled={isBusy || trimmedDraftTitle.length === 0}
                >
                  Save
                </button>
              </div>
            </div>
          ) : (
            <>
              <button
                type="button"
                onClick={onSelect}
                className="w-full text-left"
                disabled={isBusy}
              >
                <div className="flex items-start justify-between gap-3">
                  <div className="min-w-0">
                    <p className="truncate text-sm font-semibold text-gray-100">{conversation.title}</p>
                    <p className="mt-1 text-xs text-gray-400">
                      Updated {updatedAtLabel}
                    </p>
                  </div>
                  <span className="rounded-full border border-gray-700 px-2 py-0.5 text-[10px] font-medium uppercase tracking-wide text-gray-300">
                    {conversation.model}
                  </span>
                </div>
              </button>

              <div className="mt-3 flex items-center justify-end gap-2">
                <button
                  type="button"
                  onClick={() => {
                    setDraftTitle(conversation.title)
                    setIsEditing(true)
                  }}
                  className="rounded-lg border border-gray-700 px-2.5 py-1.5 text-xs font-medium text-gray-200 transition hover:border-gray-500 hover:bg-gray-800"
                  disabled={isBusy}
                  aria-label={`Rename ${conversation.title}`}
                >
                  Edit
                </button>
                <button
                  type="button"
                  onClick={() => void onDelete()}
                  className="rounded-lg border border-red-800/80 px-2.5 py-1.5 text-xs font-medium text-red-200 transition hover:bg-red-950/70"
                  disabled={isBusy}
                  aria-label={`Delete ${conversation.title}`}
                >
                  Delete
                </button>
              </div>
            </>
          )}
        </div>
      </div>
    </li>
  )
}

export function ConversationSidebar({
  conversations,
  activeConversationId,
  isOpen,
  status,
  errorMessage,
  onClose,
  onSelectConversation,
  onNewConversation,
  onRenameConversation,
  onDeleteConversation,
  onDismissError,
}: ConversationSidebarProps) {
  const isBusy = status === 'loading'

  return (
    <>
      <div
        className={`fixed inset-0 z-20 bg-gray-950/70 transition md:hidden ${
          isOpen ? 'pointer-events-auto opacity-100' : 'pointer-events-none opacity-0'
        }`}
        onClick={onClose}
        aria-hidden={!isOpen}
      />

      <aside
        className={`fixed inset-y-0 left-0 z-30 flex w-80 max-w-[85vw] flex-col border-r border-gray-800 bg-gray-950 transition-transform md:static md:z-auto md:max-w-none md:translate-x-0 ${
          isOpen ? 'translate-x-0' : '-translate-x-full'
        }`}
        aria-label="Saved conversations"
      >
        <div className="flex items-center justify-between gap-3 border-b border-gray-800 px-4 py-4">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.2em] text-gray-400">
              Signed-in workspace
            </p>
            <h2 className="text-lg font-semibold text-gray-100">Conversations</h2>
          </div>

          <button
            type="button"
            onClick={onClose}
            className="rounded-lg border border-gray-700 px-3 py-2 text-sm text-gray-200 transition hover:border-gray-500 hover:bg-gray-800 md:hidden"
          >
            Close
          </button>
        </div>

        <div className="border-b border-gray-800 px-4 py-4">
          <button
            type="button"
            onClick={onNewConversation}
            className="w-full rounded-xl bg-indigo-500 px-4 py-3 text-sm font-semibold text-white transition hover:bg-indigo-400"
          >
            New conversation
          </button>
          <p className="mt-2 text-xs text-gray-400">
            Select a saved conversation or start fresh with your next message.
          </p>
        </div>

        {errorMessage && (
          <div
            role="alert"
            className="mx-4 mt-4 flex items-start gap-3 rounded-xl border border-red-800 bg-red-950/50 px-3 py-3 text-sm text-red-200"
          >
            <span className="flex-1">{errorMessage}</span>
            <button
              type="button"
              onClick={onDismissError}
              className="text-red-200 transition hover:text-white"
              aria-label="Dismiss sidebar error"
            >
              ✕
            </button>
          </div>
        )}

        <div className="min-h-0 flex-1 overflow-y-auto px-4 py-4">
          {status === 'loading' && conversations.length === 0 ? (
            <div className="rounded-xl border border-gray-800 bg-gray-900/60 px-4 py-6 text-sm text-gray-300">
              Loading saved conversations…
            </div>
          ) : conversations.length === 0 ? (
            <div className="rounded-xl border border-dashed border-gray-800 bg-gray-900/40 px-4 py-6 text-sm text-gray-400">
              No saved conversations yet. Send a message to create your first one.
            </div>
          ) : (
            <ul className="space-y-3">
              {conversations.map((conversation) => (
                <ConversationSidebarItem
                  key={conversation.conversationId}
                  conversation={conversation}
                  isActive={conversation.conversationId === activeConversationId}
                  isBusy={isBusy}
                  onSelect={onSelectConversation.bind(null, conversation.conversationId)}
                  onRename={(title) => onRenameConversation(conversation.conversationId, title)}
                  onDelete={async () => {
                    const confirmed = window.confirm(
                      `Delete "${conversation.title}"? This removes its saved history.`,
                    )

                    if (!confirmed) {
                      return
                    }

                    await onDeleteConversation(conversation.conversationId)
                  }}
                />
              ))}
            </ul>
          )}
        </div>
      </aside>
    </>
  )
}
