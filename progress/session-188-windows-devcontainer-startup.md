# Session 188 — Windows devcontainer startup

## Root causes

- The Windows CLI passed drive-letter paths to WSL Bash. On the same checkout,
  `bash -n` failed on the expanded script path and found the relative script.
  The old lint test required the broken path literally.
- A synthetic run of the old preflight reported success after `chmod 600`.
  WSL and Docker both saw mode `0777` on this NTFS mount. The native ACL still
  granted local users read access, and Claude's credential reader required `0600`.
- Docker Desktop presented the checkout as root-owned. Git refused to operate
  as `vscode`, including during hook installation.
- During a second clean creation, npm advertised `@openai/codex@0.172.0`
  while its tarball returned HTTP 404 on five attempts. The creation hook
  aborted even though the image already contained the tools. The same tarball
  subsequently returned HTTP 200.

## Changes

The host hook uses the workspace working directory. Bash filters the two
optional credentials and publishes them with native access controls on Windows.
The managed Claude launchers read a project-specific Z.ai credential staged
on the container filesystem at creation. Git trusts the mounted checkout when
its ownership differs from the container user's.

Creation and restart now share the bounded npm refresh. On registry failure,
startup checks all seven installed tools before continuing. Building a fresh
image still requires the requested packages.

The Windows CI fixture launches the configured hook under a path containing
spaces and checks the native ACL. The opt-in Docker test covers the real
lifecycle and uses local MCP test doubles. See the [test instructions](../scripts/test/README.md).

## Local evidence

- Windows 11 build 26300, Dev Containers CLI `0.91.0`, Docker Desktop client
  and engine `29.9.3`. Bash resolved to WSL on the reported launch. The native
  ACL regression also ran under Git for Windows Bash.
- `devcontainer exec --workspace-folder . zsh -il` opened a PTY in the checkout
  as `vscode`, with both the login and interactive shell options set.
- `python scripts/test/devcontainer-smoke.py --temp-parent .. --image novasharp-windows-smoke:session188`
  exited zero on a Windows drive. The clean checkout restored the solution.
  Recreating with synthetic keys repaired a root-owned NuGet volume and both
  managed launchers delivered the expected key to local MCP doubles. The next
  start preserved cache ownership. Recreating after key removal cleared the
  Docker environment and the staged credential. Docker's exported build context
  and the captured diagnostics contained no synthetic keys.
- The Linux interpreter build and quick test suite completed with 15,716 tests
  and zero failures. The Release solution build reported zero warnings and
  errors. CSharpier checked 828 files.
