#!/usr/bin/env bash
set -euo pipefail

DEVSERVER_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$DEVSERVER_DIR")"

DOCKER=${DOCKER:-$(command -v podman 2>/dev/null || command -v docker 2>/dev/null || echo "")}
if [[ -z "$DOCKER" ]]; then
    echo "error: neither podman nor docker found in PATH" >&2
    exit 1
fi

"$DOCKER" build -t cinemamode-devserver "$DEVSERVER_DIR"

CONTAINER_ID=$("$DOCKER" run -d --rm \
    -p 8080:8080 \
    -v "$PROJECT_ROOT/Jellyfin.Plugin.CinemaMode/Configuration:/app/config" \
    -v "$DEVSERVER_DIR/fixtures:/app/fixtures" \
    cinemamode-devserver)

stop_container() {
    printf '\n'
    "$DOCKER" stop "$CONTAINER_ID" 2>/dev/null
}
trap stop_container INT TERM

echo "Cinema Mode dev server → http://localhost:8080  (Ctrl-C to stop)"
"$DOCKER" logs -f "$CONTAINER_ID"
