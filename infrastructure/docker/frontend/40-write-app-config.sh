#!/bin/sh
set -eu

escaped_publishable_key=$(
  printf '%s' "${CLERK_PUBLISHABLE_KEY:-}" \
    | sed 's/\\/\\\\/g; s/"/\\"/g'
)

cat <<EOF >/usr/share/nginx/html/app-config.js
window.__APP_CONFIG__ = {
  CLERK_PUBLISHABLE_KEY: "${escaped_publishable_key}"
};
window.CLERK_PUBLISHABLE_KEY = window.__APP_CONFIG__.CLERK_PUBLISHABLE_KEY;
EOF
