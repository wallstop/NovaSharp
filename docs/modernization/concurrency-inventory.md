# Concurrency and Synchronization Inventory

This document catalogues all synchronization primitives, shared state, and threading patterns in NovaSharp to support future modernization efforts and help identify potential deadlock or race condition risks.

> **Last Updated**: 2025-12-06

## Summary

| Category               | Count | Location                         |
| ---------------------- | ----- | -------------------------------- |
| `lock` statements      | ~55   | Runtime (17), Debuggers (38)     |
| `ConcurrentDictionary` | 3     | Runtime                          |
| `Interlocked.*`        | 8     | Runtime (interop descriptors)    |
| `Volatile.Read/Write`  | 5     | Runtime (`PlatformAutoDetector`) |
| `Monitor.Wait/Pulse`   | 3     | `BlockingChannel`                |
| `SemaphoreSlim`        | 6     | Test infrastructure only         |

## Runtime Synchronization (`src/runtime/NovaSharp.Interpreter`)

### TypeDescriptorRegistry

**File**: `Interop/UserDataRegistries/TypeDescriptorRegistry.cs`

**Pattern**: State-per-scope with explicit `SyncRoot` locking.

```csharp
// Each DescriptorRegistryState holds its own SyncRoot
public object SyncRoot { get; }
public Dictionary<Type, IUserDataDescriptor> TypeRegistry { get; }
public Dictionary<Type, IUserDataDescriptor> TypeRegistryHistory { get; }
```

**Lock Sites** (8 total):

- Line 41: `lock (template.SyncRoot)` – Cloning state from template
- Line 203, 219, 294, 417, 571, 589: `lock (state.SyncRoot)` – Registry mutations

**Risk Assessment**: Low. Each scope gets an isolated state; the lock protects dictionary mutations within a scope. The pattern supports test isolation via `RegistryScope` push/pop.

______________________________________________________________________

### ExtensionMethodsRegistry

**File**: `Interop/UserDataRegistries/ExtensionMethodsRegistry.cs`

**Pattern**: State-per-scope with `SyncRoot` locking (mirrors `TypeDescriptorRegistry`).

**Lock Sites** (4 total):

- Line 117, 180, 210, 249: `lock (state.SyncRoot)` – Extension method registration

**Risk Assessment**: Low. Same pattern as `TypeDescriptorRegistry`; isolated state per scope.

______________________________________________________________________

### PlatformAutoDetector

**File**: `Platforms/PlatformAutoDetector.cs`

**Pattern**: Double-checked locking with `Volatile.Read/Write` for AOT detection.

```csharp
private static int RunningOnAotState = Unset;  // -1 = Unset, 0 = false, 1 = true
private static readonly object RunningOnAotStateGate = new();

// Double-checked lock pattern
int cachedState = Volatile.Read(ref RunningOnAotState);
if (cachedState == Unset)
{
    lock (RunningOnAotStateGate)
    {
        cachedState = Volatile.Read(ref RunningOnAotState);
        if (cachedState == Unset)
        {
            Volatile.Write(ref RunningOnAotState, /* detected value */);
        }
    }
}
```

**Lock Sites** (1 total):

- Line 83: `lock (RunningOnAotStateGate)` – One-time AOT detection

**Risk Assessment**: Low. Classic double-checked locking pattern with correct `Volatile` memory barriers. Only runs once per process.

______________________________________________________________________

### PerformanceStatistics

**File**: `Diagnostics/PerformanceStatistics.cs`

**Pattern**: Dual locking – instance `_syncRoot` and static `GlobalSyncRoot`.

```csharp
private static readonly object GlobalSyncRoot = new();
private readonly object _syncRoot = new();
```

**Lock Sites** (6 total):

- Line 53: `lock (_syncRoot)` – Setting `Enabled` property
- Lines 59, 73, 113, 146, 168: `lock (GlobalSyncRoot)` – Global stopwatch management

