---
description: "Use when setting up the local development environment, troubleshooting tool versions, or verifying prerequisites for the chatbot app."
name: "Dev Setup Guidelines"
applyTo: "README.md, .github/**"
---
# Dev Setup Guidelines

> **For interactive setup and verification, use the `setup-local-env` skill** — it checks all prerequisites, verifies builds, and reports what needs fixing.

## General Reminders

- **Do NOT assume** tools, folders, or scripts exist — verify with actual commands first.
- Only suggest commands that have been confirmed to work in this repo.
- Keep `README.md` and other docs in sync when verified commands change.
- Prefer **nvm** or the official Node.js installer over Homebrew-managed Node on macOS (ICU errors).
- The backend expects Ollama at `http://localhost:11434` by default.

