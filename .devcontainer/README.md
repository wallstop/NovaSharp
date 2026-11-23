# NovaSharp devcontainer maintenance

Host initialization requires Bash. From PowerShell in the checkout, start the
container and open an interactive login shell with:

```powershell
npx --yes --package=@devcontainers/cli -- devcontainer up --workspace-folder "$PWD"
npx --yes --package=@devcontainers/cli -- devcontainer exec --workspace-folder "$PWD" zsh -il
```

Run the second command after the first succeeds. The host hook runs relative to
the workspace, so either WSL Bash or Git Bash can find it on a Windows drive.
Docker receives its env-file path in the host's native format.

When cloning with Git for Windows, enable symlink support and use
`git clone -c core.symlinks=true` with the repository URL. This preserves the
two skill-directory links checked by the project's pre-commit hook.

The devcontainer installs the current npm `latest` dist-tag for these coding
tools:

- `@nanocollective/nanocoder`
- `opencode-ai`
- `@openai/codex`
- `@anthropic-ai/claude-code`
- `@github/copilot`
- `mcp-remote`
- `@z_ai/mcp-server`

The image build installs the current Node.js LTS through the base image's NVM
setup, then installs the tools as the `vscode` user. Container creation and every
subsequent start resolve the same `latest` tags again and update only tools whose
installed versions differ. The npm global prefix and cache must be writable by
the current user; setup fails with a remediation message instead of invoking
`sudo` or creating mixed-ownership installs.

Dev Containers may remap the `vscode` UID to match the host. Existing global npm
package directories retain group write access through the stable `nvm` group.
The installed files remain read-only to the group; npm needs directory write
access because it stages existing package contents during replacement. The image
build checks the complete directory tree with a synthetic remapped UID.

Dev Containers passes `--no-cache` to every image build. This makes builds
slower, but ensures VS Code rebuilds and `devcontainer build` cannot preserve an
obsolete package behind npm's mutable `latest` tag. A direct Docker build does
not read `devcontainer.json`; pass `--no-cache` when building the Dockerfile
outside VS Code.

Tool refreshes use a dedicated cache under the user's cache directory. A
successful update clears downloaded CLI tarballs without deleting the cache used
by ordinary project-level npm commands.

Image builds and first-create refreshes fail if npm cannot provide the requested
latest releases. Restart checks are deliberately bounded: if npm is temporarily
unavailable but all seven image-baked tools are intact, startup emits a warning
and continues with those versions. This keeps an existing development container
usable during a registry or network outage without silently treating the tools
as current.

Run the refresh manually with:

```bash
bash .devcontainer/install-npm-tools.sh
```

## Native and Z.ai backends

The ordinary `codex` and `claude` commands keep their native OpenAI and
Anthropic backends. The setup hook also installs opt-in `codex-zai` and
`claude-zai` launchers. Switching is per process; no command rewrites the native
configuration.

To use the optional integrations, add the credentials you need before
rebuilding. A checkout can start without `.env.local`:

```bash
[ -e .env.local ] || cp .env.local.example .env.local
# Add or update ZAI_API_KEY and GITHUB_PERSONAL_ACCESS_TOKEN.
codex-zai
claude-zai
```

Exiting either process returns to the native commands immediately. Codex uses a
named `devcontainer-zai` profile and Z.ai's Responses endpoint. Claude uses Z.ai's
Anthropic endpoint and a separate `~/.claude-zai` state directory so native
credentials and sessions remain isolated. The credential is never written to a
repository or generated profile.

Optional per-process overrides are `CODEX_ZAI_MODEL`,
`CODEX_ZAI_REASONING_EFFORT` (`low`, `high`, or `max`),
`ZAI_API_TIMEOUT_MS` (default: five minutes), and
`CLAUDE_ZAI_CONFIG_DIR`. Claude's `haiku`, `sonnet`, and `opus` aliases map to
Z.ai's recommended GLM models; override them with `CLAUDE_ZAI_HAIKU_MODEL`,
`CLAUDE_ZAI_SONNET_MODEL`, and `CLAUDE_ZAI_OPUS_MODEL`. Ordinary shell and
unrelated MCP children do not inherit the model-server credential. The trusted
Z.ai MCP servers receive only their required credential. For Claude, their
wrappers use `CLAUDE_PROJECT_DIR` to select a private, workspace-specific copy
of the Z.ai key in the container user's home directory. Codex explicitly
forwards only `ZAI_API_KEY` through its stdio MCP allowlist.
The wrappers scrub the canonical key and unrelated tokens before starting the
MCP process.
Reinstall the launchers manually after moving the checkout:

