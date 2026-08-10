# Git-Safe Operations for Scripts and Hooks Reference

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

- [scripts/dev/pre-commit.sh](../../../../scripts/dev/pre-commit.sh) — Reference implementation of `cleanup_stale_index_lock` and `git_add_with_retry`
- [.githooks/pre-commit](../../../../.githooks/pre-commit) — Hook that delegates to the dev script
