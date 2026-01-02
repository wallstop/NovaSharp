______________________________________________________________________

triggers:

- "allocation"
- "GC pressure"
- "memory leak"
- "foreach allocation"
- "delegate allocation"
- "params allocation"
- "closure allocation"
  category: performance
  related:
- high-performance-csharp
- refactor-to-zero-alloc
- zstring-migration
  priority: core

______________________________________________________________________

# Skill: Allocation Traps

**When to use**: Reviewing code for hidden allocations, investigating GC pressure, or unexplained performance issues.

**Code Samples**: [pooling-patterns](../code-samples/pooling-patterns.md), [unity-gc-patterns](../code-samples/unity-gc-patterns.md)

**Related Skills**: [high-performance-csharp](high-performance-csharp.md), [refactor-to-zero-alloc](refactor-to-zero-alloc.md)

______________________________________________________________________

## Quick Reference: Allocation Costs

| Trap                           | Bytes Per Occurrence | Risk Level |
| ------------------------------ | -------------------- | ---------- |
| `foreach` on `List<T>` (Mono)  | 24 bytes             | High       |
| LINQ `.Where()/.Select()`      | 32+ bytes            | High       |
| Closure capturing local        | 32+ bytes            | High       |
| Delegate in loop               | 52 bytes             | High       |
| `params` method call           | 24+ bytes            | Medium     |
| Enum dictionary lookup         | 24 bytes             | Medium     |
| Struct without `IEquatable<T>` | 24+ bytes            | Medium     |
| Boxing to `object`             | 12+ bytes            | Medium     |
| `Enum.HasFlag()`               | 24 bytes (2x boxing) | Medium     |
| `enum.ToString()`              | 20+ bytes            | Medium     |

______________________________________________________________________

## Trap 1: foreach on List (Unity/Mono)

Unity's Mono boxes `List<T>` enumerators, allocating **24 bytes per loop**:

```csharp
// BAD: Allocates 24 bytes
foreach (DynValue item in myList) { Process(item); }

// GOOD: Zero allocation
for (int i = 0; i < myList.Count; i++) { Process(myList[i]); }
```

**Arrays are safe** - foreach on arrays is optimized.

______________________________________________________________________

## Trap 2: LINQ Methods

All LINQ methods allocate iterator objects and often delegate objects:

```csharp
// BAD: Each method allocates
List<DynValue> result = values
    .Where(v => v.Type == DataType.Number)  // Iterator + delegate
    .ToList();                               // New List

// GOOD: Explicit loop with pooling
using PooledResource<List<DynValue>> lease = ListPool<DynValue>.Get();
for (int i = 0; i < values.Count; i++)
{
    if (values[i].Type == DataType.Number)
        lease.Resource.Add(values[i]);
}
```

______________________________________________________________________

## Trap 3: Closures Capturing Variables

Lambdas that capture variables allocate closure objects:

```csharp
// BAD: Captures 'targetType' - allocates closure
DataType targetType = DataType.Number;
DynValue found = list.Find(v => v.Type == targetType);

// GOOD: Explicit loop
DynValue found = null;
for (int i = 0; i < list.Count; i++)
{
    if (list[i].Type == targetType) { found = list[i]; break; }
}
```

**Use static lambdas** (C# 9+) to prevent accidental captures:

```csharp
items.Sort(static (a, b) => a.Number.CompareTo(b.Number));
```

______________________________________________________________________

## Trap 4: Delegate Caching

Delegate creation allocates every time:

```csharp
// BAD: Allocates delegate EVERY iteration
for (int i = 0; i < count; i++)
{
    Func<DynValue> fn = GetValue;  // 52+ bytes!
    result.Add(fn());
}

// GOOD: Cache delegate
private static readonly Comparison<Item> PriorityComparison =
    static (a, b) => a.Priority.CompareTo(b.Priority);

items.Sort(PriorityComparison);  // Zero allocation
```

______________________________________________________________________

## Trap 5: params Methods

Methods with `params` allocate an array every call:

```csharp
// Method: public DynValue Call(params DynValue[] args)

// BAD: Allocates array (24+ bytes per call)
DynValue result = function.Call(arg1, arg2, arg3);

// GOOD: Use pooled array
using var pooled = DynValueArrayPool.Get(3, out DynValue[] buffer);
buffer[0] = arg1; buffer[1] = arg2; buffer[2] = arg3;
DynValue result = function.Call(buffer);
```

______________________________________________________________________

## Trap 6: Enum Dictionary Keys

Enum keys cause boxing per lookup unless you provide custom comparer:

```csharp
// BAD: Boxing per lookup
Dictionary<DataType, string> typeNames = new();
string name = typeNames[DataType.Number];  // 24 bytes!

// GOOD: Custom comparer
public readonly struct DataTypeComparer : IEqualityComparer<DataType>
{
    public bool Equals(DataType x, DataType y) => x == y;
    public int GetHashCode(DataType obj) => (int)obj;
}

var typeNames = new Dictionary<DataType, string>(new DataTypeComparer());
```

______________________________________________________________________

## Trap 7: Structs Without IEquatable

Structs in collections without `IEquatable<T>` cause boxing:

```csharp
// BAD: Boxing per comparison
public struct SourceLocation { public int Line; }
list.Contains(location);  // Boxes!

// GOOD: Implement IEquatable<T>
public readonly struct SourceLocation : IEquatable<SourceLocation>
{
    public readonly int Line;
    public bool Equals(SourceLocation other) => Line == other.Line;
    public override int GetHashCode() => Line;
}
```

______________________________________________________________________

## Trap 8: String Operations

```csharp
// BAD: O(n^2) allocations
string result = "";
for (int i = 0; i < items.Count; i++)
    result += items[i].ToString();

// GOOD: ZStringBuilder
using ZStringBuilder sb = ZStringBuilder.Create();
for (int i = 0; i < items.Count; i++)
    sb.Append(items[i].ToString());
string result = sb.ToString();
```

______________________________________________________________________

## Trap 9: Enum.HasFlag and ToString

```csharp
// BAD: HasFlag boxes BOTH enums (48 bytes!)
if (options.HasFlag(ScriptOptions.HardSandbox))

// GOOD: Bitwise check
if ((options & ScriptOptions.HardSandbox) == ScriptOptions.HardSandbox)

// BAD: enum.ToString() allocates
sb.Append(tokenType.ToString());

// GOOD: Cached lookup
sb.Append(TokenTypeStrings.GetName(tokenType));
```

______________________________________________________________________

## Detection: Finding Hidden Allocations

```bash
# LINQ usage
rg '\.Where\(|\.Select\(|\.ToList\(|\.ToArray\(' --type cs

# Collection creation
rg 'new List<|new Dictionary<|new HashSet<' --type cs

# foreach on collections (review each)
rg 'foreach.*List<|foreach.*Dictionary<' --type cs

# Enum ToString or HasFlag
rg '\.ToString\(\)|\.HasFlag\(' --type cs
```

______________________________________________________________________

## Quick Decision Tree

```text
Is it in a hot path (VM loop, frequent method)?
├── NO  → Allocation probably fine
└── YES → Check all allocation sources:
    ├── LINQ? → Replace with for loop
    ├── foreach on List/Dict? → Use for loop
    ├── Lambda captures variables? → Use static lambda or loop
    ├── Delegate created in loop? → Cache in field
    ├── params method? → Use overloads or pooled array
    └── String concatenation? → Use ZString
```
