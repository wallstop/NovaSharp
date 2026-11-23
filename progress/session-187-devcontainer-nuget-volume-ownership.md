# Session 187: Devcontainer NuGet volume ownership

## Goal

Make devcontainer startup reliable for fresh checkouts and existing NuGet
volumes. Keep the GitHub and Z.ai integrations optional and their credentials
out of image layers, shell traces, and unrelated MCP child processes. Validate
the startup paths and land the change through PR CI. Track the Docker-backed
regression harness in [#150](https://github.com/wallstop/NovaSharp/issues/150).

## Root cause

`devcontainer.json` mounts the `novasharp-nuget-cache` named volume at
`/home/vscode/.nuget`, but the image did not create that path. Docker initializes
an empty named volume from the image only when the mount point exists; otherwise
it creates the volume as `root:root 0755`. Lifecycle commands run as `vscode`, so
NuGet could not create/read `~/.nuget/NuGet/NuGet.Config` and reported `Failed to
read NuGet.Config due to unauthorized access`. Both `onCreateCommand` and
`updateContentCommand` then retried a permanent permissions failure.

## Fix

Defense in depth, matching Docker's named-volume first-use semantics:

- `.devcontainer/Dockerfile` now seeds `/home/vscode/.nuget` with `vscode`
  ownership, so fresh volumes are initialized correctly. The devcontainer CLI's
  UID-remap layer also chowns `/home/vscode`, so the seed survives
  `updateRemoteUserUID`.
- `.devcontainer/ensure-nuget-cache.sh` repairs an existing volume whose mount
  is not writable by the runtime user. It uses the dynamic UID/GID (not a
  hardcoded `1000`), only escalates through `sudo -n` when needed, creates a
  missing directory without privilege when `$HOME` is writable, and prints the
  volume-recreation remediation when passwordless sudo or the mount is
  unavailable. It is idempotent when the cache is healthy.
- The guard runs before every lifecycle step that invokes `dotnet`:
  `on-create.sh`, `update-content.sh`, `post-create.sh`, and `post-start.sh`.
- `check-tooling-consistency.py` now enforces the image seed's `vscode`
  ownership, the uncommented mount path, and command-position guard ordering.
  `test-devcontainer-lifecycle.sh` covers healthy, repair, unreadable-config,
  dynamic-UID, missing-directory-without-sudo, and denied-sudo paths.

## MCP startup

The host preflight generates a private runtime env file containing only the
configured GitHub and Z.ai keys. A checkout without credentials gets an empty
file. The old copy remains readable until the replacement is ready, so another
launch can consume it during preflight. An invalid input stops the current
launch. The Claude and Codex Z.ai wrappers bridge the affected remote servers
through stdio. Native GitHub requests continue to request read-only access.

## Evidence

- A temporary root-owned cache made real NuGet report unauthorized access.
  Running the guard as UID 1000 restored access. The second guard run exited
  silently without additional repair.
- Focused regression cases initially rejected credential-free startup and
  accepted a directory in place of `NuGet.Config`. The fixes preserve optional
  startup and diagnose the malformed cache before restore.
- A loopback MCP server received the synthetic authorization header through
  `mcp-remote` 0.16.7. The fake token stayed out of its diagnostics and scratch
  configuration files.
- The interpreter build and quick test suite succeeded (15,716 tests).
- Docker was unavailable for this session's local end-to-end startup test.
  The container-level follow-up is tracked in [#150](https://github.com/wallstop/NovaSharp/issues/150).
