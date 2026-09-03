# NovaSharp devcontainer maintenance

The devcontainer installs the current npm `latest` dist-tag for these coding
tools:

- `@nanocollective/nanocoder`
- `opencode-ai`
- `@openai/codex`
- `@anthropic-ai/claude-code`

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
unavailable but all four image-baked tools are intact, startup emits a warning
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

Provide the Z.ai credential through a private environment or secret store:

```bash
export ZAI_API_KEY="..."
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
`CLAUDE_ZAI_SONNET_MODEL`, and `CLAUDE_ZAI_OPUS_MODEL`. Harness shell and MCP
subprocesses do not inherit the model-server credential. Reinstall the launchers
manually after moving the checkout:

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
Claude still removes the model-server credential from those children.

Outside a container, the launcher retains the stronger scrubber by default. Set
`CLAUDE_ZAI_SUBPROCESS_ENV_SCRUB=1` to request it explicitly; the launcher runs a
real bubblewrap preflight and exits with actionable guidance before starting a
model session when the runtime cannot support it. Do not add `SYS_ADMIN`, run a
privileged container, or globally weaken host AppArmor merely to make this
optional second sandbox work.

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

## GitHub MCP

GitHub's hosted MCP server is configured for every agent harness used by this
repository:

- `.vscode/mcp.json` configures VS Code and Copilot Chat with GitHub OAuth.
- `.codex/config.toml` configures the Codex CLI and Codex extension.
- `.mcp.json` configures Claude Code, Copilot CLI, and Nanocoder.
- `opencode.json` configures OpenCode.

No credential is stored in the repository or image. Before opening the
devcontainer, export a least-privilege GitHub token on the host so VS Code can
forward it to the terminal harnesses:

```bash
export GITHUB_PERSONAL_ACCESS_TOKEN="$(gh auth token)"
```

If the container is already open, run the same command in its terminal before
starting Codex, Claude Code, Copilot CLI, OpenCode, or Nanocoder. In VS Code,
open `.vscode/mcp.json`, select **Start**, and then select **Auth** to complete
GitHub OAuth. Copilot CLI also includes a read-only GitHub MCP server by default;
the project entry is deliberately named `github-mcp-server` so the authenticated
project configuration replaces that built-in definition instead of duplicating
it.

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
