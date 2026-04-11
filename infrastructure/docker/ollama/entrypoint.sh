#!/bin/sh
# Start Ollama server in the background, wait for it to be ready,
# then optionally pull the default model.

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
echo "[ollama] DEFAULT_MODEL='${DEFAULT_MODEL}'"

if [ -z "${DEFAULT_MODEL}" ]; then
  echo "[ollama] No DEFAULT_MODEL set — skipping model pull."
else
  INSTALLED=$(ollama list 2>/dev/null | tail -n +2 | awk '{print $1}')
  if echo "${INSTALLED}" | grep -q "^${DEFAULT_MODEL}"; then
    echo "[ollama] Model '${DEFAULT_MODEL}' already present, skipping pull."
  else
    echo "[ollama] Pulling model '${DEFAULT_MODEL}'..."
    if ollama pull "${DEFAULT_MODEL}"; then
      echo "[ollama] Model '${DEFAULT_MODEL}' pulled successfully."
    else
      echo "[ollama] WARNING: Failed to pull model '${DEFAULT_MODEL}'. Server will still start."
    fi
  fi
fi

# Keep the server in foreground
wait $OLLAMA_PID
