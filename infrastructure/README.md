# Infrastructure

This folder contains all infrastructure configuration for **my-own-chatbot-ai**.

The current runtime supports:
- anonymous single-chat usage with no Clerk configuration
- authenticated saved multi-conversation flows when Clerk runtime variables are supplied
- SQLite-backed durable conversation storage for authenticated users

_Last updated: 2026-04-11_

---

## Table of Contents

1. [Quick Start](#quick-start)
2. [Architecture Overview](#architecture-overview)
3. [Folder Structure](#folder-structure)
4. [Prerequisites](#prerequisites)
5. [Docker — Standalone Containers](#docker--standalone-containers)
6. [Docker Compose — Full Stack](#docker-compose--full-stack)
7. [Kubernetes](#kubernetes)
8. [Environment Variables](#environment-variables)
9. [Networking](#networking)
10. [Storage](#storage)
11. [GPU Support (Ollama)](#gpu-support-ollama)
12. [Operational Validation & Rollback](#operational-validation--rollback)
13. [Adding a New Service](#adding-a-new-service)

---

## Quick Start

> **All commands below assume the repo root as the working directory** unless stated otherwise.

### Run everything with Docker Compose

```bash
# Optional Clerk configuration for production-like containers
# Only variable names are documented here. Do not commit Clerk secrets or real values.
export CLERK_PUBLISHABLE_KEY=<your-clerk-publishable-key>
# Configure the backend Clerk public verification key when authenticated
# saved conversations are enabled.
export CLERK__JWT_VERIFICATION_PUBLIC_KEY=<your-clerk-jwt-verification-public-key>

# 1. Build all images and start all services (production-like)
cd infrastructure
docker compose -f docker-compose.yml up --build

# 2. Or run in the background
cd infrastructure
docker compose -f docker-compose.yml up --build -d

# 3. Tail logs
docker compose logs -f

# 4. Stop everything
docker compose down
```

The backend stores authenticated users' saved conversations in SQLite at `/app/App_Data/conversations.sqlite` inside the container. Docker Compose persists that path through the named volume `chatbot-backend-data`.

| Service  | URL                           |
|----------|-------------------------------|
| Frontend | http://localhost:3000         |
| Backend  | http://localhost:5050         |
| Ollama   | http://localhost:11434        |

### Run with hot reload (local development)

```bash
export VITE_CLERK_PUBLISHABLE_KEY=<your-clerk-publishable-key>
# Configure the backend Clerk public verification key when authenticated
# saved conversations are enabled.
export CLERK__JWT_VERIFICATION_PUBLIC_KEY=<your-clerk-jwt-verification-public-key>

cd infrastructure
docker compose up --build   # override file is merged automatically
```

| Service          | URL                           |
|------------------|-------------------------------|
| Frontend (HMR)   | http://localhost:5173         |
| Backend (watch)  | http://localhost:5050         |
| Ollama           | http://localhost:11434        |

### Run a single service standalone

```bash
# Backend
docker build -f infrastructure/docker/backend/Dockerfile -t chatbot-ai/backend .
docker run --rm -p 5050:5050 \
  -e ConversationPersistence__DatabasePath=/app/App_Data/conversations.sqlite \
  -e Clerk__JwtVerificationPublicKey=<your-clerk-jwt-verification-public-key> \
  -v chatbot-backend-data:/app/App_Data \
  chatbot-ai/backend

# Frontend
docker build -f infrastructure/docker/frontend/Dockerfile -t chatbot-ai/frontend .
docker run --rm -p 3000:80 \
  -e CLERK_PUBLISHABLE_KEY=<your-clerk-publishable-key> \
  chatbot-ai/frontend

# Ollama
docker build -f infrastructure/docker/ollama/Dockerfile -t chatbot-ai/ollama .
docker run --rm -p 11434:11434 -v ollama_data:/root/.ollama chatbot-ai/ollama
```

### Deploy to Kubernetes

```bash
# Apply namespace first, then all manifests
kubectl apply -f infrastructure/kubernetes/namespace.yaml

# Optional Clerk runtime config. Apply your real values from CI/CD or cluster
# management tooling; do not commit them to git-managed manifests.
# This ConfigMap only carries non-secret Clerk inputs plus the frontend
# publishable key used by the browser shell plus the backend public
# verification key for bearer-token validation.
kubectl -n chatbot-ai create configmap clerk-config \
  --from-literal=CLERK_PUBLISHABLE_KEY=<your-clerk-publishable-key> \
  --from-literal=Clerk__JwtVerificationPublicKey=<your-clerk-jwt-verification-public-key> \
  --dry-run=client -o yaml | kubectl apply -f -

kubectl apply -R -f infrastructure/kubernetes/

# Watch rollout
kubectl -n chatbot-ai rollout status deployment/backend
kubectl -n chatbot-ai rollout status deployment/frontend
kubectl -n chatbot-ai rollout status deployment/ollama

# Port-forward for local access (kind / minikube)
kubectl -n chatbot-ai port-forward svc/frontend 3000:80
kubectl -n chatbot-ai port-forward svc/backend  5050:5050
kubectl -n chatbot-ai port-forward svc/ollama   11434:11434
```

---

## Architecture Overview

The application is composed of three services:

| Service      | Technology            | Default Port | Role                             |
|--------------|-----------------------|:------------:|----------------------------------|
| **backend**  | .NET 8 Minimal API    | `5050`       | Chat API, business logic, Orleans grain host (planned) |
| **frontend** | Vite + React + nginx  | `3000` (Docker) / `80` (k8s) | SPA served by nginx, proxies `/api/` to backend |
| **ollama**   | Ollama                | `11434`      | Local LLM inference              |

```
Browser
  │
  ▼
frontend (nginx :80)
  │ proxies /api/* ──────────────────────────────────▶ backend (:5050)
  │                                                          │
  │                                                          │ HTTP
  │                                                          ▼
  │                                                    ollama (:11434)
```

---

## Folder Structure

```
infrastructure/
├── README.md                          ← this file
├── docker-compose.yml                 ← production-like full-stack compose
├── docker-compose.override.yml        ← local dev overrides (hot reload)
├── docker/
│   ├── backend/
│   │   └── Dockerfile                 ← multi-stage .NET 8 build → aspnet runtime
│   ├── frontend/
│   │   ├── Dockerfile                 ← multi-stage node build → nginx serve
│   │   ├── Dockerfile.dev             ← Vite dev server (used by override)
│   │   ├── 40-write-app-config.sh     ← writes runtime Clerk config for nginx-served SPA
│   │   └── nginx.conf                 ← nginx config: SPA fallback + API proxy
│   └── ollama/
│       ├── Dockerfile                 ← extends ollama/ollama with model pull script
│       └── entrypoint.sh              ← starts Ollama, waits for readiness, pulls model
└── kubernetes/
    ├── namespace.yaml                 ← chatbot-ai namespace
    ├── backend/
    │   ├── configmap.yaml             ← env vars (ASPNETCORE_ENVIRONMENT, Ollama__BaseUrl)
    │   ├── deployment.yaml            ← backend Deployment
    │   ├── persistentvolumeclaim.yaml ← 1 Gi PVC for SQLite conversation storage
    │   └── service.yaml               ← ClusterIP service on port 5050
    ├── frontend/
    │   ├── configmap.yaml             ← nginx.conf injected as a ConfigMap
    │   ├── deployment.yaml            ← frontend Deployment (nginx)
    │   └── service.yaml               ← LoadBalancer service on port 80
    └── ollama/
        ├── persistentvolumeclaim.yaml ← 20 Gi PVC for model storage
        ├── deployment.yaml            ← Ollama Deployment with init container model pull
        └── service.yaml               ← ClusterIP service on port 11434
```

---

## Prerequisites

### Docker / Docker Compose

| Tool           | Minimum version |
|----------------|----------------|
| Docker Engine  | 24+            |
| Docker Compose | v2 (plugin)    |

### Kubernetes

| Tool      | Notes                                            |
|-----------|--------------------------------------------------|
| kubectl   | Configured to point at your target cluster       |
| Cluster   | kind / minikube (local) or any cloud k8s cluster |

### Source Code

All Dockerfiles use **`..` (the repo root) as the build context** so they can reference both the `frontend/` and `backend/` source trees. Always run `docker build` or `docker compose` commands from the `infrastructure/` directory, or use the compose file flags documented below.

---

## Docker — Standalone Containers

Each service can be built and run independently for rapid iteration.

### Backend (standalone)

```bash
# From repo root
docker build \
  -f infrastructure/docker/backend/Dockerfile \
  -t chatbot-ai/backend \
  .

docker run --rm -p 5050:5050 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e ConversationPersistence__DatabasePath=/app/App_Data/conversations.sqlite \
  -e Clerk__JwtVerificationPublicKey=<your-clerk-jwt-verification-public-key> \
  -v chatbot-backend-data:/app/App_Data \
  chatbot-ai/backend
```

Verify: `curl http://localhost:5050/`

### Frontend (standalone)

```bash
# From repo root
docker build \
  -f infrastructure/docker/frontend/Dockerfile \
  -t chatbot-ai/frontend \
  .

docker run --rm -p 3000:80 \
  -e CLERK_PUBLISHABLE_KEY=<your-clerk-publishable-key> \
  chatbot-ai/frontend
```

Verify: open `http://localhost:3000` in a browser.

> **Note:** In standalone mode the nginx `/api/` proxy will fail because no backend is running.
> Use Docker Compose to run all services together.

### Ollama (standalone)

```bash
# From repo root — default model: llama3.2
docker build \
  -f infrastructure/docker/ollama/Dockerfile \
  -t chatbot-ai/ollama \
  .

# With a different model
docker build \
  --build-arg DEFAULT_MODEL=mistral \
  -f infrastructure/docker/ollama/Dockerfile \
  -t chatbot-ai/ollama \
  .

docker run --rm -p 11434:11434 \
  -v ollama_data:/root/.ollama \
  chatbot-ai/ollama
```

Verify: `curl http://localhost:11434/api/tags`

---

## Docker Compose — Full Stack

### Production-like (all services)

```bash
cd infrastructure

# Optional Clerk configuration for the nginx-served frontend and backend token validation
export CLERK_PUBLISHABLE_KEY=<your-clerk-publishable-key>
export CLERK__JWT_VERIFICATION_PUBLIC_KEY=<your-clerk-jwt-verification-public-key>

# Build and start all services
docker compose up --build

# Run in background
docker compose up --build -d

# Tail logs
docker compose logs -f

# Stop everything
docker compose down
```

After startup:
- Frontend: `http://localhost:3000`
- Backend API: `http://localhost:5050`
- Ollama API: `http://localhost:11434`

### Local Development (hot reload)

The `docker-compose.override.yml` file is automatically merged when you run `docker compose` from the `infrastructure/` directory. It replaces the nginx frontend with a Vite dev server and enables `dotnet watch` on the backend.

```bash
export VITE_CLERK_PUBLISHABLE_KEY=<your-clerk-publishable-key>
export CLERK__JWT_VERIFICATION_PUBLIC_KEY=<your-clerk-jwt-verification-public-key>

cd infrastructure
docker compose up --build
```

Development URLs:
- Frontend (Vite + HMR): `http://localhost:5173`
- Backend (dotnet watch): `http://localhost:5050`
- Ollama: `http://localhost:11434`

To run **only production images** (ignoring the override):

```bash
cd infrastructure
docker compose -f docker-compose.yml up --build
```

---

## Kubernetes

All manifests live in `infrastructure/kubernetes/` and target the `chatbot-ai` namespace.

### Apply all manifests

```bash
# Create namespace first
kubectl apply -f infrastructure/kubernetes/namespace.yaml

# Optional Clerk runtime config shared by backend and frontend
# Supply the frontend publishable key and backend public verification key.
kubectl -n chatbot-ai create configmap clerk-config \
  --from-literal=CLERK_PUBLISHABLE_KEY=<your-clerk-publishable-key> \
  --from-literal=Clerk__JwtVerificationPublicKey=<your-clerk-jwt-verification-public-key> \
  --dry-run=client -o yaml | kubectl apply -f -

# Apply all resources (backend, frontend, ollama)
kubectl apply -R -f infrastructure/kubernetes/

# Check rollout status
kubectl -n chatbot-ai rollout status deployment/backend
kubectl -n chatbot-ai rollout status deployment/frontend
kubectl -n chatbot-ai rollout status deployment/ollama
```

### Verify services

```bash
kubectl -n chatbot-ai get pods
kubectl -n chatbot-ai get services
```

### Access the application

```bash
# If using a LoadBalancer (cloud cluster)
kubectl -n chatbot-ai get service frontend
# Use the EXTERNAL-IP shown

# If using minikube
minikube service frontend -n chatbot-ai

# If using kind with port-forwarding
kubectl -n chatbot-ai port-forward svc/frontend 3000:80
kubectl -n chatbot-ai port-forward svc/backend 5050:5050
kubectl -n chatbot-ai port-forward svc/ollama 11434:11434
```

### Update a deployment image

```bash
# After pushing a new image to a registry:
kubectl -n chatbot-ai set image deployment/backend backend=ghcr.io/<your-org>/chatbot-ai-backend:v2
kubectl -n chatbot-ai rollout status deployment/backend
```

### Tear down

```bash
# Remove all resources in the namespace
kubectl delete namespace chatbot-ai

# Or remove selectively
kubectl delete -R -f infrastructure/kubernetes/
```

---

## Environment Variables

### Backend

| Variable                  | Default               | Description                        |
|---------------------------|-----------------------|------------------------------------|
| `ASPNETCORE_ENVIRONMENT`  | `Production`          | `Development` or `Production`      |
| `ASPNETCORE_URLS`         | `http://+:5050`       | Kestrel listen address             |
| `Ollama__BaseUrl`         | `http://ollama:11434` | Ollama service URL                 |
| `ConversationPersistence__DatabasePath` | `/app/App_Data/conversations.sqlite` | Absolute SQLite file path for durable saved conversations |
| `Clerk__JwtVerificationPublicKey` | _unset_       | Optional explicit Clerk public verification key used by the backend to validate Clerk bearer tokens |

### Frontend

| Variable                       | Default | Description |
|--------------------------------|---------|-------------|
| `CLERK_PUBLISHABLE_KEY`        | _unset_ | Runtime Clerk publishable key for the nginx-served frontend |
| `VITE_CLERK_PUBLISHABLE_KEY`   | _unset_ | Clerk publishable key exposed to the Vite dev server |

The current infrastructure does **not** require a Clerk secret key. The frontend only needs the publishable key, while the backend validates Clerk bearer tokens using the explicit `Clerk__JwtVerificationPublicKey` input. The backend validates the token signature and lifetime, then extracts the current user id from the validated `sub` claim. Keep all real values out of git-managed manifests and inject them from your shell, CI/CD, or cluster tooling.

### Ollama (Docker only)

| Variable        | Default    | Description                              |
|-----------------|------------|------------------------------------------|
| `OLLAMA_HOST`   | `0.0.0.0`  | Interface to bind Ollama server          |
| `DEFAULT_MODEL` | `llama3.2` | Model pulled at container startup        |

---

## Networking

### Docker Compose

Docker Compose creates a default bridge network named `chatbot-ai_default`. Services reference each other by service name (e.g. `backend` → `http://backend:5050`, `ollama` → `http://ollama:11434`).

The production-like frontend image writes `/app-config.js` from `CLERK_PUBLISHABLE_KEY` at container startup and nginx injects that script into `index.html`, so you can change Clerk environments without rebuilding the SPA image.

### Kubernetes

All services are in the `chatbot-ai` namespace. DNS resolution follows the pattern:

```
<service-name>.<namespace>.svc.cluster.local
# Shortened form used in configs:
<service-name>   (within the same namespace)
```

Example: `http://ollama:11434` in the backend configmap resolves to `ollama.chatbot-ai.svc.cluster.local:11434`.

When Clerk is enabled in Kubernetes, both the backend and frontend deployments optionally read keys from a namespace-local `clerk-config` ConfigMap. Populate that object from your deployment pipeline or cluster tooling instead of committing real values to the repo. `CLERK_PUBLISHABLE_KEY` and `Clerk__JwtVerificationPublicKey` are the only Clerk inputs this infrastructure keeps documented for the public-key-only validation flow, and the repo documents variable names only. Keep true secrets out of this ConfigMap.

---

## Storage

### Backend conversation persistence (SQLite)

Authenticated multi-conversation history is stored in SQLite. The backend is configured to use:

- **Container path**: `/app/App_Data/conversations.sqlite`
- **Docker Compose**: named volume `chatbot-backend-data` mounted at `/app/App_Data`
- **Kubernetes**: `PersistentVolumeClaim` `backend-conversations-pvc` mounted at `/app/App_Data`

Because the SQLite database lives on a single `ReadWriteOnce` volume, the Kubernetes backend deployment stays at **1 replica** and uses the **`Recreate`** rollout strategy to avoid two pods trying to mount or write the same database volume during an update.

SQLite is a file-backed local dependency only. Do **not** treat it as a secret and do **not** add credentials for it; only the durable file path and volume mount need to be configured.

### Ollama model storage

Ollama stores downloaded model weights in `/root/.ollama`. This is backed by:

- **Docker Compose**: a named volume `chatbot-ollama-data` on the host.
- **Kubernetes**: a `PersistentVolumeClaim` (`ollama-models-pvc`) requesting 20 Gi.

Adjust the PVC size in `kubernetes/ollama/persistentvolumeclaim.yaml` based on the models you plan to run:

| Model           | Approx. size |
|-----------------|:------------:|
| llama3.2 (3B)   | ~2 GB        |
| mistral (7B)    | ~4 GB        |
| llama3 (8B)     | ~5 GB        |
| llama3 (70B)    | ~40 GB       |

---

## GPU Support (Ollama)

### Docker Compose

```bash
# After installing the NVIDIA Container Toolkit:
docker run --gpus all --rm -p 11434:11434 \
  -v ollama_data:/root/.ollama \
  chatbot-ai/ollama
```

To enable GPUs in Compose, add a `deploy` section to the `ollama` service in `docker-compose.yml`:

```yaml
ollama:
  deploy:
    resources:
      reservations:
        devices:
          - driver: nvidia
            count: 1
            capabilities: [gpu]
```

### Kubernetes

Uncomment the `nodeSelector`, `tolerations`, and `nvidia.com/gpu` resource limit sections in `kubernetes/ollama/deployment.yaml`. Requires the [NVIDIA GPU Operator](https://docs.nvidia.com/datacenter/cloud-native/gpu-operator/overview.html) to be installed in your cluster.

---

## Operational Validation & Rollback

Use these checks whenever infrastructure behavior changes.

### Docker and Compose

```bash
# Validate compose shape before deploying
cd infrastructure
docker compose -f docker-compose.yml config

# Rebuild a single image from repo root
docker build -f infrastructure/docker/backend/Dockerfile -t chatbot-ai/backend:test .
docker build -f infrastructure/docker/frontend/Dockerfile -t chatbot-ai/frontend:test .
docker build -f infrastructure/docker/ollama/Dockerfile -t chatbot-ai/ollama:test .
```

### Kubernetes validation

```bash
# Client-side validation
kubectl apply --dry-run=client -f infrastructure/kubernetes/backend/
kubectl apply --dry-run=client -f infrastructure/kubernetes/frontend/
kubectl apply --dry-run=client -f infrastructure/kubernetes/ollama/

# Prefer server-side validation too when the cluster is reachable
kubectl apply --dry-run=server -f infrastructure/kubernetes/backend/
kubectl apply --dry-run=server -f infrastructure/kubernetes/frontend/
kubectl apply --dry-run=server -f infrastructure/kubernetes/ollama/
```

### Rollout and rollback

```bash
# Watch the active rollout
kubectl -n chatbot-ai rollout status deployment/backend
kubectl -n chatbot-ai rollout status deployment/frontend
kubectl -n chatbot-ai rollout status deployment/ollama

# Roll back the last deployment if needed
kubectl -n chatbot-ai rollout undo deployment/backend
kubectl -n chatbot-ai rollout undo deployment/frontend
kubectl -n chatbot-ai rollout undo deployment/ollama
```

### Security defaults to preserve

- Keep non-secret configuration in ConfigMaps and actual secrets out of git.
- Prefer `runAsNonRoot: true`, `allowPrivilegeEscalation: false`, and `seccompProfile.type: RuntimeDefault` when the base image supports them.
- Preserve resource requests/limits and health probes when changing Deployments.
- Add a startup probe when a service has a slower boot path than its normal readiness/liveness window.

---

## Adding a New Service

1. Create `infrastructure/docker/<service-name>/Dockerfile`.
2. Add the service to `infrastructure/docker-compose.yml` and `docker-compose.override.yml` if it needs dev-mode overrides.
3. Create the Kubernetes manifests under `infrastructure/kubernetes/<service-name>/`:
   - `deployment.yaml`
   - `service.yaml`
   - `configmap.yaml` or `persistentvolumeclaim.yaml` as needed.
4. Update this README with ports, environment variables, and networking notes.
5. Update `.github/instructions/infrastructure.instructions.md` with any new conventions.
