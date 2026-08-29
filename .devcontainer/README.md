# NovaSharp devcontainer maintenance

The devcontainer installs the current npm `latest` dist-tag for these coding
tools:

- `@nanocollective/nanocoder`
- `opencode-ai`
- `@openai/codex`

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
unavailable but all three image-baked tools are intact, startup emits a warning
and continues with those versions. This keeps an existing development container
usable during a registry or network outage without silently treating the tools
as current.

Run the refresh manually with:

```bash
bash .devcontainer/install-npm-tools.sh
```

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

The age-based cleanup uses portable `find -mtime` predicates and works with both
GNU and BSD/macOS command-line tools.

The scripts validate the workspace before deletion and never use `sudo`.
