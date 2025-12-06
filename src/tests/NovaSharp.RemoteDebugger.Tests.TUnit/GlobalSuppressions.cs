using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Usage",
    "CA1515:Consider making public types internal",
    Justification = "TUnit fixtures and shared harness helpers must stay public for discovery and debugger interop.",
    Scope = "module"
)]
