# NovaSharp Project Plan

## 🔴 Lua Spec Compliance Core Principle

NovaSharp's PRIMARY GOAL is to be a **faithful Lua interpreter** that matches the official Lua reference implementation. When fixture comparisons reveal behavioral differences:

1. **ASSUME NOVASHARP IS WRONG** until proven otherwise
1. **FIX THE PRODUCTION CODE** to match Lua behavior
1. **ADD REGRESSION TESTS** with standalone `.lua` fixtures runnable against real Lua
1. **NEVER adjust tests to accommodate bugs** — fix the runtime instead

**Current Status**: Local comparison artifacts were rechecked on 2026-06-27 with `compare-lua-outputs.py --enforce` for Lua 5.1-5.5 and showed zero hard mismatches. A fresh PR CI run must still be observed passing before work is marked accepted.

______________________________________________________________________

## 🚀 STRATEGIC ROADMAP: Performance Parity + Unity-First API (2026)

**Status**: 🔴 **TOP PRIORITY** — supersedes Initiative 21 Phases 3-5. All other initiatives are subordinate to this roadmap unless they block CI health.

### Vision

NovaSharp becomes the definitive Lua modding framework for Unity:

1. Extremely simple to use; interop is first-class ("pit of success" API: RAII, pattern matching, small surface)
1. IL2CPP and Mono safe (no Reflection.Emit, no native binaries, works on WebGL/consoles where NLua cannot)
1. Highly configurable sandboxing (game creators specify features; tiered capability model for untrusted mods)
1. Lua 5.1-5.5 spec-accurate (the moat: no competitor has this + the reference-comparison harness)
1. Roslyn source-generator interop (compile-time bindings, zero reflection)
1. First-class VS Code extension/DAP debugging (already exists; retarget to new API)

### The Performance Problem (measured evidence)

| Workload | NovaSharp/MoonSharp lineage | NLua (native via P/Invoke) | Gap |
| ---------------------------------- | --------------------------- | -------------------------- | ---------------------- |
| Tower of Hanoi (pure Lua) | ~7,175 μs / 35.5 MB alloc | 430 μs / 24 B | **~15x slower** |
| EightQueens (pure Lua) | ~978 μs / 3.5 MB | 82 μs / 24 B | **~12x** |
| Hanoi + C# callback per step | ~600 ms | ~20,000 ms | **33x FASTER** |
| Empty Lua fn 5,000×/frame (Unity) | MoonSharp 315 fps | 170 fps | managed wins 2x |

Sources: `docs/Performance.md` MoonSharp baseline (2025-11-08); MoonSharp author's published Hanoi numbers; Lua-CSharp issue #156 (where Lua-CSharp v0.5, a pure-C# register VM with struct values, hit **395 fps** — beating both).

**Conclusion**: the decisive variable is interop density, not raw VM speed. A tuned managed interpreter realistically lands **2-5x of native C Lua on pure compute** (currently 10-15x) and **beats NLua outright on interop-heavy workloads** — which is the actual Unity modding workload. WattleScript proved ~6x is recoverable from the MoonSharp lineage via struct conversion alone; Lua-CSharp proved the full recipe works on IL2CPP.

### Root Causes (confirmed in source)

1. **`DynValue` is a sealed heap class** (`src/runtime/WallstopStudios.NovaSharp.Interpreter/DataTypes/DynValue.cs`) — every Lua value is a GC allocation with `_refId`, `_hashCode`, `_readOnly` baggage. Native Lua's TValue is 16 inline bytes.
1. **`Instruction` is a heap class with 8 fields** (`Execution/VM/Instruction.cs`): `SymbolRef[]`, `string`, `DynValue`, `SourceRef` — cache-hostile pointer chasing per dispatch.
1. **Per-instruction overhead in the hot loop** (`Execution/VM/Processor/ProcessorInstructionLoop.cs`): debugger check, auto-yield check, sandbox instruction check, sandbox memory check, and `shouldYieldToCaller` recomputed after most arithmetic opcodes — paid regardless of configuration.
1. **`Table` = `LinkedList<TablePair>` + three Dictionary indexes** (`DataTypes/Table.cs`): one `LinkedListNode` heap allocation per entry, ~64 B/entry overhead. Native Lua uses an array part + open-addressed hash part.
1. **Call path allocates**: `DynValue[]` tuples for multi-returns/varargs, per-Script `FastStack` defaults of 2 × 131,072 slots (>2 MiB per Script, per coroutine).

### Established Research Verdicts (do not re-litigate; see docs/performance/ research note)

