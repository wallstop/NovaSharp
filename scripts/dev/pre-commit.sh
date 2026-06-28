#!/usr/bin/env sh
set -eu

log() {
  printf '%s\n' "$1"
}

warn() {
  printf '%s\n' "[pre-commit] WARNING: $1" >&2
}

ACTIONLINT_VERSION="1.7.12"
ACTIONLINT_LINUX_AMD64_SHA256="8aca8db96f1b94770f1b0d72b6dddcb1ebb8123cb3712530b08cc387b349a3d8"
ACTIONLINT_LINUX_ARM64_SHA256="325e971b6ba9bfa504672e29be93c24981eeb1c07576d730e9f7c8805afff0c6"
ACTIONLINT_DARWIN_AMD64_SHA256="5b44c3bc2255115c9b69e30efc0fecdf498fdb63c5d58e17084fd5f16324c644"
ACTIONLINT_DARWIN_ARM64_SHA256="aba9ced2dee8d27fecca3dc7feb1a7f9a52caefa1eb46f3271ea66b6e0e6953f"
ACTIONLINT_WINDOWS_AMD64_SHA256="6e7241b51e6817ea6a047693d8e6fed13b31819c9a0dd6c5a726e1592d22f6e9"
ACTIONLINT_WINDOWS_ARM64_SHA256="cadcf7ea4efe3a68728893813643cebe1185e5b1d4be5b96245f65c9a4d5ea41"
ACTIONLINT_CMD=""
YAMLLINT_MODE=""

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

download_file() {
  url="$1"
  destination="$2"

  if command -v curl >/dev/null 2>&1; then
    curl -fsSL "$url" -o "$destination"
    return
  fi

  if command -v wget >/dev/null 2>&1; then
    wget -q --tries=5 --waitretry=2 --timeout=30 "$url" -O "$destination"
    return
  fi

  printf '%s\n' "[pre-commit] curl or wget is required to install missing tooling from $url." >&2
  return 1
}

verify_sha256_file() {
  expected_checksum="$1"
  file_path="$2"

  if command -v sha256sum >/dev/null 2>&1; then
    printf '%s  %s\n' "$expected_checksum" "$file_path" | sha256sum -c -
    return
  fi

  if command -v shasum >/dev/null 2>&1; then
    actual_checksum="$(shasum -a 256 "$file_path" | awk '{ print $1 }')"
    if [ "$actual_checksum" = "$expected_checksum" ]; then
      printf '%s\n' "$file_path: OK"
      return 0
    fi
    printf '%s\n' "$file_path: FAILED" >&2
    printf '%s\n' "[pre-commit] Expected SHA256 $expected_checksum but found $actual_checksum." >&2
    return 1
  fi

  printf '%s\n' "[pre-commit] sha256sum or shasum is required to verify $file_path." >&2
  return 1
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

ensure_yamllint_available() {
  if command -v yamllint >/dev/null 2>&1; then
    YAMLLINT_MODE="command"
    return 0
  fi

  if run_python -c "import yamllint" >/dev/null 2>&1; then
    YAMLLINT_MODE="python"
    return 0
  fi

  warn "yamllint is not installed; installing Python tooling requirements..."
  install_python_tooling || return 1

  if command -v yamllint >/dev/null 2>&1; then
    YAMLLINT_MODE="command"
    return 0
  fi

  if run_python -c "import yamllint" >/dev/null 2>&1; then
    YAMLLINT_MODE="python"
    return 0
  fi

  printf '%s\n' "[pre-commit] yamllint is required but could not be installed." >&2
  return 1
}

run_yamllint() {
  if [ "$YAMLLINT_MODE" = "command" ]; then
    yamllint "$@"
    return
  fi

  run_python -m yamllint "$@"
}

ensure_actionlint_available() {
  if command -v actionlint >/dev/null 2>&1; then
    ACTIONLINT_CMD="$(command -v actionlint)"
    return 0
  fi

  os="$(uname -s | tr '[:upper:]' '[:lower:]')"
  machine="$(uname -m)"
  case "$machine" in
    x86_64|amd64) arch="amd64" ;;
    aarch64|arm64) arch="arm64" ;;
    *)
      printf '%s\n' "[pre-commit] Unsupported architecture for actionlint auto-install: $machine" >&2
      return 1
      ;;
  esac

  case "$os" in
    mingw*|msys*|cygwin*) os="windows" ;;
  esac

  archive_extension="tar.gz"
  executable_name="actionlint"
  case "$os:$arch" in
    linux:amd64) checksum="$ACTIONLINT_LINUX_AMD64_SHA256" ;;
    linux:arm64) checksum="$ACTIONLINT_LINUX_ARM64_SHA256" ;;
    darwin:amd64) checksum="$ACTIONLINT_DARWIN_AMD64_SHA256" ;;
    darwin:arm64) checksum="$ACTIONLINT_DARWIN_ARM64_SHA256" ;;
    windows:amd64)
      checksum="$ACTIONLINT_WINDOWS_AMD64_SHA256"
      archive_extension="zip"
      executable_name="actionlint.exe"
      ;;
    windows:arm64)
      checksum="$ACTIONLINT_WINDOWS_ARM64_SHA256"
      archive_extension="zip"
      executable_name="actionlint.exe"
      ;;
    *)
      printf '%s\n' "[pre-commit] Unsupported OS for actionlint auto-install: $os/$arch" >&2
      return 1
      ;;
  esac

  tool_dir="artifacts/pre-commit-tools/actionlint-${ACTIONLINT_VERSION}-${os}-${arch}"
  ACTIONLINT_CMD="${tool_dir}/${executable_name}"
  if [ -x "$ACTIONLINT_CMD" ]; then
    return 0
  fi

  archive="actionlint_${ACTIONLINT_VERSION}_${os}_${arch}.${archive_extension}"
  download_dir="${tool_dir}/download"
  mkdir -p "$tool_dir" "$download_dir"

  log "[pre-commit] Installing actionlint ${ACTIONLINT_VERSION} into ${tool_dir}..."
  download_file "https://github.com/rhysd/actionlint/releases/download/v${ACTIONLINT_VERSION}/${archive}" "${download_dir}/${archive}"
  verify_sha256_file "$checksum" "${download_dir}/${archive}"
  if [ "$archive_extension" = "zip" ]; then
    if command -v unzip >/dev/null 2>&1; then
      unzip -p "${download_dir}/${archive}" "$executable_name" > "$ACTIONLINT_CMD"
    elif command -v pwsh >/dev/null 2>&1; then
      pwsh -NoLogo -NoProfile -Command 'Expand-Archive -LiteralPath $args[0] -DestinationPath $args[1] -Force' "${download_dir}/${archive}" "$tool_dir"
    elif command -v powershell >/dev/null 2>&1; then
      powershell -NoLogo -NoProfile -Command 'Expand-Archive -LiteralPath $args[0] -DestinationPath $args[1] -Force' "${download_dir}/${archive}" "$tool_dir"
    else
      printf '%s\n' "[pre-commit] unzip or PowerShell is required to install actionlint on Windows." >&2
      return 1
    fi
  else
    tar -xzf "${download_dir}/${archive}" -C "$tool_dir" actionlint
  fi
  chmod +x "$ACTIONLINT_CMD"
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
  if ! run_python scripts/lint/check-shell-executable.py; then
    printf '%s\n' "[pre-commit] ERROR: Shell scripts missing executable bit. See message above for fix." >&2
    exit 1
  fi
}

