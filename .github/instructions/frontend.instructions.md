---
description: "Use when working on the frontend chat app with React, Vite, TypeScript, Tailwind CSS, Zustand, and Zod. Covers component structure, UI state, API validation, and type/build checks."
name: "Frontend Chatbot Guidelines"
applyTo: "**/*.{ts,tsx,css}"
---
# Frontend Chatbot Guidelines

## Stack

- Use **React + Vite + TypeScript** for all frontend code.
- Prefer small, composable function components and typed hooks.
- Keep frontend concerns separate from backend or Orleans-specific logic.

## State and Data Flow

- Use **Zustand** for shared app state such as chat session state, current conversation, streaming status, and UI preferences.
- Keep transient component-only state local with `useState`; do not put every UI flag into the global store.
- Keep API calls, schema validation, and store updates clearly separated.

## Validation and Contracts

- Use **Zod** for API contracts and runtime validation at the UI/API boundary.
- Parse server responses before storing or rendering them.
- Prefer explicit frontend domain types such as `ChatMessage`, `ChatRequest`, and `ChatResponse`.

## UI Styling

- Use **Tailwind CSS** for styling.
- Prefer utility classes and reusable UI primitives over large custom CSS files.
- Keep the chat UI clean, responsive, and accessibility-aware.

## Chat App Patterns

- Separate presentational components from chat orchestration logic.
- Keep message list, composer, model controls, and status indicators as distinct components.
- Favor optimistic but safe UI updates, with clear loading and error states.

## AI Interaction UX

- Design the chat experience for **streaming assistant responses**, partial message updates, and cancellation.
- Keep clear UI states such as `idle`, `sending`, `streaming`, and `error`.
- Render assistant output safely, especially for markdown, code blocks, and copy/retry/regenerate actions.
- Keep model selection, generation settings, and conversation history organized so the UI remains predictable.

## Validation Before Completion

- After frontend changes, run **type** and **build** validation when the frontend scripts exist.
- Prefer the repo's verified commands, such as `npm run typecheck` and `npm run build` or the equivalent package-manager scripts.
- Do not report success unless those checks have been run and their output confirms success.

## Repo Awareness

- This repository is still an early scaffold. Verify the actual frontend structure and scripts before adding or using commands.
- Keep `README.md` aligned with any verified frontend setup and commands you introduce.
