#!/usr/bin/env bash

set -euo pipefail

pattern="MoonSharp"
message="MoonSharp identifier detected in tracked content. Replace with NovaSharp or document an explicit allowlist."

# Fail fast if any tracked filename still contains the legacy brand.
if git ls-files -- '*MoonSharp*' | grep -q .; then
  echo "MoonSharp-branded filenames detected:"
  git ls-files -- '*MoonSharp*'
  exit 1
fi

# Explicit allowlist for legacy comparison artefacts we intentionally keep.
readarray -t allowlist <<'EOF'
:(exclude)docs/Performance.md
:(exclude)README.md
:(exclude)src/samples/Tutorial/Tutorials/readme.md
:(exclude)moonsharp_DescriptorHelpers.cs
:(exclude)src/tooling/Benchmarks/NovaSharp.Benchmarks/PerformanceReportWriter.cs
:(exclude)scripts/branding/ensure-novasharp-branding.sh
:(exclude)AGENTS.md
:(exclude)src/tooling/NovaSharp.Comparison
:(exclude)src/tooling/NovaSharp.Comparison/**
:(exclude)PLAN.md
EOF

if git grep -n --color=never "${pattern}" -- . "${allowlist[@]}"; then
  echo "${message}"
  echo
  echo "Allowed locations:"
  for pathspec in "${allowlist[@]}"; do
    printf '  - %s\n' "${pathspec#:(exclude)}"
  done
  exit 1
fi

echo "NovaSharp branding check passed."
