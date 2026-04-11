interface ConversationHeaderProps {
  title: string
  subtitle: string
  showSidebarToggle?: boolean
  onToggleSidebar?: () => void
  onNewConversation?: () => void
}

export function ConversationHeader({
  title,
  subtitle,
  showSidebarToggle = false,
  onToggleSidebar,
  onNewConversation,
}: ConversationHeaderProps) {
  return (
    <header className="flex items-center justify-between gap-3 border-b border-gray-800 bg-gray-950 px-4 py-3 shrink-0">
      <div className="flex min-w-0 items-center gap-3">
        {showSidebarToggle && onToggleSidebar ? (
          <button
            type="button"
            onClick={onToggleSidebar}
            className="rounded-lg border border-gray-700 px-3 py-2 text-sm text-gray-100 transition hover:border-gray-500 hover:bg-gray-800 md:hidden"
            aria-label="Open saved conversations"
          >
            ☰
          </button>
        ) : null}

        <div className="min-w-0">
          <h1 className="truncate text-sm font-semibold text-gray-100">{title}</h1>
          <p className="truncate text-xs text-gray-400">{subtitle}</p>
        </div>
      </div>

      {onNewConversation ? (
        <button
          type="button"
          onClick={onNewConversation}
          className="hidden rounded-lg border border-gray-700 px-3 py-2 text-sm font-medium text-gray-100 transition hover:border-gray-500 hover:bg-gray-800 md:inline-flex"
        >
          New conversation
        </button>
      ) : null}
    </header>
  )
}
