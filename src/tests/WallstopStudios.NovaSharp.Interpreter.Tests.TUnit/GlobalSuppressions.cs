using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Usage",
    "CA1515:Consider making public types internal",
    Justification = "TUnit fixtures and shared harness helpers must remain public for discovery and analyzer parity with the NUnit suite.",
    Scope = "module"
)]
[assembly: SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "Shared NUnit test utilities are linked for reuse; the types are instantiated by fixtures once they migrate.",
    Scope = "type",
    Target = "~T:NovaSharp.Interpreter.Tests.TestUtilities.FakeTimeProvider"
)]
[assembly: SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "Shared NUnit test utilities are linked for reuse; the types are instantiated by fixtures once they migrate.",
    Scope = "type",
    Target = "~T:NovaSharp.Interpreter.Tests.TestUtilities.FakeHighResolutionClock"
)]
[assembly: SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "Shared NUnit test utilities are linked for reuse; the types are instantiated by fixtures once they migrate.",
    Scope = "type",
    Target = "~T:NovaSharp.Interpreter.Tests.TestUtilities.RemoteDebuggerHarness"
)]
