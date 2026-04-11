import { useEffect, useMemo, useState } from 'react'
import { useChatStore } from '../store/chatStore'
import { ConversationHeader } from './ConversationHeader'
import { ConversationSidebar } from './ConversationSidebar'
import { MessageComposer } from './MessageComposer'
import { MessageList } from './MessageList'

interface ChatLayoutProps {
  multiConversationEnabled: boolean
}

function ErrorBanner({
  message,
  onDismiss,
}: {
  message: string
  onDismiss: () => void
}) {
  return (
    <div
      role="alert"
      className="mx-4 mb-2 flex items-start gap-3 rounded-xl border border-red-800 bg-red-950/60 px-4 py-3 text-sm text-red-200 shrink-0"
    >
      <span className="flex-1">{message}</span>
      <button
        type="button"
        onClick={onDismiss}
        className="mt-0.5 leading-none text-red-300 transition-colors hover:text-white"
        aria-label="Dismiss error"
      >
        ✕
      </button>
    </div>
  )
}

export function ChatLayout({ multiConversationEnabled }: ChatLayoutProps) {
  const [isSidebarOpen, setIsSidebarOpen] = useState(false)

  const conversations = useChatStore((state) => state.conversations)
  const activeConversation = useChatStore((state) => state.activeConversation)
  const activeConversationId = useChatStore((state) => state.activeConversationId)
  const messages = useChatStore((state) => state.messages)
  const sendStatus = useChatStore((state) => state.sendStatus)
  const errorMessage = useChatStore((state) => state.errorMessage)
  const sidebarStatus = useChatStore((state) => state.sidebarStatus)
  const sidebarErrorMessage = useChatStore((state) => state.sidebarErrorMessage)
  const historyStatus = useChatStore((state) => state.historyStatus)
  const historyErrorMessage = useChatStore((state) => state.historyErrorMessage)
  const initializeChat = useChatStore((state) => state.initializeChat)
  const loadConversations = useChatStore((state) => state.loadConversations)
  const activateConversation = useChatStore((state) => state.activateConversation)
  const startNewConversation = useChatStore((state) => state.startNewConversation)
  const renameConversation = useChatStore((state) => state.renameConversation)
  const deleteConversation = useChatStore((state) => state.deleteConversation)
  const sendMessage = useChatStore((state) => state.sendMessage)
  const clearError = useChatStore((state) => state.clearError)
  const clearSidebarError = useChatStore((state) => state.clearSidebarError)
  const clearHistoryError = useChatStore((state) => state.clearHistoryError)

  useEffect(() => {
    const mode = multiConversationEnabled ? 'authenticated' : 'anonymous'
    initializeChat(mode)

    if (multiConversationEnabled) {
      void loadConversations()
    }
  }, [initializeChat, loadConversations, multiConversationEnabled])

  const isBusySending = sendStatus === 'sending' || sendStatus === 'streaming'
  const isLoadingHistory = historyStatus === 'loading'

  const emptyState = useMemo(() => {
    if (!multiConversationEnabled) {
      return {
        title: 'Start a conversation',
        description: 'Type a message below to begin chatting with your local AI.',
      }
    }

    return activeConversationId
      ? {
          title: 'No messages yet',
          description: 'Send a message to continue this saved conversation.',
        }
      : {
          title: 'Start a saved conversation',
          description: 'Choose a conversation from the sidebar or send a message to create a new one.',
        }
  }, [activeConversationId, multiConversationEnabled])

  const headerTitle = activeConversation?.title ?? 'New conversation'
  const headerSubtitle = multiConversationEnabled
    ? activeConversationId
      ? 'Saved conversation history is loaded from your account.'
      : 'Your next message will create a fresh saved conversation.'
    : 'Anonymous single-chat stays available without signing in.'

  const mainPanel = (
    <div className="flex min-h-0 min-w-0 flex-1 flex-col bg-gray-950 text-gray-100">
      <ConversationHeader
        title={headerTitle}
        subtitle={headerSubtitle}
        showSidebarToggle={multiConversationEnabled}
        onToggleSidebar={() => setIsSidebarOpen(true)}
        onNewConversation={multiConversationEnabled ? startNewConversation : undefined}
      />

      <MessageList
        messages={messages}
        sendStatus={sendStatus}
        isLoadingHistory={isLoadingHistory}
        emptyStateTitle={emptyState.title}
        emptyStateDescription={emptyState.description}
      />

      {historyStatus === 'error' && historyErrorMessage ? (
        <ErrorBanner message={historyErrorMessage} onDismiss={clearHistoryError} />
      ) : null}

      {sendStatus === 'error' && errorMessage ? (
        <ErrorBanner message={errorMessage} onDismiss={clearError} />
      ) : null}

      <MessageComposer
        onSend={sendMessage}
        sendStatus={isLoadingHistory ? 'sending' : isBusySending ? sendStatus : 'idle'}
      />
    </div>
  )

  if (!multiConversationEnabled) {
    return mainPanel
  }

  return (
    <div className="flex min-h-0 flex-1 overflow-hidden bg-gray-950 text-gray-100">
      <ConversationSidebar
        conversations={conversations}
        activeConversationId={activeConversationId}
        isOpen={isSidebarOpen}
        status={sidebarStatus}
        errorMessage={sidebarErrorMessage}
        onClose={() => setIsSidebarOpen(false)}
        onSelectConversation={(conversationId) => {
          setIsSidebarOpen(false)
          void activateConversation(conversationId)
        }}
        onNewConversation={() => {
          startNewConversation()
          setIsSidebarOpen(false)
        }}
        onRenameConversation={renameConversation}
        onDeleteConversation={deleteConversation}
        onDismissError={clearSidebarError}
      />

      {mainPanel}
    </div>
  )
}
