#!/usr/bin/env sh
set -eu

log() {
  printf '%s\n' "$1"
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

    git add -- "$@"
  )
}

format_markdown_files() {
  md_output="$(staged_files_for_pattern '*.md' || printf '')"
  if [ -z "$md_output" ]; then
    log "[pre-commit] No staged Markdown files detected; skipping Markdown checks."
    return
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
    git add -- "$@"
  )
}

update_documentation_audit_log() {
  log "[pre-commit] Refreshing documentation audit log..."
  run_python tools/DocumentationAudit/documentation_audit.py --write-log documentation_audit.log
  if [ -f documentation_audit.log ]; then
    git add documentation_audit.log
  fi
}

update_naming_audit_log() {
  log "[pre-commit] Refreshing naming audit log..."
  run_python tools/NamingAudit/naming_audit.py --write-log naming_audit.log
  if [ -f naming_audit.log ]; then
    git add naming_audit.log
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
  git add src/tests/NovaSharp.Interpreter.Tests/FixtureCatalogGenerated.cs
}

repo_root="$(git rev-parse --show-toplevel 2>/dev/null || printf '')"
if [ -z "$repo_root" ]; then
  printf '%s\n' "pre-commit hook must run inside the repo." >&2
  exit 1
fi

cd "$repo_root"

ensure_tool_on_path "git" "Git"
ensure_tool_on_path "dotnet" ".NET SDK"

log "[pre-commit] Restoring dotnet tools (CSharpier)..."
dotnet tool restore >/dev/null

format_all_csharp_files
format_markdown_files
update_documentation_audit_log
update_naming_audit_log
update_fixture_catalog

log "[pre-commit] Completed successfully."


