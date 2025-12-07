# Lint Scripts

Static analysis helpers that enforce codebase conventions. These scripts run in CI (via `tests.yml`) and can be executed locally to catch issues before pushing.

## Prerequisites

- Python 3.10+
- [ripgrep](https://github.com/BurntSushi/ripgrep) (`rg`) for pattern matching

## Scripts

### check-console-capture-semaphore.py

Ensures tests use the console capture coordination helpers (`ConsoleTestUtilities.WithConsoleCaptureAsync`/`WithConsoleRedirectionAsync`) instead of directly instantiating `ConsoleCaptureScope`/`ConsoleRedirectionScope` or accessing `ConsoleCaptureCoordinator.Semaphore`. This prevents race conditions when multiple tests capture console output concurrently.

```bash
python scripts/lint/check-console-capture-semaphore.py
```

### check-platform-testhooks.py

Validates that `PlatformAutoDetector.TestHooks` is only accessed through the approved infrastructure helpers (`PlatformDetectorScope`/`PlatformDetectorOverrideScope`). Direct access outside these scopes can cause test isolation failures.

```bash
python scripts/lint/check-platform-testhooks.py
```

### check-temp-path-usage.py

Detects tests that call `Path.GetTempPath()` or `Path.GetTempFileName()` directly instead of using the shared `TempFileScope`/`TempDirectoryScope` helpers. The scope helpers ensure proper cleanup and isolation between tests.

```bash
python scripts/lint/check-temp-path-usage.py
```

### check-test-finally.py

Flags `finally` blocks in test code that should use scope-based cleanup patterns instead. Manual `finally` blocks are error-prone and harder to maintain; prefer `TempFileScope`, `SemaphoreSlimScope`, `DeferredActionScope`, or similar disposable scopes.

```bash
python scripts/lint/check-test-finally.py
```

### check-userdata-scope-usage.py

Ensures tests use `UserDataRegistrationScope` rather than calling `UserData.RegisterType`/`UserData.UnregisterType` directly. Direct registration without proper scoping can leak type registrations between tests, causing flaky failures.

```bash
python scripts/lint/check-userdata-scope-usage.py
```

### check-shell-executable.py

Verifies that all `.sh` files in the repository have the executable bit set in git. Scripts without executable permissions cause "permission denied" errors on Linux/macOS CI runners. The fix is `git update-index --chmod=+x <script.sh>`.

```bash
python scripts/lint/check-shell-executable.py
```

### check-shell-python-invocation.py

Ensures shell scripts invoke Python scripts using the explicit `python` interpreter rather than executing them directly. Direct execution (e.g., `./script.py` or `script.py`) requires the Python file to have executable permissions in git, which is fragile across platforms and CI environments.

Correct pattern:

```bash
python "${REPO_ROOT}/scripts/lint/check-foo.py"
```

Incorrect pattern:

```bash
"${REPO_ROOT}/scripts/lint/check-foo.py"  # May fail with "permission denied"
```

```bash
python scripts/lint/check-shell-python-invocation.py
```

## Adding New Lint Scripts

1. Create a Python script following the existing patterns (use `rg` for searching, return exit code 0 on success, 1 on violation).
1. Document the script in this README.
1. Wire it into CI via `.github/workflows/tests.yml` or the appropriate `scripts/ci/*.sh` helper.
1. Update `scripts/README.md` if the script introduces a new category of checks.