**Risk Assessment**: Medium. Nested locks (`_syncRoot` → `GlobalSyncRoot`) could theoretically deadlock if another thread acquires `GlobalSyncRoot` first and tries to enable stats on a different instance. However, the nesting is consistent (always instance → global), mitigating the risk.

**Recommendation**: Consider using `System.Threading.Lock` (.NET 9+) or refactoring to avoid nested locks.

______________________________________________________________________

### EventMemberDescriptor

**File**: `Interop/StandardDescriptors/ReflectionMemberDescriptors/EventMemberDescriptor.cs`

**Pattern**: Per-descriptor locking for event handler management.

```csharp
private readonly object _lock = new();
private List<Closure> _callbacks = new List<Closure>();
```

**Lock Sites** (3 total):

- Lines 269, 296, 516: `lock (_lock)` – Add/remove event handlers

**Risk Assessment**: Low. Instance-level locking; callbacks are invoked outside the lock.

______________________________________________________________________

### PropertyMemberDescriptor / FieldMemberDescriptor / MethodMemberDescriptor

**Files**: `Interop/StandardDescriptors/ReflectionMemberDescriptors/*.cs`

**Pattern**: Lock-free lazy initialization using `Interlocked.Exchange`.

```csharp
Interlocked.Exchange(ref _optimizedGetter, lambda.Compile());
```

**Usage** (8 total):

- `PropertyMemberDescriptor`: Lines 248, 268, 308, 333
- `FieldMemberDescriptor`: Lines 185, 199
- `MethodMemberDescriptor`: Lines 402, 410

**Risk Assessment**: Low. `Interlocked.Exchange` provides atomic assignment; worst case is redundant compilation (benign).

______________________________________________________________________

### ConcurrentDictionary Usage

| File                               | Line | Purpose                                                  |
| ---------------------------------- | ---- | -------------------------------------------------------- |
| `DataTypes/DataType.cs`            | 94   | Cache for unknown `DataType` enum string representations |
| `Script.cs`                        | 54   | Cache for compatibility profile constants                |
| `Errors/ScriptRuntimeException.cs` | 707  | Cache for exception message templates                    |

**Risk Assessment**: Low. All are read-heavy caches with no complex modification patterns.

______________________________________________________________________

## Debugger Synchronization

### VsCodeDebugger (`src/debuggers/NovaSharp.VsCodeDebugger`)

#### NovaSharpVsCodeDebugServer

**Pattern**: Single `_lock` object protecting mutable state.

**Lock Sites** (12 total):

- Lines 86, 115, 135, 144, 174, 183, 219, 262, 296, 318, 428

**Protected State**:

- `_debugger` – Current async debugger instance
- `_lastSentBreakpoints` – Breakpoint state
- `_watchResults` – Variable watch cache

**Risk Assessment**: Medium. Heavy lock contention possible under rapid debug events. Consider reader/writer lock for watch results.

#### AsyncDebugger

**Pattern**: Static ID lock + per-instance lock.

```csharp
private static readonly object SAsyncDebuggerIdLock = new();
private readonly object _lock = new();
```

**Lock Sites** (8 total):

- Line 73: `lock (SAsyncDebuggerIdLock)` – ID generation
- Lines 98, 125, 135, 165, 264, 314, 325, 345: `lock (_lock)` – Debugger state

**Risk Assessment**: Low. Static lock is for ID generation only; instance locks are independent.

______________________________________________________________________

### RemoteDebugger (`src/debuggers/NovaSharp.RemoteDebugger`)

#### RemoteDebugger (main class)

**Pattern**: Single `_lock` protecting script attachment.

**Lock Sites** (4 total):

- Lines 74, 100, 222, 259

**Risk Assessment**: Low. Simple mutual exclusion for attach/detach operations.

#### DebugServer

**Pattern**: Single `_lock` for command queue management.

**Lock Sites** (4 total):

- Lines 340, 456, 485, 496

**Risk Assessment**: Medium. Lock held during command execution; blocking commands could stall other requests.

#### BlockingChannel

**File**: `Threading/BlockingChannel.cs`

**Pattern**: Classic producer-consumer with `Monitor.Wait/Pulse`.

