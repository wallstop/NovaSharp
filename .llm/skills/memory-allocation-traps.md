# Skill: Memory Allocation Traps

**When to use**: When reviewing code for hidden allocations, investigating GC pressure, or when unexplained performance issues occur.

**Related Skills**: [high-performance-csharp](high-performance-csharp.md) (zero-allocation patterns), [unity-gc-patterns](unity-gc-patterns.md) (Unity GC specifics), [refactor-to-zero-alloc](refactor-to-zero-alloc.md) (migration patterns)

______________________________________________________________________

## 🔴 Quick Reference: Allocation Costs

| Trap                           | Bytes Per Occurrence | Risk Level |
| ------------------------------ | -------------------- | ---------- |
| `foreach` on `List<T>` (Mono)  | 24 bytes             | 🔴 High    |
| LINQ `.Where()`                | 32+ bytes            | 🔴 High    |
| LINQ `.Select()`               | 32+ bytes            | 🔴 High    |
| Closure capturing local        | 32+ bytes            | 🔴 High    |
| `params` method call           | 24+ bytes            | 🟡 Medium  |
| Delegate in loop               | 52 bytes             | 🔴 High    |
| Enum dictionary lookup         | 24 bytes             | 🟡 Medium  |
| Struct without `IEquatable<T>` | 24+ bytes            | 🟡 Medium  |
| String concatenation           | Varies               | 🔴 High    |
| Boxing to `object`             | 12+ bytes            | 🟡 Medium  |
| `Enum.HasFlag()`               | 24 bytes (2× boxing) | 🟡 Medium  |
| `enum.ToString()`              | 20+ bytes            | 🟡 Medium  |

______________________________________________________________________

## Trap 1: foreach on List\<T> (Mono/Unity)

Unity's Mono compiler boxes the `List<T>` enumerator, allocating **24 bytes per loop**:

```csharp
// ❌ BAD: Allocates 24 bytes
foreach (DynValue item in myList)
{
    Process(item);
}

// ✅ GOOD: Zero allocation
for (int i = 0; i < myList.Count; i++)
{
    Process(myList[i]);
}
```

**Note**: `foreach` on arrays is optimized and does NOT allocate:

```csharp
// ✅ OK: Arrays are optimized
foreach (DynValue item in myArray)  // Zero allocation
{
    Process(item);
}
```

### Non-Indexable Collections (HashSet, Dictionary)

```csharp
// ❌ BAD: foreach allocates enumerator
foreach (KeyValuePair<string, DynValue> kvp in dictionary) { }

// ✅ GOOD: Use struct enumerator directly
Dictionary<string, DynValue>.Enumerator enumerator = dictionary.GetEnumerator();
while (enumerator.MoveNext())
{
    KeyValuePair<string, DynValue> kvp = enumerator.Current;
    // process...
}
```

______________________________________________________________________

## Trap 2: LINQ Methods

All LINQ methods allocate iterator objects and often delegate objects:

```csharp
// ❌ BAD: Each method allocates
List<DynValue> result = values
    .Where(v => v.Type == DataType.Number)    // Iterator + delegate allocation
    .Select(v => v.Number)                     // Another iterator + delegate
    .ToList();                                 // New List allocation

// ✅ GOOD: Explicit loop with pooling
using PooledResource<List<DynValue>> lease = ListPool<DynValue>.Get();
List<DynValue> result = lease.Resource;
for (int i = 0; i < values.Count; i++)
{
    if (values[i].Type == DataType.Number)
    {
        result.Add(values[i]);
    }
}
```

### LINQ Allocation Breakdown

| Method       | Allocations                          |
| ------------ | ------------------------------------ |
| `.Where()`   | WhereIterator + delegate             |
| `.Select()`  | SelectIterator + delegate            |
| `.Any()`     | Delegate (no iterator if early exit) |
| `.First()`   | Delegate                             |
| `.ToList()`  | New `List<T>`                        |
| `.ToArray()` | New `T[]`                            |
| `.Count()`   | Enumeration (may allocate)           |
| `.Sum()`     | Delegate                             |
| `.OrderBy()` | Buffer + comparer                    |

______________________________________________________________________

## Trap 3: Closures Capturing Variables