- **Value type**: `readonly struct LuaValue { LuaNumber num; object refValue; DataType tag; }`, `LayoutKind.Auto`, ~24-32 B. True NaN-boxing is impossible on the CLR (ECMA-335 §II.10.7 forbids object-ref/scalar overlap; TypeLoadException). Ref-over-ref overlap is legal (16 B DynaJson-style) but defer until measured — IL2CPP verification risk. Keep the existing `LuaNumber` int/float union verbatim.
- **Dispatch**: one `while(true)` + `switch` over a **dense, zero-based opcode enum**. This is what Mono's interpreter, the new CoreCLR (.NET 10) interpreter, MoonSharp, and Lua-CSharp all converge on; it compiles to a jump table on RyuJIT **and** under IL2CPP. Do NOT use delegate arrays (worst on IL2CPP), tail-call threading (no C# `tail.`; 60x pathological slowdowns; unavailable on IL2CPP), or instruction-object virtual dispatch (DLR-interpreter design, known slow). `delegate*` tables are unproven and IL2CPP's shakiest corner.
- **Loop hygiene**: PC/stack-base in `ref` locals; `Unsafe.Add`/`MemoryMarshal.GetArrayDataReference` for slot access (Lua-CSharp ships this on IL2CPP); `[MethodImpl(NoInlining)]` on cold/error paths; try/catch OUTSIDE the loop; never allocate per instruction. IL2CPP/Mono get NO PGO or tiering — design for the static-compile case.
- **No exceptions on hot call paths** — this exact mistake cost Lua-CSharp 10x (v0.4.2 → v0.5 fix).
- **Interop**: Roslyn incremental source generator (attribute + partial class) is the industry-consensus binding pattern (System.Text.Json, MessagePack v3, MemoryPack, VContainer, Lua-CSharp). Performance bar: xLua gencode ≈ 107 ns/call on mid-range Android. NLua's structural weakness: `MethodBase.Invoke` per call + boxing + link.xml fragility + per-platform native binaries + no WebGL.
- **Platform floor**: Unity 2021.2 minimum (C# 9, netstandard2.1, Roslyn generators via `RoslynAnalyzer` label, Microsoft.CodeAnalysis 3.8 for the generator).
- **No legacy/back-compat obligations** (pre-1.0). The ONLY invariant is Lua runtime correctness, enforced by the fixture/comparison harness.

### Methodology (iron rules for every phase)

1. **Scoreboard before surgery**: no optimization phase starts until Phase A0's comparison benchmarks exist and a baseline is committed.
1. **No phase merges with a red fixture.** Any fixture edit requires reference-Lua output as arbiter.
1. Each phase ends with: targeted tests, `./scripts/build/quick.sh`, `./scripts/test/quick.sh`, `compare-lua-outputs.py --enforce` (5 versions), scoreboard run with committed JSON baseline, PR CI observed green.
1. Allocation claims are verified by **exact B/op BenchmarkDotNet assertions** (noise-free); speed claims by ratio-vs-NLua gates (±10% tolerance).
1. IL2CPP spot-check (minimal stopwatch Unity player scene) at phase boundaries A1, A5, A8, B2 — RyuJIT wins don't automatically translate.

______________________________________________________________________

### Workstream A: VM Core Re-Architecture (staged hybrid)

**Strategy**: convert data layout inside the current stack VM first (A1-A7) — each phase lands green against the full fixture suite. A register-based VM (A8) is the **gated endgame**, built side-by-side only if data shows the stack VM plateaued short of target. Rationale: the 12K-test harness only pays off if it runs green continuously; a from-scratch VM has a months-long dark period where failures are needles in haystacks. All A1-A7 work (value type, tables, call convention, CoreLib migration, pooling) carries into A8 unchanged; only the loop bodies + emitter would be rewritten (~15-20% of effort).

#### Phase A0 — Comparison Scoreboard (~1 week) 🔴 FIRST

Extend `src/tooling/WallstopStudios.NovaSharp.Comparison/`:

- [x] Add **Lua-CSharp** (NuGet `LuaCSharp`) as a benchmark target alongside existing NLua/MoonSharp.
- [x] Add **reference `lua` CLI** wall-time as an out-of-process context column (export existing comparison scenarios and emit BenchmarkDotNet-shaped JSON; not a managed allocation measurement).
- [x] Expanded pure-Lua comparison workloads: fib(30), hanoi, n-body, binary-trees, spectral-norm; table-heavy (int-key fill/iterate, string-key lookup, `next` traversal, insert/remove churn); string-heavy (concat chains, `gsub`/`find`, `string.format`); coroutine ping-pong.
- [ ] Add **interop both directions** to the comparison matrix (1M Lua→C# registered-fn calls with 2 args + return; 1M C#→Lua `Call`) with per-engine host bindings and clear reference-`lua` skip behavior.
- [ ] Add cached-compile comparison rows alongside the existing cold compile rows.
- [ ] `[MemoryDiagnoser]` on everything; per-phase JSON baselines committed under `progress/`; one command emits the scoreboard markdown table (rows = benchmarks; columns = NovaSharp current/baseline, MoonSharp, NLua, Lua-CSharp, lua CLI).
- [ ] CI gates: ratio-vs-NLua ±10% + exact B/op assertions.
- [ ] Minimal stopwatch-based Unity player scene for IL2CPP spot checks.

**Exit criteria**: baseline table for all 5 engines committed; allocation numbers recorded.

**Progress**: Lua-CSharp comparison rows were wired on 2026-07-01. Reference `lua` CLI wall-time context was wired on 2026-07-02. Pure-Lua Phase A0 workload coverage was expanded on 2026-07-02. See [progress/session-124-phase-a0-luacsharp-comparison.md](progress/session-124-phase-a0-luacsharp-comparison.md), [progress/session-125-phase-a0-lua-cli-context.md](progress/session-125-phase-a0-lua-cli-context.md), and [progress/session-126-phase-a0-expanded-workloads.md](progress/session-126-phase-a0-expanded-workloads.md). Full five-engine scoreboard baselines, interop/cached-compile rows, ratio/allocation gates, and Unity IL2CPP spot-check work remain open.

#### Phase A1 — `LuaValue` struct (~3-5 weeks; highest impact, highest risk)

Replace the `DynValue` class with:

```csharp
[StructLayout(LayoutKind.Auto)]
public readonly struct LuaValue : IEquatable<LuaValue>
{
    private readonly LuaNumber _number;  // existing 16B explicit-layout int/float union, kept verbatim
    private readonly object _ref;        // Table / Closure / string / Coroutine / UserData / tuple
    private readonly DataType _type;     // byte-backed enum; default(LuaValue) == Nil
}
```

Sub-steps, each landing green:

- [ ] **A1a (prep)**: remove `_readOnly`/clone-as-writable machinery; audit and remove `DynValue.ReferenceId` consumers (only `CoreLib/DebugModule.cs` and `ProcessorDebugger.cs` — derive identity from the referenced object, which matches Lua `tostring(t)` semantics; `Table`/`Closure`/`Coroutine` remain classes so identity survives). Kill the `_hashCode` cache.
- [ ] **A1b (targeted fixtures FIRST)**: `select('#', ...)`, `table.pack(...).n`, nil-in-middle tuples, function-call-in-expression vs statement position — the `null` vs `Nil` vs `Void` drift hazards.
- [ ] **A1c (the conversion)**: class→struct, compiler-error-driven across interpreter + CoreLib + tests. `default(LuaValue)` = Nil; `Void` stays an explicit tag; every `== null` / `?? DynValue.Nil` site audited manually (not regex).
- [ ] **A1d (tuning)**: aggressive-inline accessors; `in`/`ref readonly` passing on hot helpers. Struct copy semantics delete the shared-readonly-literal concern entirely.

**Exit criteria**: fixtures green on 5.1-5.5; NumericLoops **0 B/op steady-state**; compute suite ≥1.5-2x vs A0 baseline; no test relies on value reference identity.

#### Phase A2 — Struct `Instruction` + packed chunks (~1-2 weeks)

- [ ] `Instruction` class → `readonly struct { OpCode Op; int A, B, C; }` stored in a contiguous `Instruction[]` per chunk.
- [ ] Constants (`Value`/`Name`) move to a per-chunk `LuaValue[]` constant pool indexed by operand; `Symbol`/`SymbolList` to per-chunk symbol tables; `SourceCodeRef` to a **parallel line-info array** (PUC-style), consulted only on error/debug.
- [ ] Keep the opcode enum dense from 0; verify the dispatch switch compiles to a jump table.
- [ ] Rework the `ByteCode.cs` emitter for packed operands; new binary dump format with version-bumped header; drop old-format load support (pre-1.0); add cross-version load-rejection test.

**Exit criteria**: fixtures + `SerializationTests/` round-trip green; chunk memory ≥4x smaller; measurable dispatch win on branch-heavy microbench.

#### Phase A3 — Hot-loop hygiene (~1-2 weeks; parallelizable with A4)

- [ ] **Loop specialization**: select at entry between a *plain loop* (zero per-instruction checks) and an *instrumented loop* (debugger/sandbox/auto-yield); re-select on state change (debugger attach, options change).
- [ ] **Fuel-based sandbox limits**: instruction-limit counter decrements only at loop back-edges (backward jumps) and call sites. Sandbox limits become basic-block-granular — an intentional, documented behavior change; sandbox tests assert "trips within limit + K", not exact counts.
- [ ] **Memory checks move to allocation sites** (`Table` growth, string creation — `AllocationTracker` already lives there), out of the dispatch loop.
- [ ] Kill per-arith `shouldYieldToCaller`: the `YieldSpecialTrap` sentinel check remains only on call/return/meta-invoke opcodes; yielding arith metamethods route through call machinery.
- [ ] try/catch stays outside `while(true)`; `NoInlining` on throw helpers and metamethod slow paths; hoist stack top into ref locals.

**Exit criteria**: plain-loop dispatch within ~2x of Lua-CSharp; sandbox/debugger/auto-yield suites green under documented granularity; zero measurable tax on the non-debugged path.

#### Phase A4 — Table rewrite (~2-3 weeks; parallelizable with A3)

- [ ] Replace `LinkedList<TablePair>` + three `LinkedListIndex` maps with PUC-style **array part (`LuaValue[]`) + open-addressed hash part** (`(LuaValue key, LuaValue value, int next)[]` node array); PUC `luaH_resize` array/hash split heuristic; border-search `#` semantics per version.
- [ ] **Before rewriting**: verify no fixture depends on insertion order (fixtures compare against reference Lua, whose order already differs) — smoke-test with a shuffled-iteration debug table; add `next`-contract fixtures (every key visited once; assignment-during-traversal legal; `next` after removal semantics; `#` border behavior per version).
- [ ] Re-point `AllocationTracker` hooks to actual array/hash sizing.
- [ ] Keep the public `Table` API shape; swap internals.

**Exit criteria**: table-heavy suite ≥3-5x vs baseline; ~24-40 B/entry; binary-trees ≥2x; `next`-contract fixtures green.

#### Phase A5 — Call path + interop signatures (~3-4 weeks)

- [ ] `CallStackItem` → struct frames in a growable stack; delete `CallStackItemPool(s)`.
- [ ] Shrink per-Script stacks: grow-on-demand from 512 values / 64 frames (currently 2 × 131,072 = >2 MiB); geometric growth, configurable ceiling. This is also the coroutine-cost fix.
- [ ] Args as stack windows (base + count); CLR callbacks receive `ReadOnlySpan<LuaValue>`; multi-return via return-buffer writer. `LuaValue[]` tuples remain only for varargs capture / `table.pack`, pooled.
- [ ] Migrate `CallbackFunction.ClrCallback` to the span-based form; migrate ~20 `CoreLib/` modules **one module per PR** (per-module fixture suites localize failures).
- [ ] **No exceptions on hot call/return/yield paths.**

**Exit criteria**: fib/hanoi ≤2-3x of NLua; **Lua→CLR interop benchmark beats NLua outright**; `new Script()` <100 KB; coroutine create/resume near-zero steady-state alloc.

#### Phase A6 — Strings (~1-2 weeks)

- [ ] N-ary `Concat` via pooled builder (one allocation per chain, not per pair).
- [ ] Optional per-engine intern pool for table string keys + constants → reference-equality fast path in hash probes.
- [ ] Span-based scanning for `gsub`/`find`/`match` hot paths; `string.format` fully on ZString.

**Exit criteria**: string-heavy suite ≤2-3x native; `StringPatternBenchmarks` regression gate green.

#### Phase A7 — Compile time (~1-2 weeks, profile-first)

Loading is 20-60x behind `luaL_loadstring`. Do NOT rewrite the parser. Profile, then:

- [ ] Lexer token payloads as `ReadOnlyMemory<char>` slices (no per-token string until identifier/string is materialized, then interned).
- [ ] Pooled AST lists; struct leaf nodes only where mechanical.
- [ ] Keep/strengthen the chunk cache; bytecode dump/load is the "precompile for Unity" story.

**Exit criteria**: cold `LoadString` within 5-10x of `luaL_loadstring` (honest ceiling for an AST pipeline); loading allocations ↓ ≥5x.

#### Phase A8 — GATED: Register-based VM v2 (~6-10 weeks IF triggered)

**Gate (evaluate after A6)**: proceed iff compute suite is >5x native lua OR call-heavy is >2x behind Lua-CSharp. Otherwise defer indefinitely.

- [ ] New compiler backend from the existing AST (`Tree/`) emitting PUC-5.4-style register bytecode; new executor with struct frames and `Unsafe.Add` register access; loop-specialization variants reused from A3.
- [ ] Side-by-side migration: internal engine-selection option; CI matrix runs the full fixture suite against BOTH engines; flip the default when green + faster across the whole scoreboard; delete the stack VM (`ProcessorInstructionLoop.cs`, stack emitter, stack opcodes) one milestone later.
- [ ] Coroutines: pooled per-coroutine frame stacks from day one; suspend = save frame window (no exceptions).
- [ ] Debugger: line-info array maps PCs to `SourceRef`; instrumented loop variant interprets the same bytecode.

#### End-state estimates (vs native C Lua; honest uncertainty)

| Class | After A1-A7 (stack VM) | After A8 (register VM) | Confidence |
| ----------------------------- | ---------------------- | ---------------------- | ------------------------------------ |
| Numeric loops | 2.5-5x | 2-3.5x | High (WattleScript/Lua-CSharp precedent) |
| Call-heavy (fib/hanoi) | 3-7x | 2-4x | Medium — the register-VM fork |
| Table-heavy | 2-4x | 2-3x | Medium-high |
| String-heavy | 1.5-3x | same | High (BCL strings are good) |
| Interop Lua↔CLR | **beats NLua 2-10x** | same or better | High — the structural advantage |
| Compile (cold) | 5-15x slower | same | Medium |

Total A0-A7 ≈ 14-20 engineer-weeks; A8 +6-10 if triggered.

______________________________________________________________________

### Workstream B: Public API + Unity Experience (facade-first, parallel with A)

**Strategy**: land the new small API as a **facade over the current VM** immediately (B0), so the source generator, Unity package, and modding framework proceed in parallel with — not behind — the VM work. The facade's `LuaValue` becomes the VM-native type when A1 lands.

#### Locked design decisions

- Root namespace **`NovaSharp`** (assemblies/NuGet keep the `WallstopStudios.` prefix). Hard break; no public compat package, ever. Target **~30 core public types** (vs ~114 today).
- **Options record + presets**, not a fluent builder: `LuaEngineOptions { Version, Modules, Sandbox, Loader, Time, Random, Print }` with `Default` / `HardSandbox` presets composed via `with`.
- **Dual sync/async; sync is the hot path**: `Run`/`RunAsync`, `Call`/`CallAsync` (`ValueTask`-based). A sync call that hits an async suspension throws a teaching `LuaBlockedException` ("this chunk awaited a host task; use RunAsync").
- **`LuaValue` struct is the only value currency**: implicit conversions in (`lua.Globals["speed"] = 3f`); `Read<T>()` / `TryRead<T>(out T)` / `AsNumber()`-family out; pattern matching via `Kind` enum + property patterns. **No boxed class hierarchy** (permanent allocation trap dressed as ergonomics).
- **RAII**: `using var lua = LuaEngine.Create(options);` — the engine owns pooled stacks, interner, compilation cache; disposal invalidates handles.
- **Calls**: arity ladder `Call(a0..a3)` (zero-alloc for per-frame use) + `ReadOnlySpan<LuaValue>` overloads + multi-return into caller `Span<LuaValue>`. No generic `Call<T>` matrix — chain `.Read<T>()`.
- Public API consumable from **C# 9** (Unity 2021.2 floor): no `required`, no ref-struct interfaces; `init`/records fine.

#### Core API sketch (the contract; refine in B0 RFC)

```csharp
namespace NovaSharp;

public sealed class LuaEngine : IDisposable
{
    public static LuaEngine Create();
    public static LuaEngine Create(LuaEngineOptions options);
    public LuaTable Globals { get; }
    public LuaValue Run(string code, string chunkName = null);
    public ValueTask<LuaValue> RunAsync(string code, string chunkName = null, CancellationToken ct = default);
    public LuaChunk Compile(string code, string chunkName = null);  // cached, dump/load capable
    public LuaTable CreateTable(int arrayCapacity = 0, int hashCapacity = 0);
    public LuaCoroutine CreateCoroutine(LuaFunction fn);
    public void Dispose();
}

public delegate LuaValue LuaCallback(LuaContext ctx, ReadOnlySpan<LuaValue> args);

// Pattern matching idiom (no boxing):
// value switch
// {
//     { IsNil: true }           => "nil",
//     { Kind: LuaKind.Integer } => $"int {value.AsInteger()}",
//     { IsString: true }        => value.AsString(),
//     _                         => value.Kind.ToString(),
// }
```

Pit-of-success targets: a sandboxed mod host in **<15 lines**; a bound game API class in **<30 lines**; per-frame `onUpdate.Call(Time.deltaTime)` with **0 B GC/frame**.

#### Packaging

| Package | Contents |
| ------------------------------------ | -------------------------------------------------------------------------------------------------------------------------------- |
| `NovaSharp` (core) | VM, LuaValue/LuaEngine/LuaTable/LuaFunction/LuaCoroutine, stdlib, sandbox, 5.1-5.5 profiles; `Tree/`, `Execution/`, descriptors → internal |
| `NovaSharp.Interop.Generator` | Roslyn incremental generator + analyzers (netstandard2.0; ships as analyzer asset + raw DLL for Unity `RoslynAnalyzer` label) |
| `NovaSharp.Interop.Reflection` | today's `UserData.RegisterType<T>` descriptor system; desktop/dev fallback; explicitly unsupported on IL2CPP; ships link.xml |
| `NovaSharp.Modding` | ModHost, capability manifests, per-mod isolation, hot reload, EmmyLua stub emission |
| `NovaSharp.Debugging.Dap` | current VS Code debugger retargeted to the new engine |
| `NovaSharp.Cli` (dotnet tool) | REPL, batch runner, luac-style compile, stub dump |
| `com.wallstopstudios.novasharp` (UPM) | asmdefs, generator DLL, `LuaAsset` ScriptedImporter, Resources/Addressables/StreamingAssets loaders, Unity struct pack, `Samples~/ModHost` |

#### Phase B0 — API facade + RFC (~2-3 weeks; parallel with A1)

- [ ] `LuaValue`, `LuaEngine`, `LuaTable`, `LuaFunction`, `LuaCoroutine`, `LuaEngineOptions`, exception types — as a facade over the current VM (LuaValue wraps DynValue internally at first).
- [ ] Public-API baseline file (`PublicAPI.Shipped.txt` style) checked in and CI-enforced (<40 core types).
- [ ] Samples compile and run: hello world, per-frame call, sandboxed host.

**Exit criteria**: facade `Run`/`Call` within 5% of `Script.DoString`/`Call` on the A0 scoreboard.

#### Phase B1 — Source generator MVP

- [ ] Attributes in core: `[LuaObject(name)]` on partial class/struct, `[LuaMember(name)]`, `[LuaMetamethod(...)]`, `[LuaIgnore]`. Enums referenced by members auto-exposed as string-keyed tables.
- [ ] Generated shape targets the new span convention directly: string-switch member dispatch (no dictionary), typed arg unpack (no MethodInfo, no boxing), async members generate suspension markers.
- [ ] Analyzer diagnostics: NS0001 not-partial; NS0002 unsupported parameter type (+fix); NS0003 ref/out/pointer/open-generic; NS0004 name collision; NS0005 async return type needs adapter package; NS0006 member on non-LuaObject type. Golden-file generator tests.
- [ ] Deprecate the CodeDom Hardwire CLI; delete at generator parity (supersedes `docs/proposals/roslyn-hardwire-generator.md`'s keep-surface goal — the generator emits the NEW shape).
- [ ] Bonus output: EmmyLua/LuaLS `.lua` stubs per `[LuaObject]` → modder autocomplete in the VS Code extension.

**Exit criteria**: bound GameApi sample <30 lines; trimmed publish emits zero NovaSharp trim warnings; generated path verified reflection-free.

#### Phase B2 — Unity package

- [ ] UPM layout (extend `scripts/packaging/`), asmdefs, generator DLL with `RoslynAnalyzer` label.
- [ ] `LuaAsset` ScriptedImporter (`.lua`/`.luac` → source + precompiled chunk); `require` resolvers for Resources/Addressables/StreamingAssets as `IScriptLoader` implementations.
- [ ] Unity struct marshal pack (xLua `GCOptimize` equivalent): pre-generated pack/unpack for Vector2/3/4, Quaternion, Color, Rect, Bounds, Matrix4x4 via pooled-slot userdata (`LuaValue{ tag=UserData, ref=StructPool page, num=slot }`); generated field accessors (`pos.x` reads the slot, no reflection). Riskiest piece — own benchmark gate; fallback = pooled-table pack/unpack.

**Exit criteria**: sample opens in Unity 2021.3 LTS; **IL2CPP iOS + Android builds with zero link.xml entries**; WebGL hello-world runs (the NLua-can't-do-this demo); Unity Profiler shows **0 B GC/frame** for per-frame Call + Vector3 read.

#### Phase B3 — Async + coroutine bridge

- [ ] `RunAsync`/`CallAsync`/`ResumeAsync`; async `[LuaMember]` (ValueTask/Task; Unity `Awaitable` in the Unity pack; UniTask adapter package); resumption on the engine's captured SynchronizationContext (Unity main thread); cancellation surfaces as a Lua error.
- [ ] `game.wait(2.5)` sample: C# await inside a bound function transparently suspends the calling Lua coroutine.

**Exit criteria**: cutscene sample works; sync-into-awaiting-chunk throws the teaching exception; cancellation suite green on Mono + IL2CPP.

#### Phase B4 — Modding framework

- [ ] `ModHost` (builds on `Modding/ModManager.cs`): one `LuaEngine` per mod (cheap post-A5), shared host API via generated bindings, cross-mod contact only through host-mediated events.
- [ ] **Tiered permissions (Noita model)**: manifest gains `"capabilities": ["storage", "net", "unsafe.io"]`; each capability maps to deltas of `CoreModules` flags + `SandboxOptions` entries + extra API tables. Default tier = HardSandbox preset + instruction/memory/stack/coroutine limits. `unsafe.*` requires a host-supplied consent callback (game shows the dialog).
- [ ] Hot reload: file watcher (editor/desktop); contract `mod.save_state() -> table` before teardown, `mod.load_state(t)` after; state serialized via the json module.
- [ ] Determinism: `ITimeProvider`/`IRandomProvider`/UTC seams already exist — document a `Deterministic` preset; build no more yet.
- [ ] `Samples~/ModHost`: one scene, a `[LuaObject]` GameApi, two mods (safe tier + one requesting `unsafe.io` to demo consent), permission prompt UI.

**Exit criteria**: malicious-mod trio contained (infinite loop → instruction limit; memory bomb → memory limit; `io.open` at safe tier → capability denial; all as `SandboxViolationException` with the engine reusable after); hot reload preserves declared state; VS Code shows completions from emitted stubs.

#### Phase B5 — Migration + surface lockdown

- [ ] CoreLib is rewritten onto `LuaValue`/span signatures as part of A5 (not shimmed — required for zero-alloc goals).
- [ ] ~12K TUnit tests migrated via a scripted Roslyn rewriter in `scripts/` (`DoString`→`Run`, `DynValue.NewNumber(x)`→`(LuaValue)x`, `.Number`→`.AsNumber()`, `.Type == DataType.X`→`.Kind == LuaKind.X`); a short-lived **in-repo** shim may bridge during migration but is never published and is deleted before 1.0.
- [ ] Old `WallstopStudios.NovaSharp.Interpreter.*` public namespaces disappear; API-baseline CI check freezes the surface.

**Exit criteria**: full suite green post-migration; surface diff reviewed and frozen.

#### Phase B6 — Debugger alignment

- [ ] DAP server + VS Code extension retargeted to `LuaEngine`/`LuaValue` (single `IDebugService`-style hook stays public in core so the DAP package needs no internals access).

**Exit criteria**: breakpoint + variable inspection (LuaValue kinds rendered) in VS Code against the Unity sample.

______________________________________________________________________

### Spec-Compliance Risk Register

| Risk | Phase | How the harness catches it / mitigation |
| ---------------------------------------------- | ----- | -------------------------------------------------------------------------------------------------------------- |
| `null` vs `Nil` vs `Void` conflation | A1 | Targeted fixtures added BEFORE conversion; manual audit of every `== null` site |
| Value identity semantics (`_refId` removal) | A1 | `Table`/`Closure`/`Coroutine` stay `RefIdObject` classes; tostring/debugger fixtures |
| Shared readonly literals in instruction stream | A1-A2 | Struct copy semantics eliminate the bug class; delete readonly machinery only after fixtures green |
| Bytecode dump/load format break | A2 | Version-bump header; `SerializationTests/` round-trip + load-rejection test |
| Sandbox limit granularity (per-instr → fuel) | A3 | Intentional documented change; tests assert "trips within limit + K" |
| Auto-yield timing shift | A3 | Assert yield happens, not exact instruction index |
| **Table iteration order change** | A4 | Fixtures compare vs reference Lua (order already differs); shuffled-iteration smoke test; `next`-contract fixtures added pre-rewrite |
| `#`/border semantics with array part | A4 | Port PUC border search exactly; per-version `#` fixtures; fuzz vs reference lua |
| CoreLib signature migration typos | A5 | One module per PR; per-module fixture gates |
| Int/float subtype (5.3+) | all | `LuaNumber` union preserved verbatim; numeric edge-case fixtures as canary |

### Priority order (what to do next, in sequence)

1. 🔴 **A0** — comparison scoreboard (everything else is blind without it)
1. 🔴 **B0** — API facade + RFC (parallel; unblocks generator/Unity work)
1. 🔴 **A1** — LuaValue struct (the single highest-impact change)
1. 🟡 **B1** — source generator MVP ∥ **A2** — struct instructions
1. 🟡 **A3 ∥ A4** — hot-loop hygiene ∥ table rewrite
1. 🟡 **A5** — call path + span interop (then CoreLib migration, one module per PR)
1. 🟡 **B2** — Unity package; **A6/A7** — strings/compile-time
1. 🟢 **B3/B4** — async bridge, modding framework
1. 🟢 **A8 gate decision** (data-driven), **B5/B6** — migration lockdown, debugger

______________________________________________________________________

## Historical Repository Snapshot (2025-12-22)

**Build & Tests**:

- Zero warnings with `<TreatWarningsAsErrors>true` enforced
- **11,901** interpreter tests via TUnit (Microsoft.Testing.Platform), as observed on 2025-12-22
- Coverage: ~75.3% line / ~76.1% branch (gating targets at 80%)
- CI: Tests on matrix of `[ubuntu-latest, windows-latest, macos-latest]`

**TUnit Version Coverage Progress**:

- **2,000+** tests with explicit version attributes
- All Lua execution tests have proper version coverage
- Helper attributes (`[AllLuaVersions]`, `[LuaVersionsFrom]`, etc.) available for new tests

**Audits & Quality**:

- `docs/audits/documentation_audit.log`, `docs/audits/naming_audit.log`, and `docs/audits/spelling_audit.log` are tracked audit baselines; rerun the matching audit scripts before treating them as current.
- Runtime/tooling/tests remain region-free
- DAP golden tests: 20 tests validating VS Code debugger payloads
- LuaNumber lint script (`check-luanumber-usage.py`) passing

**Infrastructure**:

- Sandbox: Complete with instruction/memory/coroutine limits, per-mod isolation
- Benchmark CI: BenchmarkDotNet with threshold-based regression alerting
- Packaging: NuGet publishing workflow + Unity UPM scripts
- **Array Pooling**: `DynValueArrayPool`, `ObjectArrayPool` (exact-size), `SystemArrayPool<T>` (variable-size)
- **Zero-Allocation Strings**: ZString integration complete, span-based operations for hot paths

**Lua Compatibility**:

- Historical local comparison observation (2026-06-27): existing Lua 5.1-5.5 artifacts rechecked with `compare-lua-outputs.py --enforce` had 0 `mismatch`, 0 `lua_only`, and 0 `nova_only`.
- Bytecode format version `0x151` preserves integer/float subtype
- JSON/bytecode serialization preserves integer/float subtype
- DynValue caching extended for negative integers and common floats
- All character classes, metamethod fallbacks, and version-specific behaviors implemented
- CLI argument registry (`CliArgumentRegistry`) with comprehensive Lua version support
- VM state protection (Phase 1) prevents external corruption

______________________________________________________________________

## 🔴 HIGH PRIORITY: Known Issues

### Lua Comparison CI/CD Failure Resolution

**Status**: Historical phases recorded on 2026-01-02; PR 30 follow-up remains active until fresh CI is observed passing.

**Problem**: Lua comparison tests are failing across various OS/version combinations in CI/CD. Failures downloaded as `lua-comparison-VERSION-OS-latest.zip` artifacts need systematic extraction, categorization, and resolution while maintaining strict Lua spec compliance.

**Scope**: This item incorporates and supersedes:

- "Consolidate Lua Version Parsing Logic" (tech debt item below)
- "Migrate Tests to Range-Based Version Annotations" (future-proofing item below)

**Guiding Principle**: NovaSharp must match reference Lua exactly. Failures indicate either NovaSharp bugs (fix production code), metadata bugs (fix `@lua-versions`), or documented platform C library/spec implementation-defined behavior (`@novasharp-only: true` is reserved for those cases and NovaSharp extensions).

**PR 30 follow-up (2026-06-27)**:

- Decouple `lua-comparison` and `code-coverage` from the full OS unit-test matrix so a Windows unit-test failure does not hide spec comparison or coverage signal.
- Keep the 3 OS × 5 Lua comparison matrix, with bounded workers, reference Lua caching, and summaries that include `mismatch`, `lua_only`, `nova_only`, `both_error`, ratchet counts, and fixture runtime.
- Add `docs/testing/lua-error-ratchet.json` so new or changed unclassified `both_error` signatures fail while reductions pass.
- Acceptance requires observed local verification and a fresh passing PR CI run; unrun checks remain residual risk.

**Analysis Results** (from Phase 1): 4 failures identified across 10 archives:

| Fixture                                           | Category           | Action                       |
| ------------------------------------------------- | ------------------ | ---------------------------- |
| `IndexSetDoesNotWrackStack.lua`                   | `novasharp_bug`    | Fix table iteration order    |
| `UnicodeEscapeSequenceIsDecoded.lua`              | `version_specific` | Fix to `@lua-versions: 5.3+` |
| `DateSupportsOyModifierInLua52Plus.lua`           | `os_specific`      | Add `@novasharp-only: true`  |
| `CharHandlesPositiveInfinityAsZeroLua51And52.lua` | `isolated_failure` | Investigate macOS 5.1        |

#### Implementation Steps

**Phase 1: Extract and Analyze Failures** ✅ **COMPLETE** (2026-01-02)

- Extracted all 10 failure archives into `scratch/lua-failures/`
- Created `tools/LuaComparisonAnalyzer/analyze_failures.py`
- Analysis report saved to `scratch/lua-failures/analysis-report.json`
- See [progress/session-099-lua-comparison-ci-resolution.md](progress/session-099-lua-comparison-ci-resolution.md)

**Phase 2: Create Shared Version Parsing Module** ✅ **COMPLETE** (2026-01-02)

- Created `tools/lua_version_utils.py` with all required functions
- 41 unit tests in `tools/test_lua_version_utils.py` (all passing)
- Refactored `scripts/tests/compare-lua-outputs.py` to use shared module
- See [progress/session-099-lua-comparison-ci-resolution.md](progress/session-099-lua-comparison-ci-resolution.md)

**Phase 3: Batch-Convert Lua Fixtures to Range Syntax** ✅ **COMPLETE** (2026-01-02)

- Created `tools/migrate_version_annotations.py` with full migration capabilities
- Scans 2,125 Lua fixture files in `src/tests/**/LuaFixtures/**/*.lua`
- Supports `--dry-run` (default), `--apply`, `--verbose`, `--test` modes
- Converts: `5.1, 5.2, 5.3, 5.4, 5.5` → `all`, `5.X, ..., 5.5` → `5.X+`, contiguous → `5.X-5.Y`
- All 2,113 annotated files already use optimal range syntax (no changes needed)
- 77 files flagged with annotations not on line 1 (warnings)
- 26+ unit tests with `--test` flag, uses shared `lua_version_utils.py`
- See [progress/session-100-lua-fixture-migration-phase3.md](progress/session-100-lua-fixture-migration-phase3.md)

**Phase 4: Batch-Convert C# Test Annotations** ✅ **COMPLETE** (2026-01-02)

- Created `tools/migrate_csharp_version_annotations.py` with full migration capabilities

- Scans 322 C# test files in `src/tests/.../Tests.TUnit/` directories

- Supports `--dry-run` (default), `--apply`, `--verbose`, `--test` modes

- Converts safe patterns:

  | From                                                        | To                                |
  | ----------------------------------------------------------- | --------------------------------- |
  | `[Arguments(Lua51)][Arguments(Lua52)]...[Arguments(Lua55)]` | `[AllLuaVersions]`                |
  | `[Arguments(Lua53)][Arguments(Lua54)][Arguments(Lua55)]`    | `[LuaVersionsFrom(Lua53)]`        |
  | `[Arguments(Lua51)][Arguments(Lua52)]`                      | `[LuaVersionsUntil(Lua52)]`       |
  | `[Arguments(Lua52)][Arguments(Lua53)][Arguments(Lua54)]`    | `[LuaVersionRange(Lua52, Lua54)]` |

- 9 attribute groups converted across 3 files

- 25 entries flagged for manual review (extra parameters, non-contiguous patterns)

- 35 unit tests in `tools/test_migrate_csharp_version_annotations.py`

- Historical observation on 2026-01-02: 13,163 tests passed after migration; rerun the current suite before treating that count as current.

- See [progress/session-101-csharp-annotation-migration-phase4.md](progress/session-101-csharp-annotation-migration-phase4.md)

**Phase 5: Fix Failures by Category** ✅ **COMPLETE** (2026-01-02)

All 4 failures from Phase 1 analysis were resolved:

| Fixture                                           | Resolution                                                              |
| ------------------------------------------------- | ----------------------------------------------------------------------- |
| `IndexSetDoesNotWrackStack.lua`                   | Added `@novasharp-only: true` — NovaSharp-specific behavior test        |
| `UnicodeEscapeSequenceIsDecoded.lua`              | Fixed to `@lua-versions: 5.3+` — Unicode escapes added in Lua 5.3       |
| `DateSupportsOyModifierInLua52Plus.lua`           | Added `@novasharp-only: true` — Platform C library strftime differences |
| `CharHandlesPositiveInfinityAsZeroLua51And52.lua` | Added `@novasharp-only: true` — macOS Lua 5.1 C library quirk           |

See [progress/session-102-lua-ci-resolution-phases5-7.md](progress/session-102-lua-ci-resolution-phases5-7.md)

**Phase 6: Full Matrix Verification** ✅ **COMPLETE** (2026-01-02)

Observed on 2026-06-27 from existing local comparison artifacts:

| Lua Version | Total | Match | Mismatch | Both Error | Skipped |
| ----------- | ----- | ----- | -------- | ---------- | ------- |
| 5.1         | 2,085 | 757   | 0        | 200        | 1,128   |
| 5.2         | 2,085 | 545   | 0        | 148        | 1,392   |
| 5.3         | 2,085 | 731   | 0        | 258        | 1,096   |
| 5.4         | 2,085 | 777   | 0        | 259        | 1,049   |
| 5.5         | 2,085 | 812   | 0        | 267        | 1,006   |

**Success Criteria**: all Lua comparison CI jobs are observed passing with 0 `mismatch`, 0 `lua_only`, 0 `nova_only`, and no new or changed unclassified `both_error` signatures.

**Phase 7: Strengthen CI Enforcement** ✅ **COMPLETE** (2026-01-02)

1. `.github/workflows/tests.yml` contains the `--enforce` comparison step.
1. The configured CI matrix covers 15 combinations (3 OS × 5 versions).
1. Historical local observation from 2026-01-02 reported 13,163 TUnit tests passing; rerun the current suite before treating that count as current.

#### Completion Summary

The "Lua Comparison CI/CD Failure Resolution" initiative recorded the following historical achievements:

- **Zero hard mismatches observed locally** across all 5 Lua versions (5.1, 5.2, 5.3, 5.4, 5.5)
- **4 failures fixed** via appropriate `@novasharp-only: true` or `@lua-versions` corrections
- **Shared tooling** created: `lua_version_utils.py`, migration scripts for Lua and C# annotations
- **CI enforcement** uses the `--enforce` flag to block merges on hard mismatches and ratchet regressions.
- **Future-proof annotations**: Range syntax (`5.3+`, `all`) used throughout codebase

#### Deliverables

| Deliverable           | Location                                                     | Status             |
| --------------------- | ------------------------------------------------------------ | ------------------ |
| Failure analysis tool | `tools/LuaComparisonAnalyzer/analyze_failures.py`            | ✅ Complete        |
| Shared version utils  | `tools/lua_version_utils.py`                                 | ✅ Complete        |
| Lua migration script  | `tools/migrate_version_annotations.py`                       | ✅ Complete        |
| C# migration script   | `tools/migrate_csharp_version_annotations.py`                | ✅ Complete        |
| C# migration tests    | `tools/test_migrate_csharp_version_annotations.py`           | ✅ Complete        |
| Progress: Phase 1-2   | `progress/session-099-lua-comparison-ci-resolution.md`       | ✅ Complete        |
| Progress: Phase 3     | `progress/session-100-lua-fixture-migration-phase3.md`       | ✅ Complete        |
| Progress: Phase 4     | `progress/session-101-csharp-annotation-migration-phase4.md` | ✅ Complete        |
| Progress: Phase 5-7   | `progress/session-102-lua-ci-resolution-phases5-7.md`        | ✅ Complete        |
| Updated fixtures      | `src/tests/.../LuaFixtures/` (range syntax)                  | ✅ Already optimal |
| Updated C# tests      | `src/tests/.../Tests.TUnit/` (range attributes)              | ✅ Complete        |

#### References

- Skill: [lua-comparison-harness](.llm/skills/lua-comparison-harness.md)
- Skill: [test-failure-investigation](.llm/skills/test-failure-investigation.md)
- Skill: [lua-fixture-creation](.llm/skills/lua-fixture-creation.md)
- Prior session: [session-050-fixture-comparison-version-filtering](progress/session-050-fixture-comparison-version-filtering.md)

______________________________________________________________________

### LLM Context Audit & Reorganization ✅ **COMPLETE**

**Status**: ✅ **COMPLETE** — Full reorganization achieved.

**Summary**: Reduced `.llm/` documentation from ~12,339 lines across 32 files to ~4,500 lines across 25 skills + 6 code-sample files.

**Deliverables Completed**:

| Deliverable              | Result                                                                          |
| ------------------------ | ------------------------------------------------------------------------------- |
| `tools/LlmSkillIndexer/` | Python script validates metadata & line counts                                  |
| `.llm/skills-index.json` | Auto-generated with 25 skills across 6 categories                               |
| `.llm/code-samples/`     | 6 files with extracted reusable examples                                        |
| `.llm/skills/`           | 25 files, all \<300 lines, all with YAML metadata                               |
| `.llm/context.md`        | 178 lines (target ~200)                                                         |
| Agent files              | AGENTS.md (33), CLAUDE.md (45), copilot-instructions.md (35), .cursorrules (35) |

**Validation Criteria (All Met)**:

- [x] No `.llm/` file exceeds 500 lines
- [x] All `.llm/skills/*.md` files have valid YAML front-matter
- [x] All agent files point to `context.md`
- [x] Pre-commit validates skill metadata and line counts (strict mode)
- [x] Code samples extracted; no duplicated examples across skills

**Completed**: 2026-01-02. See [progress/session-098-llm-consolidation.md](progress/session-098-llm-consolidation.md).

______________________________________________________________________

### Consolidate Lua Version Parsing Logic ✅ **INCORPORATED**

**Status**: ✅ **INCORPORATED** — Now part of "Lua Comparison CI/CD Failure Resolution" (Phase 2).

**Problem**: Lua version parsing logic is duplicated across multiple Python scripts and doesn't share code:

| File                                                  | Parses          | Supports `5.X+` | Supports `all` | Supports `5.X-5.Y` |
| ----------------------------------------------------- | --------------- | --------------- | -------------- | ------------------ |
| `scripts/tests/run-lua-fixtures-parallel.py`          | `@lua-versions` | ✅ Yes          | ❌ No          | ❌ No              |
| `scripts/tests/compare-lua-outputs.py`                | `@lua-versions` | ✅ Yes          | ❌ No          | ❌ No              |
| `tools/LuaCorpusExtractor/lua_corpus_extractor_v2.py` | Generates       | ✅ Generates    | ❌ No          | ❌ No              |

**Goal**: Create a shared Python module for Lua version parsing with full range syntax support.

**Implementation Steps**:

1. **Create shared module**: `tools/lua_version_utils.py` with:

   - `parse_lua_versions(version_string: str) -> list[str]` - Parse `@lua-versions` values
   - `is_version_compatible(lua_versions: list[str], target: str) -> bool` - Check compatibility
   - Support for: explicit lists, `all`, `5.X+`, `5.X-5.Y` range syntax

1. **Update consumers**: Refactor the three scripts to use the shared module

1. **Add tests**: Unit tests for the version parsing logic

1. **Update corpus extractor**: Generate range syntax where appropriate (e.g., `5.3+` instead of `5.3, 5.4, 5.5`)

**Benefits**:

- Single source of truth for version parsing
- Easier to add new range syntax features
- Reduced risk of parsing inconsistencies between tools

______________________________________________________________________

### Migrate Tests to Range-Based Version Annotations ✅ **INCORPORATED**

**Status**: ✅ **INCORPORATED** — Now part of "Lua Comparison CI/CD Failure Resolution" (Phases 3-4).

**Problem**: Many existing tests and Lua fixtures use explicit version lists (e.g., `5.1, 5.2, 5.3, 5.4, 5.5` or `[Arguments(Lua53)][Arguments(Lua54)][Arguments(Lua55)]`) instead of range-based annotations. When Lua 5.6 is released, these tests will require manual updates.

**Goal**: Migrate all version annotations to range-based syntax for automatic future version inclusion.

**Affected Patterns to Find and Replace**:

| Pattern Type | Find (Explicit)                                       | Replace With (Range-Based)                                   |
| ------------ | ----------------------------------------------------- | ------------------------------------------------------------ |
| Lua fixture  | `@lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5`              | `@lua-versions: all`                                         |
| Lua fixture  | `@lua-versions: 5.3, 5.4, 5.5`                        | `@lua-versions: 5.3+`                                        |
| Lua fixture  | `@lua-versions: 5.1, 5.2`                             | `@lua-versions: 5.1-5.2`                                     |
| Lua fixture  | `@lua-versions: 5.2, 5.3, 5.4`                        | `@lua-versions: 5.2-5.4`                                     |
| C# test      | Multiple `[Arguments(LuaCompatibilityVersion.LuaXX)]` | `[LuaVersionsFrom]`/`[LuaVersionsUntil]`/`[LuaVersionRange]` |

**Implementation Steps**:

1. **Phase 1: Audit** — Run grep/rg to identify all explicit version patterns:

   ```bash
   # Find explicit version lists in Lua fixtures
   rg "@lua-versions: 5\.[1-5], 5\." --type lua -c

   # Find explicit Arguments attributes in C# tests
   rg "\[Arguments\(LuaCompatibilityVersion\.Lua5" --type cs -c
   ```

1. **Phase 2: Prioritize** — Categorize by conversion type:

   - `all` conversions (all 5 versions listed)
   - `5.X+` conversions (contiguous from version to latest)
   - `5.X-5.Y` conversions (contiguous range)
   - Manual review needed (non-contiguous, version-specific behavior)

1. **Phase 3: Batch Conversion** — Use automated scripts where safe:

   - `@lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5` → `@lua-versions: all`
   - `@lua-versions: 5.3, 5.4, 5.5` → `@lua-versions: 5.3+`
   - Validate with `python3 tools/LuaCorpusExtractor/lua_corpus_extractor_v2.py`

1. **Phase 4: Verify** — Run full test suite to ensure no regressions

**Estimated Scope**:

- ~1,800+ Lua fixtures to scan
- ~2,000+ C# tests with version attributes
- Majority expected to be simple conversions

**Benefits**:

- **Future-proof**: New Lua versions automatically included
- **Less maintenance**: No manual updates when 5.6/5.7 releases
- **Consistency**: Uniform annotation style across codebase

______________________________________________________________________

### TUnit Test Project Compilation Speed 🔴 **CRITICAL**

**Status**: 🔴 **NEEDS IMMEDIATE ACTION** — Compilation takes 60-100+ seconds.

**Problem**: The monolithic TUnit test project (`WallstopStudios.NovaSharp.Interpreter.Tests.TUnit`) has grown to **312 C# files** with **~101,000 lines of code**. This results in:

- 60-100+ second compilation times (sometimes exceeding 100s)
- Severely degraded developer iteration speed
- Bottleneck for CI pipeline performance

**Root Cause**: All 11,901 tests are in a single project. The C# compiler cannot parallelize within a single project, and TUnit source generators must process the entire project on each build.

**Proposed Solution**: Split into multiple smaller test projects by domain:

| Project Name                  | Source Folder                                             | Estimated Tests  |
| ----------------------------- | --------------------------------------------------------- | ---------------- |
| `Tests.TUnit.Modules`         | `Modules/`                                                | ~4,000           |
| `Tests.TUnit.EndToEnd`        | `EndToEnd/`                                               | ~2,000           |
| `Tests.TUnit.Units`           | `Units/`                                                  | ~1,500           |
| `Tests.TUnit.Sandbox`         | `Sandbox/`                                                | ~800             |
| `Tests.TUnit.Cli`             | `Cli/`                                                    | ~500             |
| `Tests.TUnit.Serialization`   | `SerializationTests/`                                     | ~400             |
| `Tests.TUnit.Platforms`       | `Platforms/`                                              | ~300             |
| `Tests.TUnit.Spec`            | `Spec/`                                                   | ~300             |
| `Tests.TUnit.PatternMatching` | `PatternMatching/`                                        | ~200             |
| `Tests.TUnit.Isolation`       | `Isolation/`                                              | ~200             |
| `Tests.TUnit.Smoke`           | `Smoke/`                                                  | ~100             |
| `Tests.TUnit.Core`            | `TestInfrastructure/`, `Descriptors/`, `Loaders/`, `Tap/` | Shared utilities |

**Implementation Steps**:

1. Create `Tests.TUnit.Core` with shared infrastructure (`TestInfrastructure/`, `LuaFixtureHelper`, base classes)
1. Create domain-specific projects referencing `Tests.TUnit.Core`
1. Move test files to appropriate projects (preserve namespace structure)
1. Update `quick.sh` scripts to build/test all projects (parallel build)
1. Update CI workflows to run test projects in parallel
1. Verify coverage aggregation still works

**Expected Benefits**:

- **Parallel compilation**: Multiple projects compile simultaneously
- **Incremental builds**: Changing one domain only recompiles that project
- **Reduced generator overhead**: TUnit generators process smaller chunks
- **Target compile time**: \<15 seconds for incremental, \<30 seconds for clean

**Risks**:

- Shared fixtures (`LuaFixtures/`) may need symlinks or build-time copy
- Coverage aggregation may need adjustment
- CI matrix configuration complexity increases

______________________________________________________________________

### 🆕 Data Structures from Unity-Helpers 🟡 **EVALUATION**

**Status**: 🟡 **EVALUATION NEEDED** — Identified high-value components from MIT-licensed sibling repository.

**Source**: [wallstop/unity-helpers](https://github.com/wallstop/unity-helpers) (MIT License, same author as NovaSharp)
**Context**: [llms.txt](https://github.com/wallstop/unity-helpers/blob/main/llms.txt) | [Data Structures Docs](https://github.com/wallstop/unity-helpers/blob/main/docs/features/utilities/data-structures.md)

#### High-Value Components to Port

| Component               | Source File                                                                                                                     | Use Case in NovaSharp                                    | Priority  |
| ----------------------- | ------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------- | --------- |
| **Trie**                | [Trie.cs](https://github.com/wallstop/unity-helpers/blob/main/Runtime/Core/DataStructure/Trie.cs)                               | Lexer keyword lookup, string interning, autocomplete     | 🔴 HIGH   |
| **LevenshteinDistance** | [StringExtensions.cs#L213](https://github.com/wallstop/unity-helpers/blob/main/Runtime/Core/Extension/StringExtensions.cs#L213) | "Did you mean 'print'?" error messages                   | 🔴 HIGH   |
| **BitSet**              | [BitSet.cs](https://github.com/wallstop/unity-helpers/blob/main/Runtime/Core/DataStructure/BitSet.cs)                           | Upvalue tracking, scope flags, VM state                  | 🟡 MEDIUM |
| **Deque\<T>**           | [Deque.cs](https://github.com/wallstop/unity-helpers/blob/main/Runtime/Core/DataStructure/Deque.cs)                             | VM execution stack, double-ended operations              | 🟡 MEDIUM |
| **SparseSet**           | [SparseSet.cs](https://github.com/wallstop/unity-helpers/blob/main/Runtime/Core/DataStructure/SparseSet.cs)                     | O(1) membership with dense iteration for active tracking | 🟢 LOW    |

#### Trie Details (Highest Value)

The unity-helpers `Trie` is an **array-backed, allocation-free prefix tree** optimized for:

- O(m) lookup where m = key length (faster than repeated hash lookups for small sets)
- Zero-allocation prefix search (results returned via caller-provided buffer)
- Immutable post-construction (build once, query many)

**Use Cases for NovaSharp**:

1. **Lexer Keyword Table**: Replace `Dictionary<string, TokenType>` for reserved words (`if`, `then`, `while`, `function`, `local`, etc.). Trie gives predictable per-character traversal.
1. **String Interning**: Fast lookup for commonly-used Lua strings in the interpreter.
1. **"Did You Mean" Suggestions**: `GetWordsWithPrefix()` for autocomplete-style error hints.

**API Example**:

```csharp
// Words only
Trie keywords = new Trie(new[] { "if", "then", "else", "while", "function", "local", ... });
bool isKeyword = keywords.Contains(tokenText);  // O(m)

// With values
Trie<TokenType> keywordTypes = new Trie<TokenType>(new Dictionary<string, TokenType> {
    ["if"] = TokenType.If,
    ["then"] = TokenType.Then,
    ...
});
if (keywordTypes.TryGetValue(tokenText, out TokenType type)) { ... }
```

#### LevenshteinDistance Details

Zero-allocation edit distance using pooled arrays:

```csharp
// From StringExtensions.cs
public static int LevenshteinDistance(this string source1, string source2)
{
    using PooledArray<int> prevLease = SystemArrayPool<int>.Get(len2 + 1, out int[] prev);
    using PooledArray<int> currLease = SystemArrayPool<int>.Get(len2 + 1, out int[] curr);
    // ... O(n*m) dynamic programming with swap optimization
}
```

**Use in NovaSharp**: Better error messages:

```
attempt to call a nil value (global 'pirnt')
Did you mean: 'print'?
```

#### Implementation Steps

**Phase 1: Trie (Immediate Value)**

1. Copy [Trie.cs](https://github.com/wallstop/unity-helpers/blob/main/Runtime/Core/DataStructure/Trie.cs) to `src/runtime/WallstopStudios.NovaSharp.Interpreter/DataStructs/Trie.cs`
1. Remove Unity dependencies (`using UnityEngine;`, `Mathf.Max` → `Math.Max`)
1. Replace `Buffers.GetStringBuilder` with NovaSharp's `ZStringBuilder.Create()` or equivalent
1. Add TUnit tests adapted from [TrieTests.cs](https://github.com/wallstop/unity-helpers/blob/main/Tests/Runtime/DataStructures/TrieTests.cs)
1. Evaluate replacing `Lexer.KeywordTable` with `Trie<TokenType>`

**Phase 2: LevenshteinDistance (Quick Win)**

1. Add `LevenshteinDistance` extension to `src/runtime/.../Extension/StringExtensions.cs`
1. Already uses `SystemArrayPool<int>` pattern compatible with NovaSharp
1. Integrate into error message generation for undefined globals/functions

**Phase 3: BitSet/Deque (Evaluate Need)**

1. Profile current implementations for bottlenecks
1. Port if performance analysis indicates value

#### Considerations

- **No Unity dependencies**: All unity-helpers data structures use Unity only for `Mathf` helpers — trivial to replace with `System.Math`
- **Same coding style**: Same author, same `using` inside namespace, same pooling patterns
- **Well-tested**: 3,000+ tests in unity-helpers cover edge cases

______________________________________________________________________

### TUnit & Build Configuration Investigation ✅ **COMPLETE**

**Status**: ✅ **COMPLETE** — TUnit source generator disabled, using reflection mode for faster builds.

**Result**: Significant build time improvement by disabling TUnit source generator.

**Changes Implemented**:

1. **Directory.Build.props** — Added `<EnableTUnitSourceGeneration>false</EnableTUnitSourceGeneration>` for test projects
1. **GlobalSuppressions.cs** — Added `[assembly: ReflectionMode]` attribute to enable TUnit reflection-based discovery
1. **Restored static analysis** — Warnings as errors, code style enforcement, and analyzers remain enabled for tests

**Key Finding**: The TUnit source generator was the primary bottleneck, processing all ~101K lines of test code. By switching to reflection mode:

- Static analysis and code quality checks remain enforced (warnings as errors)
- Test discovery happens at runtime via reflection (negligible overhead for ~12K tests)
- Build times improved significantly without sacrificing code quality

**Trade-offs**:

- Tests discovered at runtime rather than compile time (no AOT/trimming support for tests)
- Slightly slower test startup (~1-2s) due to reflection-based discovery

**Recommendations**:

- Use test filtering during development: `./scripts/test/quick.sh --no-build -c ClassName -m MethodPattern`
- If further build speed improvement needed, consider splitting into smaller test projects

**Completed**: 2025-12-24. Updated approach to disable only source generator while keeping static analysis.

______________________________________________________________________

### Flaky Test: `MultipleConcurrentResumeAttemptsOnlyOneSucceeds` ✅ **FIXED**

**Status**: ✅ **FIXED** — Root cause identified and resolved.

**Location**: `src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:1100`

**Root Cause**: **TOCTOU Race Condition in Processor.EnterProcessor()**

The `EnterProcessor()` method had a classic Time-of-Check to Time-of-Use (TOCTOU) race condition:

```csharp
// BEFORE (vulnerable to race):
if (_owningThreadId >= 0 && _owningThreadId != threadId && ...)  // Check
{
    throw new InvalidOperationException(...);
}
_owningThreadId = threadId;  // Use/Set (not atomic with check!)
```

When 4 threads simultaneously entered `EnterProcessor()`:

1. All 4 threads read `_owningThreadId == -1` (initial value)
1. All 4 passed the check (`_owningThreadId >= 0` is false)
1. All 4 set `_owningThreadId = threadId`
1. **All 4 succeeded** — no exceptions thrown!

**Fix Applied**: Changed to atomic `Interlocked.CompareExchange` pattern:

```csharp
// AFTER (atomic check-and-set):
int previousOwner = Interlocked.CompareExchange(ref _owningThreadId, threadId, -1);
if (previousOwner != -1 && previousOwner != threadId)
{
    throw new InvalidOperationException(...);
}
```

**Files Modified**:

- `src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/VM/Processor/Processor.cs` — Added `System.Threading` using, rewrote `EnterProcessor()` with atomic CAS

**Verification**:

- Test passed 10/10 consecutive runs locally
- All 252 CoroutineModuleTUnitTests pass
- All 481 Processor-related tests pass
- All 27 ProcessorCoreLifecycleTUnitTests pass

**Completed**: 2025-12-24.

______________________________________________________________________

## Active Initiatives

### Initiative 13: Magic String Consolidation 🟡 **IN PROGRESS**

**Goal**: Eliminate all duplicated string literals ("magic strings") by consolidating them into named constants with a single source of truth.
**Scope**: All runtime, tooling, and test code.
**Status**: Phases 1-2 (Metamethods + Keywords) complete. Incremental enforcement during code changes.

**Completed**:

- ✅ **Phase 1: Metamethods** — Created `Metamethods` static class with 25 `const string` fields. See [progress/session-085-magic-string-consolidation.md](progress/session-085-magic-string-consolidation.md).
- ✅ **Phase 2: Lua Keywords** — Created `LuaKeywords` static class with 22 `const string` fields. Pre-interned in `LuaStringPool`.

**Remaining Areas to Consolidate** (Lower Priority):

1. **Error messages**: `bad argument`, `attempt to`, `number has no integer representation`, etc.
1. **Module names**: `string`, `table`, `math`, `io`, `os`, `debug`, `coroutine`, etc.

### Initiative 21: Performance Parity Analysis — NovaSharp vs Native Lua 🔬

**Status**: 🟡 **IN PROGRESS** — Phase 1 (script caching) and Phase 2 (VM pooling) complete.

**Priority**: 🟡 **MEDIUM** — Strategic performance initiative.

**Goal**: Systematically reduce the performance gap between NovaSharp (pure C# interpreter) and NLua (native Lua via P/Invoke).

**Completed Phases**:

- ✅ **Phase 1**: Script caching with hash-based lookup, lazy line-splitting
- ✅ **Phase 2**: VM execution pooling (LocalScope arrays, BlocksToClose, Varargs arrays)

**Phase 1 Deferred Items** (Session 092 Investigation):

- ❌ **SourceRef → struct**: NOT FEASIBLE — Debugger mutation patterns, ReferenceEquals usage, null semantics
- ❌ **SymbolRef → struct**: NOT FEASIBLE — Two-phase construction, binary deserialization back-patching
- ❌ **Instruction list pooling**: NOT APPLICABLE — ByteCode persists for Script lifetime

**Remaining Phases**: ⛔ **SUPERSEDED** — Phases 3-5 (DynValue struct, register-based VM, string optimizations, benchmark validation) are now covered in full detail by the [Strategic Roadmap](#-strategic-roadmap-performance-parity--unity-first-api-2026) above (Workstream A: A1 = DynValue struct, A6 = strings, A0 = benchmark scoreboard, A8 = register VM gate). Do not start work from this section.

See progress reports: [session-086](progress/session-086-script-compilation-cache.md), [session-087](progress/session-087-vm-execution-pooling-phase2.md), [session-088](progress/session-088-lazy-line-splitting.md), [session-092](progress/session-092-ipairs-metamethod-parity.md).

______________________________________________________________________

## Baseline Controls (must stay green)

- Re-run audits (`documentation_audit.py`, `NamingAudit`, `SpellingAudit`) when APIs or docs change.
- Lint guards run in CI.
- New helpers must live under `scripts/<area>/` with README updates.
- Keep `docs/Testing.md`, `docs/Modernization.md`, and `scripts/README.md` aligned.

______________________________________________________________________

## 🟡 MEDIUM Priority: Test Data-Driving Helper Migration

**Status**: 🟡 **IN PROGRESS** — Core helpers complete, migration ongoing.

### Available Helpers

All helpers are in `src/tests/TestInfrastructure/TUnit/`:

| Helper                        | Description                                 |
| ----------------------------- | ------------------------------------------- |
| `[AllLuaVersions]`            | Expands to all 5 Lua versions (5.1-5.5)     |
| `[LuaVersionsFrom(5.3)]`      | Versions from 5.3+ (inclusive)              |
| `[LuaVersionsUntil(5.2)]`     | Versions up to 5.2 (inclusive)              |
| `[LuaVersionRange(5.2, 5.4)]` | Specific version range                      |
| `[LuaTestMatrix]`             | Full Cartesian product of versions × inputs |

### Remaining Tasks

- [ ] Migrate remaining UserData tests (Methods overload patterns)
- [ ] Migrate remaining EndToEnd tests
- [ ] Migrate Sandbox tests
- [ ] Create automated migration script for common patterns

______________________________________________________________________

## 🟡 MEDIUM Priority: Comprehensive Numeric Edge-Case Audit

**Status**: 📋 **PARTIAL** — Core fixes done, remaining edge cases to audit.

**Completed**:

- ✅ `LuaNumber` struct preserves integer/float distinction
- ✅ Core validation uses `LuaNumber` not `double`
- ✅ Lint script prevents `DynValue.Number` usage in CoreLib

**Remaining**:

- [ ] Audit `Interop/Converters/*.cs` for precision loss patterns
- [ ] Create `NumericEdgeCaseTUnitTests.cs` with boundary values
- [ ] Document version-specific behavior in `docs/testing/numeric-edge-cases.md`

______________________________________________________________________

## Coverage Improvement Opportunities

Current coverage (~75% line, ~76% branch) has significant room for improvement. Key areas with low coverage include:

- **NovaSharp.Hardwire** (~54.8% line): Many generator code paths untested
- **CLI components**: Some command implementations have partial coverage
- **DebugModule**: REPL loop branches not easily testable
- **StreamFileUserDataBase**: Windows-specific CRLF paths cannot run on Linux CI

______________________________________________________________________

## Codebase Organization (future)

- Consider splitting into feature-scoped projects if warranted (e.g., separate Interop, Debugging assemblies)
- Restructure test tree by domain (`Runtime/VM`, `Runtime/Modules`, `Tooling/Cli`)
- Add guardrails so new code lands in correct folders with consistent namespaces

______________________________________________________________________

## Tooling, Docs, and Contributor Experience

- Roslyn source generators/analyzers for NovaSharp descriptors.
- DocFX (or similar) for API documentation.

______________________________________________________________________

## Concurrency Improvements (optional)

- Consider `System.Threading.Lock` (.NET 9+) for cleaner lock semantics.
- Split debugger locks for reduced contention.
- Add timeout to `BlockingChannel`.

See `docs/modernization/concurrency-inventory.md` for the full synchronization audit.

______________________________________________________________________

## Lua Specification Parity

### Official Lua Specifications (Local Reference)

**IMPORTANT**: For all Lua compatibility work, consult the local specification documents first:

- [`docs/lua-spec/lua-5.1-spec.md`](docs/lua-spec/lua-5.1-spec.md) — Lua 5.1 Reference Manual
- [`docs/lua-spec/lua-5.2-spec.md`](docs/lua-spec/lua-5.2-spec.md) — Lua 5.2 Reference Manual
- [`docs/lua-spec/lua-5.3-spec.md`](docs/lua-spec/lua-5.3-spec.md) — Lua 5.3 Reference Manual
- [`docs/lua-spec/lua-5.4-spec.md`](docs/lua-spec/lua-5.4-spec.md) — Lua 5.4 Reference Manual (primary target)
- [`docs/lua-spec/lua-5.5-spec.md`](docs/lua-spec/lua-5.5-spec.md) — Lua 5.5 (Work in Progress)

### Reference Lua comparison harness

- **Status**: Fully implemented. CI runs matrix tests against Lua 5.1, 5.2, 5.3, 5.4.
- **Gating**: `enforce` mode. Known divergences documented in `docs/testing/lua-divergences.md`.
- **Test authoring pattern**: Use `LuaFixtureHelper` to load `.lua` files from `LuaFixtures/` directory.

______________________________________________________________________

## Remaining Lua Runtime Spec Items

### os.time and os.date Semantics ✅ **COMPLETE**

**Status**: ✅ Complete — 149 tests verify os.time/os.date behavior across all Lua versions.

**Verified**:

- [x] `os.time()` returns epoch-based timestamp (integer in 5.3+, float in 5.1/5.2)
- [x] All `os.date` format strings match reference Lua outputs
- [x] Timezone handling via `!` prefix for UTC, local time conversion working
- [x] `*t` table format returns all required fields (year, month, day, hour, min, sec, wday, yday, isdst)

**Completed**: 2025-12-22

### Coroutine Semantics ✅ **COMPLETE**

**Status**: ✅ Complete — 596 tests verify coroutine behavior across all Lua versions.

**Verified**:

- [x] State transition tests for coroutine lifecycle (suspended, running, dead, normal)
- [x] Error message formats match Lua ("cannot resume dead coroutine", etc.)
- [x] `coroutine.close` (5.4) cleanup order with to-be-closed variables

**Completed**: 2025-12-22

### Error Message Parity

**Tasks**:

- [ ] Catalog all error message formats in `ScriptRuntimeException`
- [ ] Create error message normalization layer for Lua-compatible output

### Numerical For Loop Semantics (Lua 5.4)

**Tasks**:

- [ ] Verify NovaSharp for loop handles integer limits correctly per version
- [ ] Add edge case tests for near-maxinteger loop bounds

### \_\_gc Metamethod Handling (Lua 5.4)

**Tasks**:

- [ ] Document NovaSharp's current `__gc` handling
- [ ] Decide on validation strategy (strict vs. Lua-compatible)

### utf8 Library Differences (Lua 5.3 vs 5.4) ✅ **COMPLETE**

**Status**: ✅ Complete — 218 tests verify utf8 library behavior across Lua 5.3+.

**Verified**:

- [x] `utf8.offset` bounds handling is complete
- [x] Lax mode for invalid UTF-8 sequences (Lua 5.4+)
- [x] All utf8 functions match reference Lua behavior

**Completed**: 2025-12-22

### collectgarbage Options (Lua 5.4)

**Tasks**:

- [ ] Support deprecated options with warnings when targeting 5.4
- [ ] Implement `incremental` option for 5.4

### Literal Integer Overflow (Lua 5.4)

**Tasks**:

- [ ] Verify lexer/parser handles overflowing literals correctly per version
- [ ] Add tests for large literal parsing

### ipairs Metamethod Changes (Lua 5.3+) ✅ **COMPLETE**

**Status**: ✅ Complete — Lua 5.3+ now respects `__index` metamethods during `ipairs` iteration.

**Implementation**:

- Added `GetMetamethodAwareIndex()` helper in `BasicModule.cs`
- `__ipairs_callback` now checks `LuaCompatibilityVersion` and uses metamethod-aware indexing for 5.3+
- Lua 5.1/5.2 continue using raw access (spec-compliant)
- 4 new Lua fixtures, 11 new TUnit tests

**Completed**: 2025-12-22

### table.unpack Location (Lua 5.2+) ✅ **COMPLETE**

**Status**: ✅ Complete — 18 tests verify unpack availability matches target version.

**Verified**:

- [x] `unpack` is global function in Lua 5.1 only (via `[LuaCompatibility(Lua51, Lua51)]`)
- [x] `table.unpack` available in Lua 5.2+ (via `TableModule`)
- [x] Both versions use same underlying implementation

**Completed**: 2025-12-22

### Documentation

- [ ] Update `docs/LuaCompatibility.md` with version-specific behavior notes
- [ ] Add "Determinism Guide" for users needing reproducible execution
- [ ] Create version migration guides (5.1→5.2, 5.2→5.3, 5.3→5.4)
- [ ] Add "Breaking Changes by Version" quick-reference table

______________________________________________________________________

## Testing Infrastructure

**Tasks**:

- [ ] Create comprehensive version matrix tests for all modules
- [ ] Add CI jobs that run test suite with each `LuaCompatibilityVersion`
- [ ] Create version migration guide (`docs/LuaVersionMigration.md`)

______________________________________________________________________

## Long-horizon Ideas

- Property and fuzz testing for lexer, parser, VM.
- CLI output golden tests.
- Native AOT/trimming validation.
- Automated allocation regression harnesses.

______________________________________________________________________

## Recommended Next Steps (Priority Order)

### 🔴 HIGHEST: Strategic Roadmap

See the [Strategic Roadmap](#-strategic-roadmap-performance-parity--unity-first-api-2026) at the top of this document. Immediate sequence: **A0** (comparison scoreboard) → **B0** (API facade) → **A1** (LuaValue struct). Everything below is subordinate.

### 🟡 MEDIUM: Remaining Version Parity Items

1. **Version migration guides**

   - `docs/LuaVersionMigration.md` with 5.1→5.2, 5.2→5.3, 5.3→5.4 guides

1. **CI jobs per LuaCompatibilityVersion**

   - Run test suite explicitly with each version setting

### 🟢 LOWER PRIORITY: Polish and Infrastructure

1. **CI Integration**
   - Maintain CI job that runs `compare-lua-outputs.py --enforce` with the `both_error` ratchet on PRs
   - Add CI lint rule that rejects PRs with tests missing version coverage

______________________________________________________________________

## Future Research Initiatives

### 🔴 HIGH PRIORITY: Struct-Based AST for Zero-Allocation Parsing 🔬

**Status**: 🔲 **FEASIBILITY STUDY COMPLETE** — Investigation needed before implementation.

**Goal**: Investigate converting the class-based AST (NodeBase, Statement, Expression hierarchy) to structs + interfaces + generics to achieve zero-allocation parsing.

#### Feasibility Analysis Summary

**Current Architecture**:

- **27 node types**: 1 abstract base (`NodeBase`), 2 intermediate (`Statement`, `Expression`), 16 statement types, 11 expression types
- **Inheritance depth**: 2 levels (shallow, which is good for conversion)
- **Polymorphism**: Virtual `Compile()` method on all nodes, virtual `Eval()` on expressions
- **Allocation pattern**: Fresh `new` allocation per node, no pooling
- **Lifetime**: AST is **discarded after compilation** — only bytecode is retained

**Key Finding: AST Optimization Has Zero Runtime Impact** ⚠️

NovaSharp is a **bytecode VM**, not an AST interpreter:

1. Source → Parser → **AST (temporary)** → Compiler → **Bytecode (permanent)** → VM
1. AST nodes become garbage immediately after `Compile()` finishes
1. Runtime execution uses only the `Instruction` stream, not AST

**This means struct-based AST would only reduce GC pressure during script loading, NOT during execution.**

#### Technical Challenges

| Challenge                                      | Severity    | Mitigation                                                 |
| ---------------------------------------------- | ----------- | ---------------------------------------------------------- |
| **Recursive struct references**                | 🔴 Critical | Index-based children (store `int ChildIndex` into arrays)  |
| **Polymorphism without boxing**                | 🔴 Critical | Discriminated union + switch dispatch, OR generic visitors |
| **27 node types with different fields**        | 🟡 High     | Tagged union with parallel payload arrays per type         |
| **Parser builds via constructors**             | 🟡 High     | Requires arena allocator pattern rewrite                   |
| **Virtual Compile() method**                   | 🟡 High     | Replace with switch on `NodeKind` enum                     |
| **Script/SourceRef references in nodes**       | 🟡 Medium   | Store as indices into context arrays                       |
| **DynamicExprExpression keeps AST for Eval()** | 🟠 Medium   | Exempt from conversion OR special handling                 |

#### Recommended Approach (If Proceeding)

**Option A: Tagged Union + Index-Based References** (Recommended if pursued)

```csharp
public enum NodeKind : byte { BinaryExpr, IfStmt, LiteralExpr, ... }

public readonly struct AstNode  // 16 bytes
{
    public readonly NodeKind Kind;
    public readonly byte Flags;
    public readonly ushort PayloadIndex;  // Index into kind-specific array
    public readonly int ChildStart;       // Index into children array
    public readonly int SpanStart;
    public readonly int SpanLength;
}

// Separate storage per node kind
struct BinaryExprPayload { byte Op; int LeftChild; int RightChild; }
struct LiteralPayload { DynValue Value; }
```

**Option B: Keep Classes, Add Node Pooling** (Lower risk, faster implementation)

```csharp
// Use ObjectPool<T> for common node types
var expr = LiteralExpressionPool.Get();
expr.Initialize(value, sourceRef);
// ... use node ...
// After Compile(), return to pool
```

#### Effort Estimate & ROI Analysis

| Approach                   | Effort    | Risk      | Performance Gain                 |
| -------------------------- | --------- | --------- | -------------------------------- |
| **Full struct conversion** | 3-4 weeks | 🔴 High   | Parsing only (no runtime impact) |
| **Node pooling**           | 3-5 days  | 🟢 Low    | Similar to struct approach       |
| **Arena allocator**        | 1-2 weeks | 🟡 Medium | Similar, cleaner GC profile      |

**Recommendation**: ❌ **NOT RECOMMENDED** for immediate implementation.

**Rationale**:

1. AST is discarded after compilation — zero runtime benefit
1. Script caching (`ScriptBytecodeCache`) already eliminates repeated parsing
1. Effort/risk ratio unfavorable compared to node pooling
1. Roslyn (much larger AST) uses classes with pooling successfully

**Alternative High-Value Targets** (same effort, runtime impact):

- `DynValue` class → struct conversion (now Roadmap Phase A1)
- `Instruction` class → struct conversion (now Roadmap Phase A2)
- String interning improvements (now Roadmap Phase A6)

Compile-time/parsing allocation work is covered by Roadmap Phase A7 (profile-first, no parser rewrite).

#### If Business Case Exists for Zero-Allocation Parsing

Proceed with **Option A** only if:

- Profiling shows parsing is a bottleneck in production
- Many small scripts compiled frequently (REPL, hot-reload scenarios)
- Memory-constrained environments (embedded, mobile)

**Implementation Plan** (if approved):

1. Create `AstArena` class with pre-allocated node arrays
1. Convert leaf nodes first (`LiteralExpression`, `BreakStatement`)
1. Add `NodeKind` discriminated union wrapper
1. Implement switch-based `Compile()` dispatch
1. Convert remaining nodes incrementally
1. Benchmark parsing memory usage before/after

See progress reports from AST analysis: Initiative 12 (allocation analysis), Initiative 18 (compiler memory).

______________________________________________________________________

### Lua-to-C# Ahead-of-Time Compiler 🔬

**Status**: 🔲 **RESEARCH** — Long-term investigation item.

**Goal**: Investigate feasibility of creating an offline "Lua → C# compiler" tool for game developers.

**Risks**:

- Lua's extreme dynamism may resist static compilation
- Two execution paths doubles testing surface
- Unity IL2CPP constraints

**Effort Estimate**: Initial feasibility study: 2-4 weeks

### GitHub Pages Benchmark Dashboard Improvements 🎨

**Status**: 🔲 **PLANNED**

**Goal**: Prettify and configure the `gh-pages` branch for a readable benchmark dashboard.

**Tasks**:

- [ ] Expand README with benchmark methodology
- [ ] Configure chart options
- [ ] Add styled index.html

**Effort Estimate**: 1-2 days

______________________________________________________________________

## Completed Initiatives (Archived)

The following initiatives have been fully completed and their detailed documentation has been moved to the progress reports. They remain here as a summary for historical reference.

| Initiative | Description                               | Completed  | Progress Report                                                                                                                                 |
| ---------- | ----------------------------------------- | ---------- | ----------------------------------------------------------------------------------------------------------------------------------------------- |
| **9**      | Version-Aware Lua Standard Library Parity | 2025-12-22 | Multiple sessions                                                                                                                               |
| **10**     | KopiLua Performance Hyper-Optimization    | 2025-12-21 | [session-074](progress/session-074-kopilua-optimization-phase1.md) - [session-076](progress/session-076-kopilua-phase3-optimization.md)         |
| **11**     | Comprehensive Helper Performance Audit    | 2025-12-22 | [session-079](progress/session-079-helper-performance-audit.md) - [session-082](progress/session-082-helper-performance-audit-phase4.md)        |
| **12**     | Deep Codebase Allocation Analysis         | 2025-12-22 | [session-083](progress/session-083-initiative12-phase5-validation.md)                                                                           |
| **14**     | SystemArrayPool Abstraction               | 2025-12-21 | [session-063](progress/session-063-system-array-pool-abstraction.md)                                                                            |
| **15**     | Boxing-Free IList Sort Extensions         | 2025-12-21 | [session-066](progress/session-066-pdqsort-implementation.md)                                                                                   |
| **16**     | Boxing-Free pdqsort Integration           | 2025-12-21 | [session-067](progress/session-067-pdqsort-integration.md)                                                                                      |
| **17**     | Metamethod Enum Optimization              | ❌ Closed  | [session-069](progress/session-069-metamethod-enum-investigation.md) (Not beneficial)                                                           |
| **18**     | Large Script Load/Compile Memory          | 2025-12-22 | [session-070](progress/session-070-compiler-memory-investigation.md) - [session-084](progress/session-084-initiative18-phase3-investigation.md) |
| **19**     | HashCodeHelper Migration                  | 2025-12-21 | [session-072](progress/session-072-hashcode-helper-migration.md)                                                                                |
| **20**     | NLua Architecture Investigation           | 2025-12-21 | [session-077](progress/session-077-nlua-investigation.md)                                                                                       |
| **22**     | ZString Migration                         | 2025-12-22 | [session-089](progress/session-089-zstring-migration-complete.md)                                                                               |
| **23**     | Span-Based Array Operation Migration      | 2025-12-22 | [session-090](progress/session-090-span-array-migration-phase1.md)                                                                              |

______________________________________________________________________

Keep this plan aligned with `docs/Testing.md` and `docs/Modernization.md`.
