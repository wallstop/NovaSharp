# IL2CPP Spot Check

This sample is a minimal Unity player smoke test for NovaSharp's Unity and IL2CPP path. The scene contains one `IL2CPPSpotCheckRunner` component that:

- compiles a small Lua chunk once;
- warms the call path;
- times pure Lua calls, table mutation, and a simple Lua-to-CLR callback;
- writes one machine-readable player-log line beginning with `NOVASHARP_IL2CPP_SPOTCHECK PASS`.

Build the scene with IL2CPP and inspect the player log for the pass line. The timing is a stopwatch smoke signal, not a BenchmarkDotNet-quality performance gate.
