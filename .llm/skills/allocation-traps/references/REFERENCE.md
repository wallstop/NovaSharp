# Allocation Traps Reference

## Trap 5: params Methods

Methods with `params` allocate an array every call:

```csharp
// Example method: void Consume(params LuaValue[] args)

// BAD: Allocates array (24+ bytes per call)
Consume(arg1, arg2, arg3);

// GOOD: Use pooled array
using PooledResource<LuaValue[]> pooled = DynValueArrayPool.Get(3, out LuaValue[] buffer);
buffer[0] = arg1; buffer[1] = arg2; buffer[2] = arg3;
Consume(buffer);
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
