# Session 190 — Shared agent guidance

The two root policies diverged on host API compatibility. The navigation guide
also recommended running the index generator to list skills. Against the old
files, both new discovery tests failed. Against the shared policy and read-only
inventory, all 21 indexer tests succeeded. The inventory matched the canonical
directories and preserved the index bytes.

Claude imports the root policy. OpenCode and Codex use the same repository
policy and canonical skill tree. The upstream reference validator accepted all
32 packages and rejected a control missing its description. Repository metadata
and index freshness remain separate checks.

The root policy changed from 3,201 to 2,044 bytes and the optional repository
guide from 14,060 to 4,534 bytes (LF). These are file measurements. Session
token consumption and task success require paired frontend runs.

The [Agent Skills specification](https://agentskills.io/specification) informed
the distinction between package validation and discovery. The
[upstream evaluation guide](https://agentskills.io/skill-creation/evaluating-skills)
informed paired task grading. Keep the current set of skills until held-out
outcomes justify changing their triggers or grouping.

The quick build succeeded and the full quick suite reported 15,716 successful
tests. Across baseline and candidate, OpenCode 1.19.6 and Claude
Code 2.1.331 each advertised the 32 project skills. Codex 0.172.0's candidate
prompt contained the shared policy and all 32 names. Claude's local context
diagnostic estimated 1.1k memory tokens on baseline and 685 for the final
candidate (4 for the import and 681 for the shared policy).

All three [pilot inputs](../tools/LlmSkillIndexer/pilot.md) could be prepared.
The inventory task produced no scored model pair. The Claude model request
reported a missing login. OpenCode's configured provider returned a server
error. Both managed Z.ai launchers reported a missing API key. Codex's baseline
trial timed out after 120 seconds. Its candidate trial attempted the inventory,
but Linux denied namespace creation before the command executed. The same
namespace restriction and unavailable trial container prevented isolated
historical model runs. Task correctness, cost, and end-to-end token savings
remain unmeasured.
