#!/usr/bin/env sh
set -eu

log() {
  printf '%s\n' "$1"
}

warn() {
  printf '%s\n' "[pre-commit] WARNING: $1" >&2
}

# Cleans up stale git index.lock files left by crashed/killed processes.
# A lock is considered stale if:
#   1. The lock file exists, AND
#   2. No process has the lock file open (checked via lsof/fuser)
#
# This handles scenarios where:
#   - A git process was killed (Ctrl+C, OOM, etc.) before cleanup
#   - IDE git integration (GitLens, git-graph) crashed mid-operation
#   - Terminal was closed while git was running
#   - lazygit/tig/gitui was force-quit
#
# Returns: 0 if lock was cleaned or didn't exist, 1 if lock is legitimately held
cleanup_stale_index_lock() {
  lock_file=".git/index.lock"
  
  # No lock file = nothing to clean
  if [ ! -f "$lock_file" ]; then
    return 0
  fi
  
  # Check if any process has the lock file open
  # Try lsof first (most common), fall back to fuser
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
    # No lsof or fuser available - check lock file age as fallback
    # If lock is older than 30 seconds, assume it's stale
    if command -v stat >/dev/null 2>&1; then
      # Get lock file modification time (seconds since epoch)
      lock_mtime=$(stat -c %Y "$lock_file" 2>/dev/null || stat -f %m "$lock_file" 2>/dev/null || echo 0)
      current_time=$(date +%s)
      lock_age=$((current_time - lock_mtime))
      
      if [ "$lock_age" -lt 30 ]; then
        # Lock is recent, assume it's held
        lock_is_held=1
      fi
    else
      # Can't determine - assume held to be safe
      lock_is_held=1
    fi
  fi
  
  if [ "$lock_is_held" -eq 1 ]; then
    # Lock is legitimately held by another process
    return 1
  fi
  
  # Lock file exists but no process has it open - it's stale, clean it up
  warn "Cleaning up stale git index.lock (no process has it open)"
  rm -f "$lock_file"
  return 0
}

# Runs git add with retry logic to handle index.lock contention.
# Enhanced with polling, exponential backoff, and jitter.
# Usage: git_add_with_retry [file ...]
git_add_with_retry() {
  max_retries=30
  retry_delay_ms=50
  max_delay_ms=3000
  lock_poll_interval_ms=50
  lock_timeout_ms=5000
  attempt=0

  # Helper: sleep for milliseconds (POSIX-compatible)
  sleep_ms() {
    _ms="$1"
    # Use awk for float division since POSIX sh lacks floating point
    _secs=$(awk "BEGIN { printf \"%.3f\", $_ms / 1000 }")
    sleep "$_secs"
  }

  # Helper: generate random jitter 0-50ms
  random_jitter_ms() {
    # Use awk with systime seed for randomness, fallback to $$ if needed
    awk "BEGIN { srand($$ + $(date +%s 2>/dev/null || echo 0)); printf \"%d\", int(rand() * 51) }"
  }

  # Helper: wait for index.lock to be released (with timeout)
  wait_for_lock_release() {
    _waited_ms=0
    while [ -f ".git/index.lock" ] && [ "$_waited_ms" -lt "$lock_timeout_ms" ]; do
      sleep_ms "$lock_poll_interval_ms"
      _waited_ms=$((_waited_ms + lock_poll_interval_ms))
    done
    # Return success if lock is gone, failure if still locked
    [ ! -f ".git/index.lock" ]
  }

  while [ "$attempt" -lt "$max_retries" ]; do
    # Clean up stale locks before attempting (no process owns them)
    cleanup_stale_index_lock
    
    # Wait for any existing lock to be released before attempting
    if [ -f ".git/index.lock" ]; then
      if ! wait_for_lock_release; then
        # Lock still exists after timeout - try stale cleanup again
        if cleanup_stale_index_lock; then
          # Lock was stale and cleaned, continue
          :
        else
          warn "git index.lock still present after ${lock_timeout_ms}ms wait (held by another process)"
        fi
      fi
    fi

    # Attempt the git add
    if git add -- "$@"; then
      return 0
    fi

    # Check if the failure was due to index.lock
    if [ -f ".git/index.lock" ]; then
      attempt=$((attempt + 1))
      if [ "$attempt" -lt "$max_retries" ]; then
        # Add jitter to prevent thundering herd
        jitter_ms=$(random_jitter_ms)
        total_delay_ms=$((retry_delay_ms + jitter_ms))
        warn "git index.lock contention detected, retrying in ${total_delay_ms}ms (attempt $attempt/$max_retries)..."
        sleep_ms "$total_delay_ms"
        # Exponential backoff: multiply by 1.4, cap at max_delay_ms
        retry_delay_ms=$(awk "BEGIN { v = int($retry_delay_ms * 1.4); if (v > $max_delay_ms) v = $max_delay_ms; print v }")
      fi
    else
      # Some other error occurred, fail immediately
      git add -- "$@"
      return $?
    fi
  done

  # Final attempt - let it fail with the actual error message
  git add -- "$@"
}

