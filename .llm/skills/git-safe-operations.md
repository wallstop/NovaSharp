# Skill: Git-Safe Operations for Scripts and Hooks

**When to use**: Creating or modifying scripts that run `git add`, `git reset`, or other git index operations.

**Related Files**: [scripts/dev/pre-commit.sh](../../scripts/dev/pre-commit.sh), [.githooks/pre-commit](../../.githooks/pre-commit)

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

## 🔴 Forbidden Patterns

### ❌ Never use raw `git add` without retry logic

```bash
# ❌ WRONG: No retry, will fail on lock contention
git add formatted-file.cs

# ❌ WRONG: Even with error checking, no retry
if ! git add formatted-file.cs; then
  echo "Failed to stage"
  exit 1
fi
```

### ❌ Never ignore exit codes

```bash
# ❌ WRONG: Silently ignores failures
git add formatted-file.cs || true

# ❌ WRONG: Continues on failure
git add file1.cs
git add file2.cs  # Runs even if first failed
```

### ❌ Never use `git add` in a loop without batching

```bash
# ❌ WRONG: N separate git operations = N chances for lock contention
for file in "${files[@]}"; do
  git add "$file"
done

# ✅ CORRECT: Single batched operation
git_add_with_retry "${files[@]}"
```

### ❌ Never manually delete `index.lock`

```bash
# ❌ WRONG: Can corrupt the index if another process is legitimately using it
rm -f .git/index.lock
git add file.cs
```

______________________________________________________________________

## Checklist for New Scripts

When creating or modifying scripts that interact with git:

- [ ] **Use `git_add_with_retry`** for ALL staging operations
- [ ] **Batch files together** in a single call when possible
- [ ] **Source the function** from `scripts/dev/pre-commit.sh` or copy the implementation
- [ ] **Test with concurrent git operations** — Run the script while using lazygit or another git tool
- [ ] **Verify no lock errors** appear under concurrent load
- [ ] **Check exit codes** — Don't silently ignore git command failures

### Testing Concurrency

```bash
# Terminal 1: Run your script in a loop
while true; do ./your-script.sh; sleep 0.1; done

# Terminal 2: Spam git operations
while true; do git status; git diff --cached; done

# Watch for "index.lock" errors in either terminal
```

______________________________________________________________________

## When Retry Isn't Enough

If you're seeing persistent lock contention even with retries:

1. **Check for stuck processes** — `ps aux | grep git`
1. **Use stale lock cleanup** — The `cleanup_stale_index_lock` function in `scripts/dev/pre-commit.sh` safely removes orphaned locks
1. **Increase timeouts** — Adjust `max_retries` or `lock_timeout_ms` for very slow systems
1. **Serialize operations** — Use a wrapper script that coordinates multiple git tools

______________________________________________________________________

## Stale Lock Cleanup

A **stale lock** is an `index.lock` file left behind when a git process crashes, is killed (Ctrl+C, OOM), or the terminal is closed mid-operation.

### The Problem

```
fatal: Unable to create '.git/index.lock': File exists.
```

This error occurs when `index.lock` exists but **no process owns it**. Common causes:

- User pressed Ctrl+C during `git add` or `git commit`
- IDE extension (GitLens, git-graph) crashed mid-operation
- Terminal was closed while git was running
- TUI tool (lazygit, tig, gitui) was force-quit
- System OOM killed a git process

### Safe Detection

**NEVER blindly delete `index.lock`** — it might be legitimately held by another process!

Use `cleanup_stale_index_lock` from `scripts/dev/pre-commit.sh`:

```bash
# Safely cleans up ONLY if no process has the lock file open
cleanup_stale_index_lock() {
  lock_file=".git/index.lock"
  
  # No lock file = nothing to clean
  if [ ! -f "$lock_file" ]; then
    return 0
  fi
  
  # Check if any process has the lock file open via lsof/fuser
  lock_is_held=0
  
  if command -v lsof >/dev/null 2>&1; then
    if lsof "$lock_file" >/dev/null 2>&1; then
      lock_is_held=1
    fi
  elif command -v fuser >/dev/null 2>&1; then
    if fuser "$lock_file" >/dev/null 2>&1; then
      lock_is_held=1
    fi
  else
    # Fallback: if lock is older than 30 seconds, assume stale
    lock_mtime=$(stat -c %Y "$lock_file" 2>/dev/null || stat -f %m "$lock_file" 2>/dev/null || echo 0)
    current_time=$(date +%s)
    lock_age=$((current_time - lock_mtime))
    [ "$lock_age" -ge 30 ] || lock_is_held=1
  fi
  
  if [ "$lock_is_held" -eq 1 ]; then
    return 1  # Lock is legitimately held
  fi
  
  # Lock is stale - safe to remove
  rm -f "$lock_file"
  return 0
}
```

### Integration Points

Call `cleanup_stale_index_lock` at these points:

1. **Start of pre-commit hook** — Before any git operations
1. **Before each retry attempt** — In case lock became stale during wait
1. **In any script that runs git index operations** — Proactively clean up

______________________________________________________________________

## VS Code / IDE Configuration

To reduce lock contention from IDE git integrations:

```json
{
  // GitLens: Reduce background refresh frequency
  "gitlens.statusBar.enabled": false,
  "gitlens.hovers.currentLine.over": "line",
  "gitlens.currentLine.enabled": false,
  
  // Git: Reduce auto-refresh
  "git.autorefresh": false,
  "git.decorations.enabled": false,
  
  // Alternative: Use debounced refresh (GitLens 14+)
  "gitlens.advanced.repositorySearchDepth": 1
}
```

______________________________________________________________________

## See Also

- [scripts/dev/pre-commit.sh](../../scripts/dev/pre-commit.sh) — Reference implementation of `cleanup_stale_index_lock` and `git_add_with_retry`
- [.githooks/pre-commit](../../.githooks/pre-commit) — Hook that delegates to the dev script