```csharp
lock (_queue)
{
    _queue.Enqueue(item);
    Monitor.Pulse(_queue);
}

lock (_queue)
{
    while (_queue.Count == 0 && !_closed)
        Monitor.Wait(_queue);
    // dequeue...
}
```

**Risk Assessment**: Low. Standard blocking queue pattern; disposal sets `_closed` and calls `Monitor.PulseAll`.

#### Utf8TcpServer

**Pattern**: Per-server `_peerListLock` for connection management.

**Lock Sites** (6 total):

- Lines 141, 152, 163, 188, 204, 273

**Protected State**: `_peers` list of connected clients.

**Risk Assessment**: Low. Lock only protects peer list mutations.

#### HttpServer

**Pattern**: Single `_lock` for HTTP session state.

**Lock Sites** (2 total):

- Lines 89, 360

**Risk Assessment**: Low. Simple session state protection.

______________________________________________________________________

## Test Infrastructure Synchronization (`src/tests/TestInfrastructure`)

### SemaphoreSlim Usage

| File                                             | Field                             | Purpose                              |
| ------------------------------------------------ | --------------------------------- | ------------------------------------ |
| `Scopes/PlatformDetectorIsolationScope.cs`       | `IsolationGate`                   | Serialize platform detector tests    |
| `TUnit/UserDataIsolationExecutor.cs`             | `IsolationGate`, `ExclusiveMutex` | Test isolation for UserData registry |
| `TUnit/ScriptGlobalOptionsIsolationAttribute.cs` | `IsolationGate`                   | Serialize global options tests       |
| `Scopes/DebugCommandScope.cs`                    | `DebugCommandLock`                | Serialize debug command tests        |

**Pattern**: All use `SemaphoreSlimScope.WaitAsync()` helper for disposable leases.

**Risk Assessment**: Low. Test-only infrastructure; semaphores prevent parallel test interference.

______________________________________________________________________

## Collections Requiring Thread Safety Analysis

These collections are mutated from multiple call sites and may need thread-safe alternatives or explicit locking:

| Collection          | Location                   | Current Protection        |
| ------------------- | -------------------------- | ------------------------- |
| `TypeRegistry`      | `TypeDescriptorRegistry`   | `lock (state.SyncRoot)` ✓ |
| `ExtensionMethods`  | `ExtensionMethodsRegistry` | `lock (state.SyncRoot)` ✓ |
| `_callbacks`        | `EventMemberDescriptor`    | `lock (_lock)` ✓          |
| `_stopwatches`      | `PerformanceStatistics`    | `lock (_syncRoot)` ✓      |
| `GlobalStopwatches` | `PerformanceStatistics`    | `lock (GlobalSyncRoot)` ✓ |
| `_peers`            | `Utf8TcpServer`            | `lock (_peerListLock)` ✓  |
| `_queue`            | `BlockingChannel`          | `lock (_queue)` ✓         |

______________________________________________________________________

## Lock Ordering Rules

To prevent deadlocks, locks must always be acquired in a consistent order. The following rules apply:

### Global Lock Ordering

When multiple locks from different components must be held simultaneously, acquire them in this order:

1. **Test infrastructure semaphores** (highest priority)

   - `UserDataIsolationExecutor.IsolationGate`
   - `UserDataIsolationExecutor.ExclusiveMutex`
   - `ScriptGlobalOptionsIsolationAttribute.IsolationGate`
   - `PlatformDetectorIsolationScope.IsolationGate`

1. **Platform detection**

   - `PlatformAutoDetector.RunningOnAotStateGate`

1. **Type registries** (alphabetical by registry name)

   - `ExtensionMethodsRegistry.state.SyncRoot`
   - `TypeDescriptorRegistry.state.SyncRoot`

1. **Performance statistics**

   - `PerformanceStatistics._syncRoot` (instance)
   - `PerformanceStatistics.GlobalSyncRoot` (static) — always acquire after instance lock

1. **Interop descriptors** (alphabetical by class)

   - `EventMemberDescriptor._lock`