ensure_tool_on_path() {
  tool_name="$1"
  friendly_name="$2"
  if ! command -v "$tool_name" >/dev/null 2>&1; then
    printf '%s\n' "[pre-commit] $friendly_name is required but was not found on PATH." >&2
    exit 1
  fi
}

run_python() {
  if [ -n "${PYTHON:-}" ]; then
    "$PYTHON" "$@"
    return
  fi

  if command -v python3 >/dev/null 2>&1; then
    python3 "$@"
    return
  fi

  if command -v python >/dev/null 2>&1; then
    python "$@"
    return
  fi

  if command -v py >/dev/null 2>&1; then
    py -3 "$@"
    return
  fi

  printf '%s\n' "[pre-commit] Python 3 is required to format Markdown files. Set PYTHON to override." >&2
  exit 1
}

install_python_tooling() {
  log "[pre-commit] Installing Python tooling dependencies..."
  if run_python -m pip install --quiet --requirement requirements.tooling.txt; then
    return 0
  fi

  warn "Standard pip install failed; retrying with user-level installation..."
  if run_python -m pip install --quiet --user --requirement requirements.tooling.txt; then
    return 0
  fi

  warn "User install failed; retrying with --break-system-packages (PEP 668 overrides)..."
  if run_python -m pip install --quiet --user --break-system-packages --requirement requirements.tooling.txt; then
    return 0
  fi

  warn "Failed to install Python tooling dependencies. Run 'python -m pip install --break-system-packages -r requirements.tooling.txt'."
  return 1
}

check_mdformat_version() {
  # Expected version from requirements.tooling.txt
  expected_version="1.0.0"

  # Get installed version (returns empty if not installed)
  installed_version="$(run_python -c "import mdformat; print(mdformat.__version__)" 2>/dev/null || printf '')"

  if [ -z "$installed_version" ]; then
    warn "mdformat is not installed; installing tooling requirements..."
    install_python_tooling || return 1
    installed_version="$(run_python -c "import mdformat; print(mdformat.__version__)" 2>/dev/null || printf '')"

    if [ -z "$installed_version" ]; then
      warn "mdformat is still missing. Run 'python -m pip install -r requirements.tooling.txt' to install."
      return 1
    fi
  fi

  if [ "$installed_version" != "$expected_version" ]; then
    warn "mdformat version mismatch: installed $installed_version, expected $expected_version"
    warn "Attempting to update Python tooling dependencies..."
    if install_python_tooling; then
      installed_version="$(run_python -c "import mdformat; print(mdformat.__version__)" 2>/dev/null || printf '')"
    else
      warn "Continuing with installed version (may cause CI failures)..."
      return 0
    fi

    if [ "$installed_version" != "$expected_version" ]; then
      warn "mdformat version remains $installed_version after update; continuing (may cause CI failures)..."
    fi
  fi

  return 0
}

staged_files_for_pattern() {
  pattern="$1"
  git diff --cached --name-only --diff-filter=ACM -- "$pattern"
}

format_all_csharp_files() {
  log "[pre-commit] Running CSharpier across the entire repository..."
  dotnet tool run csharpier format . >/dev/null
  log "[pre-commit] CSharpier formatting complete."

  cs_output="$(staged_files_for_pattern '*.cs' || printf '')"
  if [ -z "$cs_output" ]; then
    log "[pre-commit] No staged C# files detected; nothing to restage."
    return
  fi

  log "[pre-commit] Restaging previously staged C# files..."
  (
    set -f
    old_ifs=$IFS
    IFS='
'
    set -- dummy
    for file in $cs_output; do
      [ -z "$file" ] && continue
      set -- "$@" "$file"
    done
    shift
    IFS=$old_ifs

    git_add_with_retry "$@"
  )
}

