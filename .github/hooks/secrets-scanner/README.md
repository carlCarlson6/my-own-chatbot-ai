---
name: 'Secrets Scanner'
description: 'Scans modified files for likely secrets before a Copilot-driven commit or handoff'
tags: ['security', 'secrets', 'hook']
---

# Secrets Scanner

Scan modified files for likely leaked secrets before an agent session ends or before you commit. The hook is tailored for this repo's workflow: it checks the current git diff or staged files, prints only file/line metadata, and avoids echoing matched secret values back to the terminal.

## Files

- `scan-secrets.sh` — the scanner script
- `hooks.json` — example GitHub Copilot hook configuration

## Default behavior

- Scans **git diff vs `HEAD`** by default
- Can scan an explicit git diff range for CI (`SCAN_SCOPE=range` + `SCAN_RANGE=<base>...<head>`)
- Supports **warn** mode (log findings, exit `0`)
- Supports **block** mode (exit non-zero when findings exist)
- Skips lockfiles, binary files, and common placeholder/example values
- Supports an allowlist for known-safe false positives

## Install

1. Keep this folder at `.github/hooks/secrets-scanner/`.
2. Ensure the script is executable:

   ```bash
   chmod +x .github/hooks/secrets-scanner/scan-secrets.sh
   ```

3. Wire it into your local Copilot hooks setup using `hooks.json` as the starting point.

## Configuration

| Variable | Values | Default | Purpose |
|---|---|---|---|
| `SCAN_MODE` | `warn`, `block` | `warn` | Whether findings only log or also fail the hook |
| `SCAN_SCOPE` | `diff`, `staged`, `range` | `diff` | Whether to scan local changes, staged files, or a specific git diff range |
| `SCAN_RANGE` | git revision range | unset | Required when `SCAN_SCOPE=range`, e.g. `origin/main...HEAD` |
| `SECRETS_ALLOWLIST` | comma-separated substrings | unset | Ignore known false positives containing these substrings |
| `SKIP_SECRETS_SCAN` | `true` | unset | Skip the scan entirely |

## Recommended usage

- Use `SCAN_MODE=warn` while introducing the hook.
- Move to `SCAN_MODE=block` once the team is comfortable with the false-positive profile.
- Prefer `SCAN_SCOPE=staged` if you want the hook to align with manual staging.
- Use `SCAN_SCOPE=range` in CI to scan exactly the files changed by a pull request.

## Example command

```bash
SCAN_MODE=warn SCAN_SCOPE=diff .github/hooks/secrets-scanner/scan-secrets.sh
```
