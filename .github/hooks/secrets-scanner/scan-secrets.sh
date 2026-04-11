#!/usr/bin/env bash

set -euo pipefail

SCAN_MODE="${SCAN_MODE:-warn}"
SCAN_SCOPE="${SCAN_SCOPE:-diff}"
SCAN_RANGE="${SCAN_RANGE:-}"
SECRETS_ALLOWLIST="${SECRETS_ALLOWLIST:-}"
SKIP_SECRETS_SCAN="${SKIP_SECRETS_SCAN:-}"

if [[ "${SKIP_SECRETS_SCAN}" == "true" ]]; then
  echo "Secrets scan skipped."
  exit 0
fi

if [[ "${SCAN_MODE}" != "warn" && "${SCAN_MODE}" != "block" ]]; then
  echo "Invalid SCAN_MODE: ${SCAN_MODE}. Expected 'warn' or 'block'." >&2
  exit 2
fi

if [[ "${SCAN_SCOPE}" != "diff" && "${SCAN_SCOPE}" != "staged" && "${SCAN_SCOPE}" != "range" ]]; then
  echo "Invalid SCAN_SCOPE: ${SCAN_SCOPE}. Expected 'diff', 'staged', or 'range'." >&2
  exit 2
fi

IFS=',' read -r -a ALLOWLIST <<< "${SECRETS_ALLOWLIST}"

if [[ "${SCAN_SCOPE}" == "range" && -z "${SCAN_RANGE}" ]]; then
  echo "SCAN_RANGE is required when SCAN_SCOPE=range." >&2
  exit 2
fi

PATTERNS=(
  "AWS_ACCESS_KEY|critical|AKIA[0-9A-Z]{16}"
  "GITHUB_PAT|critical|gh[pousr]_[A-Za-z0-9_]{20,255}"
  "PRIVATE_KEY|critical|-----BEGIN ([A-Z0-9]+ )?PRIVATE KEY-----"
  "STRIPE_OR_GENERIC_SK|critical|sk_(live|test)_[A-Za-z0-9]{16,}"
  "SLACK_TOKEN|high|xox[baprs]-[A-Za-z0-9-]{10,}"
  "CONNECTION_STRING|high|(postgres(ql)?|mysql|mongodb(\\+srv)?|redis|sqlserver)://[^[:space:]\"']+"
  "GENERIC_SECRET_ASSIGNMENT|high|(api[_-]?key|secret|token|password)[[:space:]]*[:=][[:space:]]*[\"'][^\"']{8,}[\"']"
  "JWT_TOKEN|medium|eyJ[A-Za-z0-9_-]{10,}\\.[A-Za-z0-9._-]{10,}\\.[A-Za-z0-9._-]{10,}"
)

should_skip_path() {
  local path="$1"

  case "${path}" in
    *.lock|*.min.js|*.min.css|package-lock.json|pnpm-lock.yaml|yarn.lock|Cargo.lock|*.svg|*.png|*.jpg|*.jpeg|*.gif)
      return 0
      ;;
  esac

  return 1
}

is_text_file() {
  local path="$1"
  grep -Iq . "${path}" 2>/dev/null || [[ ! -s "${path}" ]]
}

looks_like_placeholder() {
  local value="$1"
  printf '%s' "${value}" | grep -Eiq '(example|changeme|replace[-_]?me|your[-_]|dummy|sample|placeholder|test[-_]?value|not-a-real)'
}

is_allowlisted() {
  local value="$1"
  local item

  for item in "${ALLOWLIST[@]}"; do
    [[ -z "${item}" ]] && continue
    if [[ "${value}" == *"${item}"* ]]; then
      return 0
    fi
  done

  return 1
}

list_files() {
  if [[ "${SCAN_SCOPE}" == "staged" ]]; then
    git diff --cached --name-only --diff-filter=ACMRTUXB
  elif [[ "${SCAN_SCOPE}" == "range" ]]; then
    git diff --name-only --diff-filter=ACMRTUXB "${SCAN_RANGE}"
  else
    if git rev-parse --verify HEAD >/dev/null 2>&1; then
      git diff --name-only --diff-filter=ACMRTUXB HEAD
    else
      git ls-files --others --exclude-standard
    fi
  fi
}

declare -a FINDINGS=()
declare -i FILES_SCANNED=0

while IFS= read -r file; do
  [[ -z "${file}" ]] && continue
  [[ -f "${file}" ]] || continue

  if should_skip_path "${file}"; then
    continue
  fi

  if ! is_text_file "${file}"; then
    continue
  fi

  FILES_SCANNED+=1

  for entry in "${PATTERNS[@]}"; do
    IFS='|' read -r name severity regex <<< "${entry}"

    while IFS=: read -r line_number _; do
      [[ -n "${line_number}" ]] || continue
      line_value="$(sed -n "${line_number}p" "${file}")"

      if looks_like_placeholder "${line_value}" || is_allowlisted "${line_value}"; then
        continue
      fi

      FINDINGS+=("${file}|${line_number}|${name}|${severity}")
    done < <(grep -Ein "${regex}" "${file}" || true)
  done
done < <(list_files)

if [[ ${FILES_SCANNED} -eq 0 ]]; then
  echo "Secrets scan: no modified text files to scan."
  exit 0
fi

if [[ ${#FINDINGS[@]} -eq 0 ]]; then
  echo "Secrets scan: clean (${FILES_SCANNED} file(s) scanned)."
  exit 0
fi

printf 'Secrets scan found %d potential issue(s) across %d scanned file(s):\n' "${#FINDINGS[@]}" "${FILES_SCANNED}"
printf '%-60s %-6s %-28s %-10s\n' "FILE" "LINE" "PATTERN" "SEVERITY"

for finding in "${FINDINGS[@]}"; do
  IFS='|' read -r file line_number name severity <<< "${finding}"
  printf '%-60s %-6s %-28s %-10s\n' "${file}" "${line_number}" "${name}" "${severity}"
done

if [[ "${SCAN_MODE}" == "block" ]]; then
  echo "Secrets scan blocked the session. Remove the finding or allowlist the false positive."
  exit 1
fi

echo "Secrets scan completed in warn mode."
exit 0
