---
description: >
  Use when working on infrastructure for this project: Dockerfiles, Docker Compose,
  Kubernetes manifests, nginx configuration, or any file under infrastructure/.
  Covers conventions, folder layout, build context rules, naming, and review gates.
name: "Infrastructure Guidelines"
applyTo: "infrastructure/**"
---

# Infrastructure Guidelines

## Canonical Location

- All infrastructure configuration lives under `infrastructure/`.
- Do **not** place Dockerfiles, Compose files, or Kubernetes manifests elsewhere in the repo.
- Keep `infrastructure/README.md` updated whenever you add or change a service, port, volume, or environment variable.

## Services

The project has three services. Keep their identities stable:

| Service    | Image name prefix      | Internal port | DNS name (Compose / k8s) |
|------------|------------------------|:-------------:|--------------------------|
| `backend`  | `chatbot-ai/backend`   | `5050`        | `backend`                |
| `frontend` | `chatbot-ai/frontend`  | `80`          | `frontend`               |
| `ollama`   | `chatbot-ai/ollama`    | `11434`       | `ollama`                 |

## Dockerfile Conventions

- Use **multi-stage builds** for backend and frontend to keep runtime images small.
- Use specific base image tags (e.g. `node:22-alpine`, `mcr.microsoft.com/dotnet/sdk:8.0`) — never `latest` for base images in production Dockerfiles.
- Exception: `ollama/ollama:latest` is acceptable because Ollama does not publish versioned SemVer tags.
- The **build context for all Dockerfiles is the repo root (`..` from `infrastructure/`)** — this lets Dockerfiles copy from both `frontend/` and `backend/` in one build.
- Run containers as a **non-root user** where the base image permits.

## Docker Compose Conventions

- `docker-compose.yml` is the **production-like** configuration.
- `docker-compose.override.yml` holds **local development** overrides: hot reload, source mounts, dev servers.
- The override file is automatically merged by Docker Compose when both files are in the same directory.
- Use **named volumes** for any persistent data (e.g. `chatbot-ollama-data`).
- Use `depends_on` with `condition: service_healthy` to order startup correctly.
- Define `healthcheck` for every service so dependents can wait for readiness.

## Kubernetes Conventions

- All resources must declare `namespace: chatbot-ai`.
- All resources must carry the labels `app: <service>` and `project: my-own-chatbot-ai`.
- Apply the namespace manifest first: `kubectl apply -f infrastructure/kubernetes/namespace.yaml`.
- Use `Recreate` strategy for Ollama (only one pod may claim the `ReadWriteOnce` PVC at a time).
- Use `ClusterIP` for internal services (backend, ollama); use `LoadBalancer` for the public-facing frontend.
- Keep resource `requests` and `limits` defined on every container.
- Secrets (API keys, tokens) must **never** be committed to the repo. Use Kubernetes `Secret` objects or an external secrets manager and document the variable names without values.

## Environment Variables

- Declare environment variables in `kubernetes/<service>/configmap.yaml` for non-sensitive config.
- Mirror the same variable names in `docker-compose.yml` under the `environment:` key.
- Document every variable in `infrastructure/README.md`.

## Networking Rules

- Services talk to each other using the **service DNS name as the hostname** (e.g. `http://ollama:11434`).
- The frontend nginx always proxies `/api/` to `http://backend:5050/`.
- Only the frontend service should be publicly accessible; backend and ollama are internal.

## Adding a New Service

1. Create `infrastructure/docker/<service>/Dockerfile`.
2. Add the service to `docker-compose.yml` (and `docker-compose.override.yml` if applicable).
3. Create `infrastructure/kubernetes/<service>/` with at minimum `deployment.yaml` and `service.yaml`.
4. Update `infrastructure/README.md` — ports, env vars, networking, and storage sections.
5. Update this instructions file with the new service's conventions.

## Pre-Implementation Change Review

Before making changes to any infrastructure file, inspect the current state:

1. Read `infrastructure/README.md` and the relevant manifests/Dockerfiles.
2. Check that ports, image names, and DNS references are still consistent.
3. Verify the build context assumption still holds (repo root).
4. If the plan or docs are stale, update them before changing files.

## Verification

- After changing a Dockerfile, verify the image builds successfully:
  ```bash
  docker build -f infrastructure/docker/<service>/Dockerfile -t chatbot-ai/<service>:test .
  ```
- After changing Kubernetes manifests, validate them:
  ```bash
  kubectl apply --dry-run=client -f infrastructure/kubernetes/<service>/
  ```
- After changing `docker-compose.yml`, verify the config parses:
  ```bash
  docker compose -f infrastructure/docker-compose.yml config
  ```