format_markdown_files() {
  md_output="$(staged_files_for_pattern '*.md' || printf '')"
  if [ -z "$md_output" ]; then
    log "[pre-commit] No staged Markdown files detected; skipping Markdown checks."
    return
  fi

  if ! check_mdformat_version; then
    warn "mdformat is required to format Markdown. Run 'python -m pip install -r requirements.tooling.txt'."
    exit 1
  fi

  (
    set -f
    old_ifs=$IFS
    IFS='
'
    set -- dummy
    for file in $md_output; do
      [ -z "$file" ] && continue
      set -- "$@" "$file"
    done
    shift
    IFS=$old_ifs

    log "[pre-commit] Formatting staged Markdown files..."
    run_python scripts/ci/format_markdown.py --fix --files "$@"

    log "[pre-commit] Checking Markdown links for staged files..."
    run_python scripts/ci/check_markdown_links.py --files "$@"

    log "[pre-commit] Restaging formatted Markdown files..."
    git_add_with_retry "$@"
  )
}

update_documentation_audit_log() {
  log "[pre-commit] Refreshing documentation audit log..."
  run_python tools/DocumentationAudit/documentation_audit.py --write-log docs/audits/documentation_audit.log
  if [ -f docs/audits/documentation_audit.log ]; then
    git_add_with_retry docs/audits/documentation_audit.log
  fi
}

update_naming_audit_log() {
  log "[pre-commit] Refreshing naming audit log..."
  run_python tools/NamingAudit/naming_audit.py --write-log docs/audits/naming_audit.log
  if [ -f docs/audits/naming_audit.log ]; then
    git_add_with_retry docs/audits/naming_audit.log
  fi
}

run_powershell_script() {
  script="$1"
  if command -v pwsh >/dev/null 2>&1; then
    pwsh -NoLogo -NoProfile -File "$script"
    return
  fi

  if command -v powershell >/dev/null 2>&1; then
    powershell -NoLogo -NoProfile -File "$script"
    return
  fi

  printf '%s\n' "[pre-commit] PowerShell is required to run $script. Install pwsh or Windows PowerShell to keep the fixture catalog in sync." >&2
  exit 1
}

update_fixture_catalog() {
  log "[pre-commit] Regenerating NUnit fixture catalog..."
  run_powershell_script ./scripts/tests/update-fixture-catalog.ps1 >/dev/null
  git_add_with_retry src/tests/WallstopStudios.NovaSharp.Interpreter.Tests/FixtureCatalogGenerated.cs
}

update_spelling_audit_log() {
  log "[pre-commit] Refreshing spelling audit log..."
  if run_python tools/SpellingAudit/spelling_audit.py --write-log docs/audits/spelling_audit.log 2>/dev/null; then
    if [ -f docs/audits/spelling_audit.log ]; then
      git_add_with_retry docs/audits/spelling_audit.log
    fi
  else
    warn "Spelling audit failed (codespell may not be installed). Run 'pip install -r requirements.tooling.txt' to enable."
  fi
}

check_branding() {
  log "[pre-commit] Checking NovaSharp branding..."
  # Check for MoonSharp-branded filenames in staged files
  moonsharp_files="$(git diff --cached --name-only --diff-filter=ACM | grep -i 'MoonSharp' || printf '')"
  if [ -n "$moonsharp_files" ]; then
    printf '%s\n' "[pre-commit] ERROR: MoonSharp-branded filenames detected in staged files:" >&2
    printf '%s\n' "$moonsharp_files" >&2
    exit 1
  fi

  # Check for MoonSharp content in staged files (excluding allowlisted paths)
  staged_output="$(git diff --cached --name-only --diff-filter=ACM || printf '')"
  if [ -z "$staged_output" ]; then
    return
  fi

  # Allowlist for files that may legitimately contain MoonSharp references
  # (performance comparisons, branding enforcement scripts, documentation about branding)

  violations=""
  for file in $staged_output; do
    # Skip allowlisted files
    case "$file" in
      docs/Performance.md|README.md|src/samples/Tutorial/Tutorials/readme.md|moonsharp_DescriptorHelpers.cs|AGENTS.md|PLAN.md) continue ;;
      src/tooling/WallstopStudios.NovaSharp.Benchmarks/PerformanceReportWriter.cs) continue ;;
      scripts/branding/ensure-novasharp-branding.sh) continue ;;
      scripts/dev/pre-commit.sh|scripts/dev/README.md) continue ;;  # Branding check documentation
      src/tooling/WallstopStudios.NovaSharp.Comparison*) continue ;;
      .devcontainer/devcontainer.json) continue ;;  # cSpell dictionary includes MoonSharp
      .llm/skills/documentation-and-changelog.md) continue ;;  # Changelog example includes MoonSharp
      .llm/skills/pre-commit-validation.md) continue ;;  # Pre-commit docs explain branding check
    esac

    # Check staged content for MoonSharp
    if git show ":$file" 2>/dev/null | grep -q 'MoonSharp'; then
      violations="$violations$file\n"
    fi
  done

  if [ -n "$violations" ]; then
    printf '%s\n' "[pre-commit] ERROR: MoonSharp identifier detected in staged content:" >&2
    printf "$violations" >&2
    printf '%s\n' "Replace with NovaSharp or add to the allowlist in scripts/branding/ensure-novasharp-branding.sh" >&2
    exit 1
  fi
}

