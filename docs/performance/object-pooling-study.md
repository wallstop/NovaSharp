# NovaSharp Allocation Reduction Study

> Last updated: 2025‑11‑13\
> Context: PLAN.md – Performance Optimisation Campaign, “allocation guardrails”

## Goals

- Reduce hot-path allocations so interpreter + tooling only allocate when absolutely required.
- Mirror proven approaches (UnityHelpers `Buffers`, `ArrayPool<T>`, `ValueListBuilder<T>`, Roslyn-style pooled builders) while keeping APIs discoverable.
- Explore struct-based or buffer-populating overloads that let callers reuse containers instead of enumerating `IEnumerable<T>` sequences.
- Identify places where value-type `IDisposable` wrappers can replace heap-based implementations without breaking the existing API surface.

## Candidate Hotspots

| Area                                                    | Current Behaviour                                                 | Notes                                                                                                                             |
| ------------------------------------------------------- | ----------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------- |
| `Table.Pairs/Keys/Values`                               | Now iterator backed (no LINQ) but still allocates per enumerator. | Consider exposing `FillPairs(List<TablePair> buffer)` and returning the buffer for chaining; evaluate read-only span projections. |
| `StandardUserDataDescriptor` member discovery           | Reflection scans allocate descriptor lists + temporary arrays.    | Investigate pooling `List<MemberDescriptor>` via `ArrayPool<T>`, or generating descriptors with the Roslyn hardwire generator.    |
| `UserData.RegisterType` / `TypeDescriptorRegistry`      | Builds dictionaries each time registration occurs.                | Explore `DictionaryPool` or cached manifests generated at compile time.                                                           |
| Coroutine stacks (`Processor.Call`, `Coroutine.Resume`) | Allocate new arrays/lists when resizing stacks.                   | Assess `ArrayPool<ValueType>` usage or a slab allocator for stack frames.                                                         |
| Debugger actions (`DebuggerAction`)                     | Uses `List<DebuggerAction>` + LINQ for filtering.                 | Move to pooled list builder (e.g., `ValueListBuilder<T>` or manual stackalloc).                                                   |
| IO buffers (`UndisposableStream`, `FileUserDataBase`)   | Allocate temporary arrays for copy operations.                    | Switch to `ArrayPool<byte>.Shared` and ensure deterministic return paths.                                                         |
| Disposable helpers (`IoModule`, `ReplInterpreter`)      | Return `IDisposable` implementations backed by classes.           | Prototype `struct` disposables (e.g., `ref struct PooledHandle`) that still satisfy `IDisposable` assignment.                     |

## Proposed API Experiments

- **Buffer-Populating Overloads**

  - `Table.FillPairs(List<TablePair> destination)` – clears and appends so callers can reuse buffers.
  - `StandardUserDataDescriptor.GetMembers(List<UserDataMemberDescriptor> destination)` – avoids enumerator allocations during per-call registration.
  - Consider `Span<T>`/`ReadOnlySpan<T>` return patterns where ABI allows (requires .NET 5+ consumer awareness).

- **Value-Based `IDisposable`**

  - Introduce `struct PooledDisposable<TPool>` that wraps a pooled resource without heap allocation.
  - Ensure implicit boxing is avoided; `IDisposable temp = pooled` still works because structs implement interfaces without boxing when assigned to interface variables? Actually boxing occurs – but we can return `ref struct` or pattern-based `Dispose()` method + using declarations. Need doc on compatibility.

- **Pooling Utilities**

  - Evaluate `ArrayPool<T>` for transient `byte[]`/`DynValue[]`/`TablePair[]`.
  - Investigate a simple `ListPool<T>` built on `ArrayPool<T>` + `List<T>.Clear()`.
  - Look at UnityHelpers `Buffers` for API inspiration (`using(Buffer.Get(out var buffer)) { … }`).

- **Value Builders**

  - For short-lived sequences (e.g., argument lists), adopt `ValueListBuilder<T>` (Roslyn approach) to keep data on the stack when possible, spilling to pooled arrays if required.

## Next Steps

1. Instrument allocations (BenchmarkDotNet + `EventCounters`) to confirm hotspots before modifying APIs.
1. Prototype `Table.FillPairs` + pooled list usage in interpreter tests; measure allocation delta.
1. Evaluate struct-based disposable pattern compatibility (ensure existing consumers compiling against `IDisposable` continue to work).
1. Document findings + adoption guidance in `docs/Performance.md` before rolling out API changes.
1. Update PLAN.md as experiments land (per area).

Feel free to append more ideas or link to benchmarks once experiments begin.