check_shell_python_invocation() {
  log "[pre-commit] Checking shell script Python invocation patterns..."
  if ! run_python scripts/lint/check-shell-python-invocation.py; then
    printf '%s\n' "[pre-commit] ERROR: Shell scripts must use explicit 'python' to invoke .py files. See message above." >&2
    exit 1
  fi
}

check_yaml_lint() {
  yaml_output="$(git diff --cached --name-only --diff-filter=ACM -- '*.yml' '*.yaml' || printf '')"
  if [ -z "$yaml_output" ]; then
    return
  fi

  ensure_yamllint_available
  log "[pre-commit] Running yamllint on staged YAML files..."

  (
    set -f
    old_ifs=$IFS
    IFS='
'
    set -- dummy
    for file in $yaml_output; do
      [ -z "$file" ] && continue
      set -- "$@" "$file"
    done
    shift
    IFS=$old_ifs

    run_yamllint -c .yamllint.yml "$@"
  )
}

check_yaml_lint_all() {
  yaml_output="$(git ls-files '*.yml' '*.yaml' || printf '')"
  if [ -z "$yaml_output" ]; then
    return
  fi

  ensure_yamllint_available
  log "[pre-commit] Running yamllint on tracked YAML files..."

  (
    set -f
    old_ifs=$IFS
    IFS='
'
    set -- dummy
    for file in $yaml_output; do
      [ -z "$file" ] && continue
      set -- "$@" "$file"
    done
    shift
    IFS=$old_ifs

    run_yamllint -c .yamllint.yml "$@"
  )
}

check_github_actions_lint() {
  workflow_output="$(git diff --cached --name-only --diff-filter=ACM -- '.github/workflows/*.yml' '.github/workflows/*.yaml' || printf '')"
  if [ -z "$workflow_output" ]; then
    return
  fi

  ensure_actionlint_available
  log "[pre-commit] Running actionlint on staged GitHub Actions workflows..."

  (
    set -f
    old_ifs=$IFS
    IFS='
'
    set -- dummy
    for file in $workflow_output; do
      [ -z "$file" ] && continue
      set -- "$@" "$file"
    done
    shift
    IFS=$old_ifs

    "$ACTIONLINT_CMD" "$@"
  )
}

check_github_actions_lint_all() {
  workflow_output="$(git ls-files '.github/workflows/*.yml' '.github/workflows/*.yaml' || printf '')"
  if [ -z "$workflow_output" ]; then
    return
  fi

  ensure_actionlint_available
  log "[pre-commit] Running actionlint on tracked GitHub Actions workflows..."

  (
    set -f
    old_ifs=$IFS
    IFS='
'
    set -- dummy
    for file in $workflow_output; do
      [ -z "$file" ] && continue
      set -- "$@" "$file"
    done
    shift
    IFS=$old_ifs

    "$ACTIONLINT_CMD" "$@"
  )
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
  if ! run_python scripts/lint/check-temp-path-usage.py; then
    lint_failed=1
  fi

  # Check for userdata scope violations
  if ! run_python scripts/lint/check-userdata-scope-usage.py; then
    lint_failed=1
  fi

  # Check for console capture violations
  if ! run_python scripts/lint/check-console-capture-semaphore.py; then
    lint_failed=1
  fi

  # Check for finally block violations
  if ! run_python scripts/lint/check-test-finally.py; then
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

if [ "${1:-}" = "--lint-yaml-actions" ]; then
  check_yaml_lint_all
  check_github_actions_lint_all
  log "[pre-commit] YAML and GitHub Actions lint checks completed successfully."
  exit 0
fi

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
check_yaml_lint
check_github_actions_lint
check_test_lint

# Validate LLM skill metadata (strict mode - fail on errors)
log "[pre-commit] Validating LLM skill metadata..."
run_python tools/LlmSkillIndexer/llm_skill_indexer.py --check

log "[pre-commit] Completed successfully."
