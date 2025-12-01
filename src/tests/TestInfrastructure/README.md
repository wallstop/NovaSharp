# Test Infrastructure

This directory centralizes helper code that must be shared across multiple test projects.

- `TUnit/` hosts the isolation attributes and shared helpers used by every interpreter/remote-debugger fixture. Keep the helpers here so suites can retain identical semantics without duplicating code inside each project.
- The legacy `NUnit/` folder was removed after the final interpreter migration (2025-12-01). Refer to Git history if you need to resurrect those adapters for a new NUnit host.

Both `NovaSharp.Interpreter.Tests` (shared assets only) and `NovaSharp.Interpreter.Tests.TUnit` link files from here via their project files so new helpers only need to be authored once.