check_namespace_alignment() {
  log "[pre-commit] Checking namespace alignment..."
  if ! run_python tools/NamespaceAudit/namespace_audit.py; then
    printf '%s\n' "[pre-commit] ERROR: Namespace mismatches detected. Fix the namespaces to match directory layout." >&2
    exit 1
  fi
}

check_shell_executable() {
  log "[pre-commit] Checking shell script permissions..."
  if ! run_python scripts/lint/check-shell-executable.py 2>/dev/null; then
    printf '%s\n' "[pre-commit] ERROR: Shell scripts missing executable bit. See message above for fix." >&2
    exit 1
  fi
}

check_shell_python_invocation() {
  log "[pre-commit] Checking shell script Python invocation patterns..."
  if ! run_python scripts/lint/check-shell-python-invocation.py 2>/dev/null; then
    printf '%s\n' "[pre-commit] ERROR: Shell scripts must use explicit 'python' to invoke .py files. See message above." >&2
    exit 1
  fi
}

check_test_lint() {
  # Only run test linting if test files are staged
  test_files="$(git diff --cached --name-only --diff-filter=ACM -- 'src/tests/*.cs' || printf '')"
  if [ -z "$test_files" ]; then
    return
  fi

  log "[pre-commit] Running test infrastructure lint checks..."
  lint_failed=0

  # Check for temp path usage violations
  if ! run_python scripts/lint/check-temp-path-usage.py 2>/dev/null; then
    lint_failed=1
  fi

  # Check for userdata scope violations
  if ! run_python scripts/lint/check-userdata-scope-usage.py 2>/dev/null; then
    lint_failed=1
  fi

  # Check for console capture violations
  if ! run_python scripts/lint/check-console-capture-semaphore.py 2>/dev/null; then
    lint_failed=1
  fi

  # Check for finally block violations
  if ! run_python scripts/lint/check-test-finally.py 2>/dev/null; then
    lint_failed=1
  fi

  if [ "$lint_failed" -eq 1 ]; then
    printf '%s\n' "[pre-commit] ERROR: Test lint checks failed. See messages above." >&2
    exit 1
  fi
}

repo_root="$(git rev-parse --show-toplevel 2>/dev/null || printf '')"
if [ -z "$repo_root" ]; then
  printf '%s\n' "pre-commit hook must run inside the repo." >&2
  exit 1
fi

cd "$repo_root"

# === Stale Lock Cleanup ===
# Clean up any orphaned index.lock files left by crashed git processes
# This prevents "Unable to create index.lock: File exists" errors when
# using lazygit, GitLens, or other concurrent git tools
cleanup_stale_index_lock

ensure_tool_on_path "git" "Git"
ensure_tool_on_path "dotnet" ".NET SDK"

log "[pre-commit] Restoring dotnet tools (CSharpier)..."
dotnet tool restore >/dev/null

# === Auto-fix / Auto-update Hooks ===
# These hooks auto-fix issues and restage files

format_all_csharp_files
format_markdown_files
update_documentation_audit_log
update_naming_audit_log
update_spelling_audit_log
update_fixture_catalog

# === Validation Hooks ===
# These hooks check for issues and fail the commit if found

check_branding
check_namespace_alignment
check_shell_executable
check_shell_python_invocation
check_test_lint

log "[pre-commit] Completed successfully."