```bash
bash .devcontainer/ai-backends.sh install
```

Claude's experimental Linux subprocess scrubber uses `bubblewrap` and `socat`;
both are baked into the devcontainer. Current Claude Code still asks bubblewrap
to create a user and mount namespace even with
`sandbox.enableWeakerNestedSandbox=true`, which Docker's default seccomp and
AppArmor profiles reject. In an existing devcontainer, `claude-zai` therefore
defaults `CLAUDE_CODE_SUBPROCESS_ENV_SCRUB=0` and relies on the outer container
as the process boundary. Controlled Bash and stdio MCP probes verified that
Claude still removes the model-server credential from ordinary shell children
while deliberately authenticating the selected Z.ai MCP servers.

Outside a container, the launcher retains the stronger scrubber by default. Set
`CLAUDE_ZAI_SUBPROCESS_ENV_SCRUB=1` to request it explicitly; the launcher
runs a real bubblewrap preflight and exits with actionable guidance before
starting a model session when the runtime cannot support it. Do not add
`SYS_ADMIN`, run a privileged container, or globally weaken host AppArmor merely
to make this optional second sandbox work.

## Agent network reliability

OpenCode, Nanocoder, and the other terminal agents use the container's ordinary
outbound network path. On Linux, Docker's default bridge masquerades that traffic
through the host; changing the container DNS server or switching to host-network
mode does not repair a failing host uplink.

Do not keep wired Ethernet and Wi-Fi active at the same time when both addresses
are on the same IPv4 subnet. The observed failure pattern is consistent with
Linux's weak-host addressing model creating ambiguous ARP or return paths; a
packet capture is required to distinguish the exact mechanism. The symptom is
especially misleading for streaming model requests: short DNS and TLS probes
may pass, then a long-lived stream resets or stops receiving chunks.

Check the host before debugging the container:

```bash
ip -brief -4 address
ip -4 route show default
ip -4 route show scope link
nmcli device status
model_ip=$(getent ahostsv4 api.z.ai | awk 'NR == 1 {print $1}')
ip route get "$model_ip"
```

If two physical interfaces have addresses and connected routes for the same
subnet, test each one in isolation. Replace the interface and URL as needed:

```bash
wired_interface=eth0
gateway_ip=192.168.1.1
ping -I "$wired_interface" -c 15 "$gateway_ip"
curl --interface "$wired_interface" --ipv4 --connect-timeout 3 --max-time 10 \
    --show-error \
    --write-out '\ncode=%{http_code} total=%{time_total} error=%{errormsg}\n' \
    https://api.z.ai/api/coding/paas/v4/models
```

An expected HTTP 401 proves only DNS, TCP, TLS, and HTTP reachability. It does
not prove that an authenticated streaming request will remain healthy.

The durable, low-complexity fix is to leave only one interface active on that
LAN. For a workstation that should prefer wired Ethernet, disable automatic
activation for the Wi-Fi profile and disconnect it:

Do this from a local terminal. If the shell is remote, do not disconnect the
interface carrying its route. Verify the alternate link first and keep the
restore commands below available before changing NetworkManager state.

```bash
nmcli connection modify "<wifi-profile>" connection.autoconnect no
nmcli connection down "<wifi-profile>"
```

Restore Wi-Fi later with:

```bash
nmcli connection modify "<wifi-profile>" connection.autoconnect yes
nmcli connection up "<wifi-profile>"
```

If automatic failover is required, install the repository's event-driven user
service. It disables the Wi-Fi radio while an eligible Ethernet interface is
connected and restores it only when the service itself disabled it:

