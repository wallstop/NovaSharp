# NovaSharp Plan

This file is the execution queue. Repository priorities and closure gates live in
[`.llm/context.md`](.llm/context.md); planning rules live in the
[plan-maintenance skill](.llm/skills/plan-maintenance/SKILL.md). Design evidence
belongs in domain documentation, completed work in `progress/`, and the full
backlog in [GitHub issues](https://github.com/wallstop/NovaSharp/issues).

## Now

1. Advance Phase A5 call paths in
   [#108](https://github.com/wallstop/NovaSharp/issues/108):
   - Migrate each remaining legacy CoreLib registration in its own scoped change:
     Table, Load, Utf8, OsSystem, StringPack, MetaTable, ErrorHandling,
     TableIterators, OsTime, Json, and Dynamic.
   - Add the return-buffer writer and confine tuple arrays to escaped varargs or
     multi-return values.
   - Keep ordinary hot call, return, and yield paths free of exception-driven
     control flow.
   - Profile a compact hot/cold frame split before renewing the rejected full
     [inline-frame experiment](docs/performance/memory-cache-retention-research.md).
   - Exit when fib/Hanoi are within 2–3x of NLua, one-result calls allocate 0 B
     after warmup, `new Script()` retains less than 100 KiB, and coroutine
     create/resume is near-zero allocation.
1. Close the post-`LuaValue` table measurements in
   [#93](https://github.com/wallstop/NovaSharp/issues/93), then ratchet the hosted
   allocation baseline in [#92](https://github.com/wallstop/NovaSharp/issues/92).
1. Finish the
   [B1 source-generator MVP](docs/proposals/roslyn-hardwire-generator.md): async
   suspension output, the NS0002 code fix after adapter contracts settle,
   Hardwire retirement at parity, LuaLS stubs, a single
   [machine-readable API authority](docs/proposals/runtime-research-gates.md#machine-readable-api-authority),
   and reflection-free trimmed-publish evidence.

## Next

1. A1d: benchmark `LuaValue` copy/layout and measured inlining or `in`/`ref`
   changes; retain only demonstrated wins.
1. A2: pack instructions and constants into contiguous chunk storage, version the
   binary format, and prove round-trip and rejection behavior with chunk memory at
   least 4x smaller plus a measurable branch-heavy dispatch win.
1. A3: split plain and instrumented VM loops; move sandbox work to loop edges and
   allocation sites without weakening limits, debugger behavior, or Unity safety;
   keep plain dispatch within about 2x of Lua-CSharp with zero non-debug
   instrumentation tax, document fuel as basic-block-granular with tests that trip
   within `limit + K`, and close the applicable
   [sandbox threat-model](docs/security/sandbox-threat-model.md) controls.
1. A4.5: evaluate table-field inline caches, specialized arithmetic, fast builtins,
   and safe global-import caching independently against the scoreboard.
1. A6/A7: profile strings and cold compilation, including the cached-compilation
   work tracked in [#118](https://github.com/wallstop/NovaSharp/issues/118), then
   optimize only measured bottlenecks; exit with string-heavy workloads within
   2–3x of native Lua, cold `LoadString` within 5–10x, and cold-load allocations
   reduced at least 5x. Do not rewrite the parser without a demonstrated loading
   need.
1. B2: ship the UPM package, generator integration, Lua assets/loaders, and
   allocation-gated Unity value marshalling; prove Mono, IL2CPP, and WebGL paths.

## Later or gated

1. B3/B4: async/coroutine hosting, capability-based mod isolation, deterministic
   providers, hot reload, supply-chain controls, and a Unity mod-host sample;
   close the remaining [sandbox threat-model](docs/security/sandbox-threat-model.md)
   controls.
1. B5/B6: converge the remaining public/runtime surface on the smallest coherent
   root API, remove superseded APIs outright, and retarget the DAP server and VS
   Code extension in the same cutover. Do not add compatibility layers,
   deprecation windows, or a legacy API freeze.
1. A8: consider a side-by-side register VM only after A6 if measured compute is
   still more than 5x native Lua or call-heavy work remains more than 2x behind
   Lua-CSharp; evaluate the gated
   [stackless/fuel model](docs/proposals/runtime-research-gates.md#stacklessfuel-execution-model)
   in the same study.
1. Lua parity backlog: [call-context error names](https://github.com/wallstop/NovaSharp/issues/124),
   [optional-argument validation](https://github.com/wallstop/NovaSharp/issues/125),
   other error formats, [Lua 5.5 const loop-variable enforcement](https://github.com/wallstop/NovaSharp/issues/130),
   `__gc`, Lua 5.4 garbage collector options, version-migration docs, and explicit
   compatibility-version matrix coverage; retain the
   [host-GC and byte-fidelity constraints](docs/proposals/runtime-research-gates.md#host-gc-and-string-fidelity).
1. Maintenance backlog: complete TUnit data-driving migration and numeric
   conversion boundary audits; consolidate repeated error-message and module-name
   literals incrementally when touched.