1. **Debugger components** (by layer)

   - `AsyncDebugger.SAsyncDebuggerIdLock` (static ID generation)
   - `NovaSharpVsCodeDebugServer._lock`
   - `AsyncDebugger._lock` (instance)
   - `RemoteDebugger._lock`
   - `DebugServer._lock`
   - `HttpServer._lock`
   - `Utf8TcpServer._peerListLock`
   - `BlockingChannel._queue`

### Component-Specific Rules

#### PerformanceStatistics

```
Always: _syncRoot → GlobalSyncRoot (instance before static)
Never:  GlobalSyncRoot → _syncRoot (would violate ordering)
```

#### Debugger Chain

```
VS Code: NovaSharpVsCodeDebugServer._lock → AsyncDebugger._lock
Remote:  RemoteDebugger._lock → DebugServer._lock → BlockingChannel._queue
Network: HttpServer._lock and Utf8TcpServer._peerListLock are independent
```

#### Test Isolation

```
Always: IsolationGate → ExclusiveMutex (in UserDataIsolationExecutor)
```

### Lock-Free Patterns

These components use lock-free synchronization and don't participate in lock ordering:

| Component                  | Pattern                         | Notes                                   |
| -------------------------- | ------------------------------- | --------------------------------------- |
| `PropertyMemberDescriptor` | `Interlocked.Exchange`          | Lazy compilation of optimized accessors |
| `FieldMemberDescriptor`    | `Interlocked.Exchange`          | Lazy compilation of optimized accessors |
| `MethodMemberDescriptor`   | `Interlocked.Exchange`          | Lazy compilation of optimized delegates |
| `DataType` cache           | `ConcurrentDictionary.GetOrAdd` | Thread-safe string cache                |
| `Script` profile cache     | `ConcurrentDictionary.GetOrAdd` | Thread-safe constant cache              |
| `ScriptRuntimeException`   | `ConcurrentDictionary.GetOrAdd` | Thread-safe message template cache      |

### Verification

Before merging code that acquires multiple locks:

1. Verify the acquisition order matches this document
1. Add comments at lock sites referencing this ordering
1. Consider whether the operation truly requires multiple locks
1. If deadlock risk exists, refactor to avoid nested locking

______________________________________________________________________

## Potential Deadlock Scenarios

### 1. Nested Locks in PerformanceStatistics

```
Thread A: lock(_syncRoot) → lock(GlobalSyncRoot)
Thread B: lock(GlobalSyncRoot) → ???
```

**Mitigation**: The nesting is always instance → global, so circular wait is impossible. However, this could still cause contention.

### 2. Debugger Event Callbacks

Debugger locks (`AsyncDebugger._lock`, `DebugServer._lock`) are held while processing events. If a callback re-enters the debugger API, deadlock is possible.

**Mitigation**: Ensure callbacks don't call back into debugger APIs while holding locks.

______________________________________________________________________

## Recommendations

1. ✅ **Document lock ordering**: Lock ordering rules are now documented in the "Lock Ordering Rules" section above.

1. **Consider `System.Threading.Lock`**: .NET 9's `Lock` type provides `EnterScope()` with automatic scoping and better diagnostics.

1. **Reduce lock granularity in debuggers**: The VS Code debugger's single `_lock` could be split into multiple locks (breakpoints, watches, state).

1. **Add timeout to `BlockingChannel`**: The `Receive()` method blocks indefinitely; consider adding a timeout option for graceful shutdown.

1. **Audit Dispose patterns**: Ensure all locks are released when owning objects are disposed, especially in debugger cleanup paths.

1. **Add lock ordering comments**: When touching lock sites, add comments referencing the ordering rules (e.g., `// Lock order: see docs/modernization/concurrency-inventory.md`).

______________________________________________________________________

## Related Documents

- `docs/modernization/reflection-audit.md` – Reflection usage inventory
- `docs/testing/spec-audit.md` – Specification compliance tracking
- `PLAN.md` – Modernization roadmap
