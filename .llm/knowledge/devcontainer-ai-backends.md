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
  Do not put credentials in generated profiles or an intermediate command's
  arguments. Ordinary shell, hook, and unrelated MCP children must not receive
  model credentials; a trusted MCP may receive only its required key.
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

## Project MCP catalogs and runtime secrets

- **Fact:** As verified on 2026-09-03, GitHub plus Z.ai Vision, Web Search Prime,
  Web Reader, and Zread are projected into each frontend's native project format.
  Nanocoder requires `NANOCODER_MCPSERVERS_FILE` because it does not search parent
  directories for `.mcp.json`.
- **Fact:** A host preflight allowlists `GITHUB_PERSONAL_ACCESS_TOKEN` and
  `ZAI_API_KEY` from ignored `.env.local` into an ignored Docker env file.
  The workspace-relative host hook uses native Windows ACLs on Windows drives
  and mode `0600` on POSIX storage. It atomically publishes after validation so a concurrent launch
  can open the previous file. A failed preflight stops that launch. A missing
  source publishes an empty file, allowing credential-free devcontainer startup.
- **Fact:** The GitHub server sends `X-MCP-Readonly: true` in every frontend.
  The Vision wrapper maps only the required Z.ai alias and scrubs the canonical
  Z.ai key and GitHub token before executing the package.
- **Evidence:** Cross-repository hostile preflight tests, real frontend parsers,
  authenticated MCP initialization, cwd-shift probes, and a Claude tool-list
  oracle showing no GitHub write tools.
- **Implication:** Keep `.env.local*` and generated runtime variants ignored and
  excluded from Docker build contexts. Treat the mounted repository and agent
  process as trusted: runtime keys are not a sandbox boundary. Remove the
  read-only header only for a deliberate, least-privilege mutation workflow.

## Claude and Codex remote MCP need a version-scoped compatibility bridge

- **Fact:** As verified with Claude Code 2.1.260 on 2026-09-04, Claude's native
  HTTP MCP client rejects the SSE-framed initialization result returned by
  Z.ai's Web Search Prime, Web Reader, and Zread endpoints. The same key and
  endpoint succeed through raw HTTP initialization, OpenCode, and `mcp-remote`.
- **Fact:** Codex 0.153.2 reaches the same servers and accepts initialization,
  but its Streamable HTTP transport closes while sending the subsequent
  `initialized` notification. A fresh session using the stdio bridge produced
  no MCP startup errors and proceeded to the independently failing model quota.
- **Fact:** Claude 2.1.260 also does not forward its reserved Anthropic auth
  variable to the configured stdio MCP child. The Z.ai wrappers therefore parse
  a private credential file selected by the canonical `CLAUDE_PROJECT_DIR`.
  The managed image stages only the Docker-injected Z.ai key on its Linux
  filesystem during creation. Windows bind mounts can report mode `0777` even
  for host files protected by a private NTFS ACL.
- **Fact:** `claude-zai` uses a private MCP catalog and Codex uses its project
  catalog to bridge only the three affected remote servers through `mcp-remote`.
  Vision remains direct stdio and GitHub remains native HTTP. Codex forwards only
  `ZAI_API_KEY` through the documented per-server `env_vars` allowlist; the
  wrapper validates it and scrubs all credentials before starting the bridge.
  Standalone POSIX wrappers read the filtered runtime file below `CLAUDE_PROJECT_DIR`.
  The presence of `CLAUDE_PROJECT_DIR` makes that file authoritative: an absent,
  malformed, symlinked, or over-readable file fails closed and never falls back
  to an ambient key.
  Its isolated settings approve exactly the five audited project server names.
- **Fact:** In 2.1.260, the `claude mcp list` management subcommand ignores the
  launcher's per-session strict MCP catalog and instead health-checks project
  `.mcp.json`. It can therefore report native HTTP failures while a real session
  has all five private-catalog servers connected. Treat the streamed
  `system/init.mcp_servers` result as the runtime oracle.
- **Evidence:** Authenticated HTTP/2 initialization returned status 200 and MCP
  protocol `2024-11-05`; OpenCode connected to all five servers; native Claude
  reported Zod `invalid_union` failures for the three remote Z.ai servers;
  Claude debug output showed the missing child credential; and the bridge then
  connected to the same endpoints. See [Claude MCP configuration](https://code.claude.com/docs/en/mcp),
  [the upstream native SSE failure](https://github.com/anthropics/claude-code/issues/58759),
  and [`mcp-remote`](https://github.com/geelen/mcp-remote).
- **Implication:** Do not diagnose this signature as container networking or
  rotate a working key. Keep native HTTP in clients where the authenticated
  handshake succeeds. After each Claude or Codex upgrade, run a real
  authenticated native-HTTP session through the `initialized` notification;
  an HTTP 200 initialization response alone is insufficient. Remove that
  client's bridge only after full session startup succeeds. After each
  `mcp-remote@latest` upgrade, repeat the bridged session probe. Preserve hostile
  wrapper tests for argv/log leakage, source precedence, key scrubbing, symlink
  rejection, and missing credentials. See
  [Codex MCP configuration](https://developers.openai.com/codex/mcp/).
- **Fact:** MCP transport health and model-plan capacity are independent. The
  2026-09-04 Totem session initialized all five MCPs before the Z.ai model API
  returned HTTP 429/code `1310` for exhausted weekly/monthly allowance. Claude's
  retries made the prompt look hung.
- **Implication:** For `1310`, stop the retry loop and inspect the Z.ai plan's
  reset/capacity. Do not rebuild the container, rotate a working key, or change
  MCP transport. Diagnose native MCP parsing, actual session MCP status, and
  model quota as three separate failure domains.

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
