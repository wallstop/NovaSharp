# Devcontainer AI Backend Launchers

## Native and alternate providers are process-scoped

- **Fact:** `.devcontainer/ai-backends.sh` leaves `codex` and `claude` native and
  provides opt-in `codex-zai` and `claude-zai` processes. Claude state is
  isolated with `CLAUDE_CONFIG_DIR`; its wrapper removes competing Bedrock,
  Vertex, Foundry, and gateway selectors before setting the Z.ai endpoint.
- **Scope:** NovaSharp, Totem, and Fortress devcontainer tooling as verified on
  2026-09-02.
- **Evidence:** `scripts/lint/test-ai-backends.sh` exercises a hostile provider
  environment, custom `CODEX_HOME`, key absence, argument forwarding, and file
  permissions.
- **Implication:** Never implement a backend toggle by rewriting native config.
  Credentials belong in exported process environment variables, never generated
  files or an intermediate command's arguments.
  Model-server credentials must also be filtered from shell, hook, and stdio MCP
  subprocesses.
- **Fact:** Claude Code 2.1.259's experimental Linux subprocess scrubber forces
  the strong bubblewrap path. `sandbox.enableWeakerNestedSandbox=true` only
  changes `/proc` handling and still requests `--unshare-user`; it cannot repair
  a Docker runtime that denies user or mount namespaces. Persisting the same
  setting in `settings.json` is therefore not a fix.
- **Fact:** `claude-zai` defaults the experimental scrubber off inside an
  existing container and on outside one. The outer devcontainer is the process
  boundary. Explicit `CLAUDE_ZAI_SUBPROCESS_ENV_SCRUB=1` runs a real weak-mode
  bubblewrap preflight and fails before model startup if unsupported.
- **Evidence:** Real Claude Bash red/green oracle, a real stdio MCP environment
  probe, direct `unshare`/bubblewrap probes, disposable Docker security-option
  matrix, inspection of Claude Code 2.1.259's bundled argument construction,
  [Anthropic's sandbox documentation](https://code.claude.com/docs/en/sandboxing),
  and the upstream reports for
  [`SUBPROCESS_ENV_SCRUB` overriding sandbox settings](https://github.com/anthropics/claude-code/issues/50167)
  and [nested Docker failures](https://github.com/anthropics/claude-code/issues/48304).
- **Implication:** Do not grant `SYS_ADMIN`, use a privileged container, disable
  host AppArmor, or set Docker security profiles unconfined just to obtain a
  redundant inner sandbox. Re-run the real Bash and credential-presence oracles
  after Claude upgrades because this implementation is version-sensitive.

## Duplicated launchers must remain synchronized

- **Fact:** The launcher and its regression suite are intentionally duplicated
  byte-for-byte in NovaSharp, Totem, and Fortress.
- **Scope:** `.devcontainer/ai-backends.sh` and the repository-specific
  `test-ai-backends.sh` location.
- **Evidence:** SHA-256 comparison plus all three isolated regression suites.
- **Implication:** Apply behavior changes to all three copies and compare hashes.
  After any `latest` CLI upgrade, rerun the fake-CLI suite and a controlled local
  connection-refusal smoke test to verify provider/model parsing without billing.

## Endpoint and model contracts can change

- **Fact:** Z.ai's Codex integration currently uses the Responses endpoint
  `https://api.z.ai/api/v1`; Claude Code uses
  `https://api.z.ai/api/anthropic`.
- **Scope:** Vendor contracts verified on 2026-09-02; model catalog schema and
  provider-selector variables are version-sensitive.
- **Evidence:** Z.ai Codex and Claude integration documentation, Anthropic Claude
  Code environment-variable documentation, and installed-CLI regression checks.
- **Implication:** Revalidate official endpoints, selector names, and Codex model
  catalog parsing when upgrading the continuously installed `latest` packages.
