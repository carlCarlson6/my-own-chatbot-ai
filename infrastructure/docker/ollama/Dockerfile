# Ollama image — extends the official image with an optional startup model pull.
#
# Build args:
#   DEFAULT_MODEL  — model to pull on first start (default: mistral)
#                    Set to empty string "" to skip the pull.
#
# Usage (standalone — run from repo root):
#   docker build -f infrastructure/docker/ollama/Dockerfile -t chatbot-ollama .
#   docker run -p 11434:11434 -v ollama_data:/root/.ollama chatbot-ollama
#
# Usage with a specific model (run from repo root):
#   docker build -f infrastructure/docker/ollama/Dockerfile --build-arg DEFAULT_MODEL=mistral -t chatbot-ollama .

FROM ollama/ollama:latest

ARG DEFAULT_MODEL=mistral

# Copy entrypoint script that starts Ollama and optionally pulls the default model
COPY infrastructure/docker/ollama/entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

ENV OLLAMA_HOST=0.0.0.0
ENV DEFAULT_MODEL=${DEFAULT_MODEL}

EXPOSE 11434

VOLUME ["/root/.ollama"]

ENTRYPOINT ["/entrypoint.sh"]
