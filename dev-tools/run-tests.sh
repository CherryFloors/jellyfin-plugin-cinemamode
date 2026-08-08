#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"

podman run --rm \
    -v "${REPO_ROOT}:/src:Z" \
    -w /src \
    mcr.microsoft.com/dotnet/sdk:9.0 \
    dotnet test --verbosity normal
