# Mac Quickstart Guide — my-own-chatbot-ai

Get the chatbot running on your Mac in minutes. Docker Compose is the recommended path — no SDK or Node needed.

---

## Table of Contents

1. [Prerequisites](#1-prerequisites)
2. [Clone the repo](#2-clone-the-repo)
3. [Run with Docker Compose (Recommended)](#3-run-with-docker-compose-recommended)
4. [Port reference](#4-port-reference)
5. [Run on Kubernetes (Local)](#5-run-on-kubernetes-local)
6. [Run locally without Docker (Optional)](#6-run-locally-without-docker-optional)
7. [Verify everything is running](#7-verify-everything-is-running)
8. [One-time setup notes](#8-one-time-setup-notes)
9. [Useful commands reference](#9-useful-commands-reference)

---

## 1. Prerequisites

Install [Homebrew](https://brew.sh) first if you don't have it:

```bash
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
```

### Tool table

| Tool | Min version | Homebrew install | When you need it |
|------|-------------|-----------------|------------------|
| **Docker Desktop** | 24+ | `brew install --cask docker` | Everything — required for Docker Compose path |
| **Git** | any | `brew install git` | Cloning the repo (pre-installed on most Macs) |
| **.NET SDK** | 8.0+ | `brew install dotnet-sdk` | Local backend development only |
| **Node.js** | 22+ | `brew install node@22` | Local frontend development only |
| **Ollama** (native) | any | `brew install ollama` | Local AI development without Docker |
| **kubectl** | any | `brew install kubectl` | Kubernetes deployments only |

> **Minimum requirement for Docker Compose**: Docker Desktop only.

### Install all at once (if you want everything)

```bash
brew install --cask docker
brew install git dotnet-sdk node@22 ollama kubectl
```

After installing Docker Desktop, launch it from Applications and wait for the whale icon to appear in the menu bar before continuing.

---

## 2. Clone the repo

```bash
git clone https://github.com/carlCarlson6/my-own-chatbot-ai.git
cd my-own-chatbot-ai
```

---

## 3. Run with Docker Compose (Recommended)

All commands are run from the `infrastructure/` directory.

```bash
cd infrastructure
```

### Production-like (all three services)

Builds and starts the full stack — backend, frontend (nginx), and Ollama — in production mode.

```bash
docker compose -f docker-compose.yml up --build
```

- **First run**: pulls the `llama3.2` model (~2 GB). Expect 2–5 minutes depending on your connection.
- The model is stored in the `chatbot-ollama-data` Docker volume and **persists across restarts** — you won't re-download it.
- Frontend is served by nginx on port **3000**.

### Local development (hot reload)

The `docker-compose.override.yml` file is automatically merged when you omit the `-f` flag. It replaces the production images with hot-reload variants:

```bash
docker compose up --build
```

What changes in dev mode:
- **Backend**: runs `dotnet watch` on port `5050` — source files are mounted from `backend/`.
- **Frontend**: runs the Vite dev server on port `5173` with HMR — source files are mounted from `frontend/`.
- **Ollama**: unchanged (same container as production).

### Stop everything

```bash
# Stop containers, keep the model cache volume
docker compose down

# Stop containers AND delete the model cache (re-download required next start)
docker compose down -v
```

---

## 4. Port reference

| Service | Docker prod | Dev (hot reload) | Local native |
|---------|:-----------:|:----------------:|:------------:|
| Frontend | http://localhost:3000 | http://localhost:5173 | http://localhost:5173 |
| Backend API | http://localhost:5050 | http://localhost:5050 | http://localhost:5050 |
| Ollama | http://localhost:11434 | http://localhost:11434 | http://localhost:11434 |

---

## 5. Run on Kubernetes (Local)

Use this to test the full Kubernetes deployment locally before pushing to a cloud cluster.
You need to build the Docker images first, load them into your local cluster, then apply the manifests.

### Prerequisites

| Tool | Install | Purpose |
|------|---------|---------|
| **Docker Desktop** | (already installed) | Builds the images |
| **kind** | `brew install kind` | Local Kubernetes cluster (recommended) |
| **kubectl** | `brew install kubectl` | Apply manifests and port-forward |

> **Alternative**: replace `kind` with `minikube` (`brew install minikube`). Commands differ slightly — see the minikube tab below.

---

### Option A — kind (Recommended)

#### 1. Create a cluster

```bash
kind create cluster --name chatbot-ai
```

Verify kubectl is pointing at it:
```bash
kubectl cluster-info --context kind-chatbot-ai
```

#### 2. Build the Docker images

Run from the **repo root**:
```bash
docker build -f infrastructure/docker/backend/Dockerfile  -t chatbot-ai/backend:latest  .
docker build -f infrastructure/docker/frontend/Dockerfile -t chatbot-ai/frontend:latest .
docker build -f infrastructure/docker/ollama/Dockerfile   -t chatbot-ai/ollama:latest   .
```

> Building the Ollama image pulls `llama3.2` (~2 GB) into the image layer. This only happens once.

#### 3. Load images into the cluster

kind clusters do not share the host Docker daemon. Load each image explicitly:
```bash
kind load docker-image chatbot-ai/backend:latest  --name chatbot-ai
kind load docker-image chatbot-ai/frontend:latest --name chatbot-ai
kind load docker-image chatbot-ai/ollama:latest   --name chatbot-ai
```

#### 4. Apply the manifests

```bash
# Namespace first
kubectl apply -f infrastructure/kubernetes/namespace.yaml

# Then everything else
kubectl apply -R -f infrastructure/kubernetes/
```

#### 5. Wait for pods to be ready

```bash
kubectl -n chatbot-ai get pods -w
```

Expected output (may take 2–3 min while Ollama initialises):
```
NAME                        READY   STATUS    RESTARTS
backend-xxxx                1/1     Running   0
frontend-xxxx               1/1     Running   0
ollama-xxxx                 1/1     Running   0
```

#### 6. Port-forward to access the services

Open three terminals (or run with `&`):
```bash
kubectl -n chatbot-ai port-forward svc/frontend 3000:80    &
kubectl -n chatbot-ai port-forward svc/backend  5050:5050  &
kubectl -n chatbot-ai port-forward svc/ollama   11434:11434 &
```

#### 7. Verify

```bash
curl http://localhost:5050/            # backend health
curl http://localhost:11434/api/tags   # ollama models
open http://localhost:3000             # frontend
```

#### 8. Tear down

```bash
# Remove all resources
kubectl delete namespace chatbot-ai

# Delete the cluster entirely
kind delete cluster --name chatbot-ai
```

---

### Option B — minikube

#### 1. Start the cluster

```bash
minikube start
```

#### 2. Point Docker at minikube's daemon

This lets you build images directly into minikube — no separate load step needed:
```bash
eval $(minikube docker-env)
```

> Run this in every terminal session where you build images for minikube.

#### 3. Build the images (inside minikube's daemon)

```bash
docker build -f infrastructure/docker/backend/Dockerfile  -t chatbot-ai/backend:latest  .
docker build -f infrastructure/docker/frontend/Dockerfile -t chatbot-ai/frontend:latest .
docker build -f infrastructure/docker/ollama/Dockerfile   -t chatbot-ai/ollama:latest   .
```

#### 4. Apply the manifests

```bash
kubectl apply -f infrastructure/kubernetes/namespace.yaml
kubectl apply -R -f infrastructure/kubernetes/
```

#### 5. Wait for pods

```bash
kubectl -n chatbot-ai get pods -w
```

#### 6. Access the frontend via minikube tunnel

```bash
# In a separate terminal — keeps the tunnel alive
minikube tunnel
```

Then open: http://localhost:3000 (uses the LoadBalancer service)

Or use port-forward (no tunnel needed):
```bash
kubectl -n chatbot-ai port-forward svc/frontend 3000:80   &
kubectl -n chatbot-ai port-forward svc/backend  5050:5050 &
```

#### 7. Tear down

```bash
kubectl delete namespace chatbot-ai
minikube stop
```

---

## 6. Run locally without Docker (Optional)

Use this if you want to iterate on a single service outside of Docker, or if you prefer native tooling.

### Backend (.NET 8)

Requires the **.NET SDK 8.0+** prerequisite.

```bash
cd backend
dotnet restore
ASPNETCORE_ENVIRONMENT=Development dotnet run \
  --project src/MyOwnChatbotAi.Api/MyOwnChatbotAi.Api.csproj
```

Verify it's up:

```bash
curl http://localhost:5050/
# Expected: {"service":"my-own-chatbot-ai-api","status":"ok"}
```

### Frontend (Vite + React)

Requires the **Node.js 22+** prerequisite. The Vite dev server proxies `/api/*` to `localhost:5050`.

```bash
cd frontend
npm install
npm run dev
```

Open: http://localhost:5173

### Ollama (native)

Requires the **Ollama** prerequisite.

```bash
# Terminal 1 — start the Ollama server
ollama serve

# Terminal 2 — pull the default model (first time only, ~2 GB)
ollama pull llama3.2
```

Verify the model is available:

```bash
curl http://localhost:11434/api/tags
```

---

## 7. Verify everything is running

Run these checks after startup to confirm each service is healthy:

```bash
# Backend health check
curl http://localhost:5050/
# → {"service":"my-own-chatbot-ai-api","status":"ok"}

# Ollama model list
curl http://localhost:11434/api/tags
# → {"models":[{"name":"llama3.2:latest", ...}]}

# Open the frontend in the browser
open http://localhost:3000    # Docker prod
open http://localhost:5173    # Dev (hot reload) or local native
```

---

## 8. One-time setup notes

### Changing the default Ollama model

The default model is set as a Docker build argument in `infrastructure/docker-compose.yml`:

```yaml
build:
  args:
    DEFAULT_MODEL: llama3.2   # ← change this
```

After editing, rebuild:

```bash
cd infrastructure
docker compose -f docker-compose.yml up --build
```

### Model size reference

| Model | Approx. size | Notes |
|-------|:------------:|-------|
| `llama3.2` | ~2 GB | Default — fast, good for most tasks |
| `mistral` | ~4 GB | Better quality, slower on CPU |
| `llama3` (8B) | ~5 GB | Largest of the defaults |

> **Tip**: On a Mac with Apple Silicon, all models run on the GPU via Metal. Expect noticeably faster inference than on Intel Macs.

---

## 9. Useful commands reference

### Docker Compose

| Task | Command (run from `infrastructure/`) |
|------|--------------------------------------|
| Start full stack (prod) | `docker compose -f docker-compose.yml up --build` |
| Start with hot reload (dev) | `docker compose up --build` |
| Start in background | `docker compose up -d` |
| Stop (keep volumes) | `docker compose down` |
| Stop and delete model cache | `docker compose down -v` |
| View logs (all services) | `docker compose logs -f` |
| View logs (one service) | `docker compose logs -f backend` |
| Rebuild one service | `docker compose build backend` |
| Check service status | `docker compose ps` |

### Backend (local)

| Task | Command (run from repo root) |
|------|------------------------------|
| Build | `dotnet build backend/src/MyOwnChatbotAi.sln` |
| Run | `dotnet run --project backend/src/MyOwnChatbotAi.Api` |
| Watch (hot reload) | `dotnet watch run --project backend/src/MyOwnChatbotAi.Api` |

### Frontend (local)

| Task | Command (run from `frontend/`) |
|------|-------------------------------|
| Install dependencies | `npm install` |
| Dev server (HMR) | `npm run dev` |
| Build for production | `npm run build` |
| Lint | `npm run lint` |

### Ollama (native)

| Task | Command |
|------|---------|
| Start server | `ollama serve` |
| Pull a model | `ollama pull llama3.2` |
| List local models | `ollama list` |
| Remove a model | `ollama rm llama3.2` |

### Kubernetes (local)

| Task | Command |
|------|---------|
| Create kind cluster | `kind create cluster --name chatbot-ai` |
| Load image into kind | `kind load docker-image chatbot-ai/backend:latest --name chatbot-ai` |
| Apply all manifests | `kubectl apply -R -f infrastructure/kubernetes/` |
| Watch pod status | `kubectl -n chatbot-ai get pods -w` |
| Port-forward frontend | `kubectl -n chatbot-ai port-forward svc/frontend 3000:80` |
| View pod logs | `kubectl -n chatbot-ai logs -f deployment/backend` |
| Delete all resources | `kubectl delete namespace chatbot-ai` |
| Delete kind cluster | `kind delete cluster --name chatbot-ai` |
| Start minikube | `minikube start` |
| Use minikube Docker daemon | `eval $(minikube docker-env)` |
| Open via minikube tunnel | `minikube tunnel` (then http://localhost:3000) |
