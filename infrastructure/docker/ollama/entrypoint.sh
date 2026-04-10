#!/bin/sh
# Start Ollama server in the background, wait for it to be ready,
# then optionally pull the default model.

set -e

echo "[ollama] Starting Ollama server..."
ollama serve &
OLLAMA_PID=$!

# Wait until the Ollama API is responsive
echo "[ollama] Waiting for Ollama API to be ready..."
until curl -sf http://localhost:11434/api/tags > /dev/null 2>&1; do
  sleep 1
done
echo "[ollama] Ollama is ready."

# Pull default model if specified and not already present
if [ -n "${DEFAULT_MODEL}" ]; then
  if ollama list | grep -q "^${DEFAULT_MODEL}"; then
    echo "[ollama] Model '${DEFAULT_MODEL}' already present, skipping pull."
  else
    echo "[ollama] Pulling model '${DEFAULT_MODEL}'..."
    ollama pull "${DEFAULT_MODEL}"
    echo "[ollama] Model '${DEFAULT_MODEL}' pulled successfully."
  fi
fi

# Keep the server in foreground
wait $OLLAMA_PID
