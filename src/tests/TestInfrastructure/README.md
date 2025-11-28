# Test Infrastructure

This directory centralizes helper code that must be shared across multiple test projects.

- `NUnit/` contains the existing attributes and helpers that plug into the interpreter's NUnit suite (for example the isolation attributes that wrap `UserData`, `Script.GlobalOptions`, and `PlatformAutoDetector`).
- `TUnit/` will host the equivalent implementations for the TUnit suites as they come online so fixtures can retain the exact same isolation semantics without duplicating code inside each project.

Both `NovaSharp.Interpreter.Tests` and `NovaSharp.Interpreter.Tests.TUnit` link files from here via their project files so new helpers only need to be authored once.