```bash
# Allow the preferred Wi-Fi profile to reconnect after Ethernet goes away.
nmcli connection modify "<wifi-profile>" connection.autoconnect yes
bash .devcontainer/host-network/install-user-service.sh
```

Edit `~/.config/prefer-wired-network/config` to disable the policy, restrict it
to named Ethernet interfaces, or suppress automatic restoration, then restart
it with `systemctl --user restart prefer-wired-network.service`. Run the
deterministic branch tests with:

```bash
bash .devcontainer/host-network/test-prefer-wired-network.sh
```

To remove the policy without leaving a radio disabled by it:

```bash
systemctl --user disable --now prefer-wired-network.service
~/.local/libexec/prefer-wired-network --restore
```

This no-root service is the practical workstation equivalent of NetworkManager's
root-owned dispatcher example during an active local login. Default Polkit rules
usually prevent a lingering user service from changing the radio before login.
For boot-time or headless failover, a system administrator should instead install
the official dispatcher pattern under `/etc/NetworkManager/dispatcher.d`. Route
metrics alone select a preferred default route but do not remove same-subnet ARP
ambiguity.

The repository's Z.ai OpenCode provider also sets a five-minute per-attempt
request timeout and a 45-second streamed-chunk timeout in `opencode.json`.
`.opencode/plugins/bounded-network-retries.js` stops network retries after the
third failed attempt or two minutes after the first retry begins, whichever
happens first. These are containment, not a substitute for fixing the host: they
turn a silent broken path into a bounded OpenCode error. They do not control
Nanocoder. Nanocoder 1.30 uses a 120-second hosted-provider socket, header, and
between-chunk timeout with two retries by default; `requestTimeout` and
`socketTimeout` can be set per provider in the user's `agents.config.json` when
a different bound is needed. The single-uplink host repair covers both clients.

After repairing the host, repeat interface-bound and in-container probes, then
run authenticated streaming and read-only tool workloads in both clients.
Record attempt counts, latency, and failure class without recording credentials
or response bodies.

## Project MCP servers

The devcontainer exposes GitHub's hosted MCP server plus every MCP currently
listed by the Z.ai Coding Plan: Vision Understanding, Web Search Prime, Web
Reader, and Zread. The Vision package is installed in the image and refreshed
with the other npm agent tools; the three remote Z.ai servers use their official
HTTPS endpoints.

Claude Code 2.1.260 cannot consume the SSE-framed MCP initialization response
currently returned by those three endpoints. Codex 0.153.2 completes the
response but loses the transport while sending the subsequent `initialized`
notification. Direct HTTP probes and OpenCode succeed from the same container,
so `claude-zai` and Codex use the installed `mcp-remote` stdio bridge only for
those three servers. Vision remains direct stdio and GitHub remains native HTTP.
Re-test native HTTP after either client upgrades before removing its bridge.

In Claude Code 2.1.260, `claude-zai mcp list` ignores this per-session strict
catalog and health-checks `.mcp.json`, so it can falsely report the three native
HTTP failures. The authoritative check is the `system/init` event from an actual
`claude-zai --print --output-format stream-json --verbose` session: its
`mcp_servers` array must show all five entries as `connected`.

Keep model access separate from MCP health. In the 2026-09-04 live probe, all
five MCPs connected before Z.ai's model API returned HTTP 429, code `1310`, for
an exhausted weekly/monthly subscription allowance. Claude retries that response
and can look hung. A rebuild, key rotation, or MCP change cannot repair quota;
interrupt the retries, check the Z.ai plan's usage/reset time, and retry after
capacity is available.

The same catalog is configured for every agent frontend used by this repository:

- `.vscode/mcp.json` configures VS Code and Copilot Chat.
- `.codex/config.toml` configures the Codex CLI and Codex extension.
- `.mcp.json` configures Claude Code, Copilot CLI, and Nanocoder.
- `opencode.json` configures OpenCode.

Nanocoder receives the catalog's absolute path, so launching it below the
repository root still discovers every server.