Lambdas that capture local variables allocate closure objects:

```csharp
// ❌ BAD: Captures 'targetType' - allocates closure (32+ bytes)
DataType targetType = DataType.Number;
DynValue found = list.Find(v => v.Type == targetType);

// ❌ BAD: Captures 'this' - allocates closure
items.RemoveAll(x => x.Owner == this);

// ✅ GOOD: Explicit loop - zero allocation
DynValue found = null;
for (int i = 0; i < list.Count; i++)
{
    if (list[i].Type == targetType)
    {
        found = list[i];
        break;
    }
}
```

### Static Lambdas (C# 9+)

```csharp
// ✅ Static lambda - compiler error if it tries to capture
items.Sort(static (a, b) => a.Number.CompareTo(b.Number));

// ✅ Cached delegate - single allocation at class load
private static readonly Comparison<DynValue> NumberComparison =
    static (a, b) => a.Number.CompareTo(b.Number);

items.Sort(NumberComparison);
```

______________________________________________________________________

## Trap 4: params Methods

Methods with `params` allocate an array for every call:

```csharp
// Method signature
public DynValue Call(params DynValue[] args) { }

// ❌ BAD: Allocates array (24+ bytes per call)
DynValue result = function.Call(arg1, arg2, arg3);

// ✅ GOOD: Use overloads or pass pre-existing array
DynValue[] args = DynValueArrayPool.Get(3, out DynValue[] buffer);
buffer[0] = arg1;
buffer[1] = arg2;
buffer[2] = arg3;
DynValue result = function.Call(buffer);
```

### Common params Traps

| Method                       | Allocation               |
| ---------------------------- | ------------------------ |
| `string.Format(fmt, params)` | Array + formatted string |
| `ZString.Concat(params)`     | Array (use overloads!)   |
| `Call(params DynValue[])`    | Array                    |
| `Path.Combine(params)`       | Array + result string    |

______________________________________________________________________

## Trap 5: Delegate Assignment in Loops

Assigning a method to a delegate variable boxes each iteration:

```csharp
// ❌ BAD: 52 bytes per iteration!
for (int i = 0; i < count; i++)
{
    Func<DynValue> fn = GetValue;  // Boxing each iteration
    result.Add(fn());
}

// ✅ GOOD: Assign once outside loop
Func<DynValue> fn = GetValue;
for (int i = 0; i < count; i++)
{
    result.Add(fn());
}
```

______________________________________________________________________

## Trap 6: Enum Dictionary Keys

Enum keys cause boxing on every dictionary operation unless you provide a custom comparer:

```csharp
// ❌ BAD: Boxing per lookup (24 bytes per lookup!)
Dictionary<DataType, string> typeNames = new Dictionary<DataType, string>();
string name = typeNames[DataType.Number];

// ✅ GOOD: Custom comparer (zero allocation)
public readonly struct DataTypeComparer : IEqualityComparer<DataType>
{
    public bool Equals(DataType x, DataType y) => x == y;
    public int GetHashCode(DataType obj) => (int)obj;
}

Dictionary<DataType, string> typeNames = 
    new Dictionary<DataType, string>(new DataTypeComparer());

// ✅ ALTERNATIVE: Use int keys
Dictionary<int, string> typeNames = new Dictionary<int, string>();
typeNames[(int)DataType.Number] = "number";
```

______________________________________________________________________

## Trap 7: Structs Without IEquatable\<T>

Structs used in collections without `IEquatable<T>` cause boxing:

```csharp
// ❌ BAD: Boxing per comparison
public struct SourceLocation
{
    public int Line;
    public int Column;
}

list.Contains(location);  // Boxes each comparison!

// ✅ GOOD: Implement IEquatable<T>
public readonly struct SourceLocation : IEquatable<SourceLocation>
{
    public readonly int Line;
    public readonly int Column;

    public bool Equals(SourceLocation other) => Line == other.Line && Column == other.Column;
    public override bool Equals(object obj) => obj is SourceLocation s && Equals(s);
    public override int GetHashCode() => HashCodeHelper.HashCode(Line, Column);
    
    public static bool operator ==(SourceLocation left, SourceLocation right) => left.Equals(right);
    public static bool operator !=(SourceLocation left, SourceLocation right) => !left.Equals(right);
}
```

