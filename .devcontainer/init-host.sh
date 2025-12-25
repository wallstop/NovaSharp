#!/usr/bin/env bash
# Host-side initialization script for NovaSharp dev container
# This runs on the HOST before the container is created.
# Purpose: Clean stale Windows build artifacts and fix line endings.

set -euo pipefail

cd "${1:-.}"

# Clean stale Windows build artifacts that would cause issues in Linux container
# Use find with -prune for efficiency
find src -type d \( -name obj -o -name bin \) -prune -exec rm -rf {} + 2>/dev/null || true

# Clean Visual Studio cache (contains Windows-specific paths)
rm -rf src/.vs 2>/dev/null || true

# Fix line endings for shell scripts (Windows git autocrlf issues)
# Only process if files exist and are regular files
for f in .devcontainer/*.sh; do
    if [ -f "$f" ]; then
        # Use tr to remove carriage returns (more portable than sed -i on macOS)
        if grep -q $'\r' "$f" 2>/dev/null; then
            tr -d '\r' < "$f" > "$f.tmp" && mv "$f.tmp" "$f"
        fi
    fi
done

echo "✅ Host initialization complete"
