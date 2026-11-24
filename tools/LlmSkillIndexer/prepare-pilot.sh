#!/usr/bin/env bash
# Prepare one blinded workspace from the repository root.
set -euo pipefail
if [[ $# != 4 ]]; then
  echo 'Usage: bash tools/LlmSkillIndexer/prepare-pilot.sh numeral|bit32|inventory BASELINE_REV GUIDANCE_REV EMPTY_DEST' >&2
  exit 2
fi
case "$1" in
  numeral) source_revision=a906b22f^ ;;
  bit32) source_revision=f31d973e^ ;;
  inventory) source_revision=$2 ;;
  *) echo "Unknown pilot task: $1" >&2; exit 2 ;;
esac
# Use a new directory. A stale trial can reveal the solution to the next agent.
mkdir -- "$4"
git archive "$source_revision" | tar -x -C "$4" \
  --exclude=progress --exclude=PLAN.md --exclude=GOAL.md \
  --exclude=.llm --exclude=.agents --exclude=.claude \
  --exclude=AGENTS.md --exclude=CLAUDE.md --exclude='tools/LlmSkillIndexer/prepare-pilot.sh' \
  --exclude='tools/LlmSkillIndexer/pilot.md' --exclude='tools/LlmSkillIndexer/README.md'
# Domain knowledge stays at the task's source revision to avoid future findings.
git archive "$source_revision" .llm/knowledge | tar -x -C "$4"
git archive "$3" AGENTS.md CLAUDE.md .llm .agents/skills .claude/skills |
  tar -x -C "$4" --exclude='.llm/knowledge'
# Include the same local task and delivery guidance in both conditions.
cat > "$4/PLAN.md" <<'EOF'
# Trial plan

Complete the task from the prompt in this checkout. Record the regression,
implementation, and validation results for the assessor.
EOF
cat > "$4/GOAL.md" <<'EOF'
# Trial goal

## Work Style

Keep changes scoped to the task and validate against the repository's checks.
Use the matching reference Lua for language behavior. Submit the local working
tree and validation results. The assessor handles publication and review.
EOF
# Give clients a project root without any later commit or upstream remote.
git -C "$4" init --quiet
mkdir -- "$4/.git/pilot-hooks"
git -C "$4" -c core.hooksPath=.git/pilot-hooks add --force .
git -C "$4" -c core.hooksPath=.git/pilot-hooks -c commit.gpgSign=false \
  -c user.name=Assessor -c user.email=assessor@example.invalid commit --quiet -m 'Trial input'