______________________________________________________________________

## Trap 8: String Operations

Strings are immutable; every modification creates a new string:

```csharp
// ❌ BAD: O(n²) allocations
string result = "";
for (int i = 0; i < items.Count; i++)
{
    result += items[i].ToString();  // New string each iteration!
}

// ❌ BAD: Hidden allocation in interpolation
string msg = $"Value at {line}:{column} is {value}";  // Multiple allocations

// ✅ GOOD: ZStringBuilder pooling
using ZStringBuilder sb = ZStringBuilder.Create();
for (int i = 0; i < items.Count; i++)
{
    sb.Append(items[i].ToString());
}
string result = sb.ToString();
```

______________________________________________________________________

## Trap 9: Boxing Value Types

Passing structs to `object` parameters causes boxing:

```csharp
// ❌ BAD: Boxing (12+ bytes per box)
int x = 42;
object boxed = x;           // Boxes int
ArrayList list = new ArrayList();
list.Add(x);                // Boxes int

// ❌ BAD: Interface boxing (without generics)
IComparable comp = x;       // Boxes int

// ✅ GOOD: Use generic collections
List<int> list = new List<int>();
list.Add(x);                // No boxing

// ✅ GOOD: Generic interface constraint
void Compare<T>(T a, T b) where T : IComparable<T>
{
    a.CompareTo(b);         // No boxing
}
```

______________________________________________________________________

## Trap 10: Enum.HasFlag() and enum.ToString()

Standard enum operations cause boxing:

```csharp
// ❌ BAD: Enum.HasFlag boxes BOTH enums (48 bytes!)
if (options.HasFlag(ScriptOptions.HardSandbox))

// ✅ GOOD: Bitwise check (zero allocation)
if ((options & ScriptOptions.HardSandbox) == ScriptOptions.HardSandbox)

// ❌ BAD: enum.ToString() allocates every call
sb.Append(tokenType.ToString());

// ✅ GOOD: Use cached string lookup
sb.Append(TokenTypeStrings.GetName(tokenType));
```

______________________________________________________________________

## Detection: Finding Hidden Allocations

### Search Patterns (Regex)

Use these with `rg` (ripgrep) to find allocation sites:

```bash
# LINQ usage
rg "\.Where\(|\.Select\(|\.Any\(|\.First\(|\.ToList\(|\.ToArray\(" --type cs

# Collection creation in methods
rg "new List<|new Dictionary<|new HashSet<|new Queue<|new Stack<" --type cs

# String operations in loops
rg "\+\s*\"|string\.Format|string\.Concat" --type cs

# foreach on collections (review each)
rg "foreach.*List<|foreach.*Dictionary<|foreach.*HashSet<" --type cs

# Potential closures (lambdas with external references)
rg "=>\s*[^;]*[a-z_][a-zA-Z0-9_]*[^(]" --type cs

# Enum ToString or HasFlag
rg "\.ToString\(\)|\.HasFlag\(" --type cs
```

### Code Review Checklist

1. **Identify hot paths**: Code in VM execution loop, opcode handlers, frequently called methods
1. **Check allocations**: Look for `new`, LINQ, closures, string operations
1. **Look for boxing**: Value types passed to `object` parameters
1. **Find LINQ usage**: Search for `.Where`, `.Select`, `.Any`, `.First`
1. **Check string operations**: `+` operator, `Format`, interpolation in loops
1. **Review collection creation**: `new List<>`, `new Dictionary<>` in methods
1. **Find foreach on List**: Replace with `for` loops (Mono boxing)
1. **Check enum dictionary keys**: Need custom `IEqualityComparer`
1. **Verify struct equality**: Must implement `IEquatable<T>`

______________________________________________________________________

## Resources

- [high-performance-csharp](high-performance-csharp.md) — Zero-allocation patterns
- [unity-gc-patterns](unity-gc-patterns.md) — Why allocations matter in Unity
- [refactor-to-zero-alloc](refactor-to-zero-alloc.md) — Migration patterns
- [zstring-migration](zstring-migration.md) — String allocation elimination
- [performance-audit](performance-audit.md) — Audit checklist