No credential is committed or stored in an image layer. Before container
creation, the host preflight reads only the two allowlisted values from the
ignored `.env.local` and atomically publishes an ignored Docker env file.
The preflight restricts access to both files with an owner-only Windows ACL on
Windows drives and mode `0600` on POSIX filesystems. Each credential is optional.
A failed preflight stops the launch and preserves the last published contents.
Docker captures the environment at creation. After adding, changing, or removing
a credential, recreate the container with **Dev Containers: Rebuild Container**
or `devcontainer up --workspace-folder . --remove-existing-container`.

```bash
[ -e .env.local ] || cp .env.local.example .env.local
# Add or update GITHUB_PERSONAL_ACCESS_TOKEN and ZAI_API_KEY, then:
code .
# Select “Dev Containers: Rebuild Container” if one already exists.
```

Use a least-privilege GitHub token. Every project definition sends
`X-MCP-Readonly: true` by default, so unattended/YOLO sessions cannot invoke
GitHub write tools. Remove that header only when intentional GitHub mutations
are required; the token's permissions then define the available writes. The two
keys enter the trusted container process environment and remain readable from
the mounted workspace, so do not run untrusted repository code or agents.
The isolated `claude-zai` launcher pre-approves only the five audited names;
native Claude Code still asks before trusting project MCP definitions. Codex
loads them only after the repository itself is trusted. The project entry
replaces Copilot CLI's built-in read-only GitHub definition instead of creating
a duplicate.

Sources: [Z.ai Coding Plan MCP overview](https://docs.z.ai/devpack/overview),
[Claude Code MCP configuration](https://code.claude.com/docs/en/mcp),
[Codex MCP configuration](https://developers.openai.com/codex/mcp/),
[Claude Code native SSE failure](https://github.com/anthropics/claude-code/issues/58759),
[`mcp-remote`](https://github.com/geelen/mcp-remote),
[GitHub MCP server](https://github.com/github/github-mcp-server), and
[Dev Container environment files](https://code.visualstudio.com/remote/advancedcontainers/environment-variables).

## NuGet cache volume ownership

`novasharp-nuget-cache` is mounted at `/home/vscode/.nuget`. Docker only
copies ownership from the image when it initializes an empty named volume. If
the mount point is absent from the image, Docker creates the volume as
`root:root`, and the non-root `vscode` user cannot create
`~/.nuget/NuGet/NuGet.Config`. NuGet reports that failure as
`Failed to read NuGet.Config due to unauthorized access`.

The image seeds `/home/vscode/.nuget` with `vscode` ownership so new volumes
start correct. `ensure-nuget-cache.sh` then runs before every lifecycle step
that invokes `dotnet`; it repairs an existing volume whose owner no longer
matches the current runtime UID, including after `updateRemoteUserUID` or when
the volume predates the image seed. The repair uses only the current UID/GID
and is skipped when the cache is already writable.

If passwordless `sudo` is unavailable, or the volume is mounted read-only, stop
the container and recreate the disposable cache volume, then rebuild so the
image seed applies:

```bash
docker volume rm novasharp-nuget-cache
```

`docker volume rm` fails while a container is still using the volume.

## Generated artifact retention

Container creation removes all ignored build output under `artifacts/`,
`BenchmarkDotNet.Artifacts/`, and `src/**/{bin,obj}`. Every container start then
removes generated files and build directories older than seven days. Override
the start-time retention period when older diagnostics must remain available:

```bash
NOVA_ARTIFACT_RETENTION_DAYS=30 bash .devcontainer/post-start.sh "$(pwd)"
```

The persistent NuGet volume is intentionally outside these generated output
trees, so lifecycle cleanup does not force package downloads on every start. The
previous `artifacts/build-cache` volume was removed because no build entrypoint
used it. Cleanup preserves that path so an already-created container can migrate
without trying to delete a mounted volume.

Run either cleanup mode manually with:

```bash
bash .devcontainer/cleanup-artifacts.sh "$(pwd)" --older-than-days 7
bash .devcontainer/cleanup-artifacts.sh "$(pwd)" --all
```

The age-based cleanup creates an exact cutoff timestamp with Python and compares
it using the portable `find -newer` predicate, avoiding GNU/BSD age-rounding
differences.

The scripts validate the workspace before deletion and never use `sudo`.
