# Test Scripts

This folder contains quick test execution utilities for NovaSharp development.

## Devcontainer startup

With Docker running and `devcontainer` on PATH, run
`python scripts/test/devcontainer-smoke.py` to build the current committed
checkout and exercise the configured lifecycle. Use `--image IMAGE` to reuse
a local devcontainer image built from that commit with its features. On Windows,
`--temp-parent ..` places the disposable checkout on the same drive as the repo.
The test uses isolated containers, a named NuGet volume, and synthetic credentials.

## quick.sh - Quick Test Runner

Optimized script for fast iterative test execution with filtering support.

### Usage

```bash
# Run all tests
./scripts/test/quick.sh

# Filter by method name pattern
./scripts/test/quick.sh Floor              # Methods containing "Floor"

# Filter by class name pattern
./scripts/test/quick.sh -c MathModule      # Classes containing "MathModule"

# Combined class and method filter
./scripts/test/quick.sh -c Math -m Floor   # Both filters applied

# Skip build step (faster when code unchanged)
./scripts/test/quick.sh --no-build Floor

# Debug configuration
./scripts/test/quick.sh --debug

# List all available tests
./scripts/test/quick.sh --list
```

### Options

| Option       | Short | Description                                        |
| ------------ | ----- | -------------------------------------------------- |
| `--class`    | `-c`  | Filter by class name (classes containing PATTERN)  |
| `--method`   | `-m`  | Filter by method name (methods containing PATTERN) |
| `--no-build` | `-n`  | Skip build step (use pre-built binaries)           |
| `--debug`    | `-d`  | Run Debug configuration tests                      |
| `--list`     |       | List all available tests                           |
| `--help`     | `-h`  | Show help message                                  |

### Performance Tips

1. **Use `--no-build`** when iterating on test logic without code changes
1. **Use specific filters** to run fewer tests for faster feedback
1. **Tests auto-parallelize** via TUnit for maximum throughput

### How It Works

The script uses Microsoft.Testing.Platform's `--treenode-filter` with path-based wildcards:

- `/assembly/namespace/class/method/arguments` format
- `*` wildcards for partial matching
- `**` at the end matches remaining path segments

Example internal filter: `/*/*/*MathModule*/*Floor*/**`
