using System.Diagnostics.CodeAnalysis;
using TUnit.Core;

// Use reflection mode since source generation is disabled for faster builds
[assembly: ReflectionMode]

[assembly: SuppressMessage(
    "Usage",
    "CA1515:Consider making public types internal",
    Justification = "TUnit fixtures and shared harness helpers must stay public for discovery and debugger interop.",
    Scope = "module"
)]
