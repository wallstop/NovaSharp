---
name: git-safe-operations
description: "Design scripts and hooks that modify the Git index without lock races or data loss. Use when changing git add, git reset, staging, pre-commit hooks, index.lock handling, or concurrent Git automation."
metadata:
  category: workflow
  priority: reference
  related: ci-workflow
---
# Skill: Git-Safe Operations for Scripts and Hooks

**Related Files**: [scripts/dev/pre-commit.sh](../../../scripts/dev/pre-commit.sh), [.githooks/pre-commit](../../../.githooks/pre-commit)

______________________________________________________________________

## Purpose

Ensure all scripts and hooks that interact with the git index use proper locking, retries, and coordination to prevent race conditions and lock contention errors.

______________________________________________________________________

## Background: The `index.lock` Problem

Git creates a lock file at `.git/index.lock` during any operation that modifies the index (staging area). This prevents concurrent modifications from corrupting the index.

**The Problem**: When multiple processes attempt index operations simultaneously (e.g., a pre-commit hook running `git add` while the user interacts with lazygit, GitLens, or another git tool), you get:

```
fatal: Unable to create '.git/index.lock': File exists.

Another git process seems to be running in this repository, e.g.
an editor opened by 'git commit'. Please make sure all processes
are terminated then try again.
```

This is especially common with:

- Pre-commit hooks that stage formatted files
- IDE git integrations running in background
- Multiple terminal sessions with git operations
- TUI tools like lazygit, tig, gitui

______________________________________________________________________

## 🔴 Required Pattern: `git_add_with_retry`

**Always use the `git_add_with_retry` function from `scripts/dev/pre-commit.sh`** for any staging operations.

### Key Features

1. **Lock polling** — Waits for existing `index.lock` to be released before attempting
1. **Exponential backoff** — Starts at 50ms, multiplies by 1.4 each retry, caps at 3000ms
1. **Jitter** — Adds 0-50ms random delay to prevent thundering herd
1. **Configurable retries** — Up to 30 attempts over ~45 seconds total
1. **Clean failure** — Falls through to real error on non-lock failures

### Implementation Reference

```bash
# From scripts/dev/pre-commit.sh
git_add_with_retry() {
  max_retries=30
  retry_delay_ms=50
  max_delay_ms=3000
  lock_poll_interval_ms=50
  lock_timeout_ms=5000
  attempt=0

  # ... (see full implementation in scripts/dev/pre-commit.sh)

  while [ "$attempt" -lt "$max_retries" ]; do
    # Wait for any existing lock to be released before attempting
    if [ -f ".git/index.lock" ]; then
      if ! wait_for_lock_release; then
        warn "git index.lock still present after ${lock_timeout_ms}ms wait"
      fi
    fi

    # Attempt the git add
    if git add -- "$@"; then
      return 0
    fi

    # Exponential backoff with jitter on lock contention
    # ...
  done
}
```

### Usage

```bash
# ✅ CORRECT: Use retry wrapper
git_add_with_retry file1.cs file2.cs

# ✅ CORRECT: Batch multiple files in single call
git_add_with_retry "${formatted_files[@]}"

# ✅ CORRECT: Source the function from pre-commit.sh
source "$(dirname "$0")/../dev/pre-commit.sh"
git_add_with_retry "$file"
```

______________________________________________________________________

## Additional guidance

Read [the detailed reference](references/REFERENCE.md) for Forbidden Patterns, Checklist for New Scripts, When Retry Isn't Enough, Stale Lock Cleanup, and later sections.
