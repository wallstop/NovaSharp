# Skill: Delegate Caching

**When to use**: Eliminating hidden allocations from delegate creation and lambda expressions in hot paths.

**Related Skills**: [refactor-to-zero-alloc](refactor-to-zero-alloc.md) (general refactoring), [high-performance-csharp](high-performance-csharp.md) (performance patterns)

______________________________________________________________________

## 🔴 The Problem: Delegate Allocation

Every time you create a delegate (including lambdas), memory is allocated:

```csharp
// ❌ BAD: Allocates delegate object EVERY iteration
for (int i = 0; i < 100000; i++)
{
    Func<int> fn = SomeMethod;  // 52+ bytes allocated!
    total += fn();
}
// 100,000 iterations × 52 bytes = 5.2 MB garbage!
```

### Allocation Sizes

| Delegate Type                  | Approximate Size                         |
| ------------------------------ | ---------------------------------------- |
| Static method delegate         | 52-64 bytes                              |
| Instance method delegate       | 52-64 bytes + captures `this`            |
| Lambda (no capture)            | 52-64 bytes                              |
| Lambda (capturing 1 variable)  | 52-64 bytes + closure object (32+ bytes) |
| Lambda (capturing N variables) | 52-64 bytes + closure with all captures  |

______________________________________________________________________

## 🔴 Pattern 1: Static Field Caching

Cache delegates as static readonly fields when they don't need instance state:

```csharp
// ❌ BAD: Creates new delegate each call
public void ProcessItems(List<Item> items)
{
    items.Sort((a, b) => a.Priority.CompareTo(b.Priority));  // Allocates!
}

// ✅ GOOD: Cached static delegate
private static readonly Comparison<Item> PriorityComparison = 
    static (a, b) => a.Priority.CompareTo(b.Priority);

public void ProcessItems(List<Item> items)
{
    items.Sort(PriorityComparison);  // Zero allocation
}
```

### Multiple Cached Delegates

```csharp
public static class ItemComparisons
{
    public static readonly Comparison<Item> ByPriority = 
        static (a, b) => a.Priority.CompareTo(b.Priority);
    
    public static readonly Comparison<Item> ByName = 
        static (a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal);
    
    public static readonly Comparison<Item> ByDate = 
        static (a, b) => a.Date.CompareTo(b.Date);
    
    public static readonly Predicate<Item> IsActive = 
        static item => item.IsActive;
    
    public static readonly Func<Item, int> GetPriority = 
        static item => item.Priority;
}
```

______________________________________________________________________

## 🔴 Pattern 2: Instance Field Caching

When the delegate needs access to instance state, cache it as an instance field:

```csharp
// ❌ BAD: Creates closure every call
public class Processor
{
    private int _threshold;
    
    public void Process(List<Item> items)
    {
        // Captures 'this' implicitly - allocates!
        var filtered = items.FindAll(item => item.Value > _threshold);
    }
}

// ✅ GOOD: Cached instance delegate
public class Processor
{
    private int _threshold;
    private Predicate<Item> _thresholdPredicate;
    
    public Processor()
    {
        // Cache once at construction
        _thresholdPredicate = item => item.Value > _threshold;
    }
    
    public void Process(List<Item> items)
    {
        // Uses cached delegate - zero allocation per call
        var filtered = items.FindAll(_thresholdPredicate);
    }
}
```

### When Threshold Changes

```csharp
// ✅ If the captured value rarely changes, cache and update when needed
public class Processor
{
    private int _threshold;
    private Predicate<Item> _thresholdPredicate;
    
    public int Threshold
    {
        get => _threshold;
        set
        {
            _threshold = value;
            // Recreate delegate only when value changes
            _thresholdPredicate = item => item.Value > _threshold;
        }
    }
}
```

______________________________________________________________________

## 🔴 Pattern 3: Static Lambda (C# 9+)

Use the `static` modifier on lambdas to prevent accidental captures:

```csharp
// ❌ BAD: Accidentally captures 'this'
public class MyClass
{
    private int _value;
    
    public void DoWork()
    {
        // Even though we don't use _value, the lambda could capture 'this'
        items.ForEach(item => Console.WriteLine(item));  // May allocate!
    }
}

// ✅ GOOD: Static lambda cannot capture anything
public class MyClass
{
    private int _value;
    
    public void DoWork()
    {
        // Compiler error if you try to access _value or 'this'
        items.ForEach(static item => Console.WriteLine(item));  // Zero allocation
    }
}
```

### Static Lambda Benefits

1. **Compile-time enforcement** — Cannot accidentally capture
1. **Zero allocation** — No closure object created
1. **Documentation** — Makes intent clear

```csharp
// ✅ Static lambdas for common operations
list.Sort(static (a, b) => a.CompareTo(b));
list.FindAll(static x => x > 0);
list.ForEach(static x => Process(x));  // Process must be static
```

______________________________________________________________________

## 🔴 Pattern 4: Avoid Method Group Allocation

Method groups (passing a method without calling it) also allocate:

```csharp
// ❌ BAD: Method group creates new delegate each call
void ProcessAll(List<Item> items)
{
    items.ForEach(ProcessItem);  // Allocates delegate!
}

private void ProcessItem(Item item) { /* ... */ }

// ✅ GOOD: Cache the delegate
private readonly Action<Item> _processItemDelegate;

public MyClass()
{
    _processItemDelegate = ProcessItem;  // Cache once
}

void ProcessAll(List<Item> items)
{
    items.ForEach(_processItemDelegate);  // Zero allocation
}

// ✅ ALTERNATIVE: Use explicit loop
void ProcessAll(List<Item> items)
{
    for (int i = 0; i < items.Count; i++)
    {
        ProcessItem(items[i]);  // Direct call, no delegate
    }
}
```

______________________________________________________________________

## 🔴 Pattern 5: Event Handler Caching

Event handlers are a common source of allocation:

```csharp
// ❌ BAD: Allocates new delegate every Subscribe call
public class Subscriber
{
    public void Subscribe(EventSource source)
    {
        source.OnEvent += HandleEvent;  // Allocates!
    }
    
    public void Unsubscribe(EventSource source)
    {
        source.OnEvent -= HandleEvent;  // Also allocates to compare!
    }
    
    private void HandleEvent(object sender, EventArgs e) { }
}

// ✅ GOOD: Cache the handler
public class Subscriber
{
    private readonly EventHandler _handleEvent;
    
    public Subscriber()
    {
        _handleEvent = HandleEvent;  // Cache once
    }
    
    public void Subscribe(EventSource source)
    {
        source.OnEvent += _handleEvent;  // Zero allocation
    }
    
    public void Unsubscribe(EventSource source)
    {
        source.OnEvent -= _handleEvent;  // Zero allocation
    }
    
    private void HandleEvent(object sender, EventArgs e) { }
}
```

______________________________________________________________________

## 🔴 Pattern 6: Action/Func with State Parameter

Some APIs accept a state parameter to avoid closures:

```csharp
// ❌ BAD: Closure captures 'threshold'
int threshold = 10;
bool found = Array.Exists(items, x => x.Value > threshold);  // Allocates!

// ✅ GOOD: Design APIs with state parameter
public static bool Find<T, TState>(
    T[] array, 
    TState state, 
    Func<T, TState, bool> predicate)
{
    for (int i = 0; i < array.Length; i++)
    {
        if (predicate(array[i], state))
            return true;
    }
    return false;
}

// Usage: static lambda, state passed explicitly
int threshold = 10;
bool found = Find(items, threshold, static (x, thresh) => x.Value > thresh);
```

### Implementing State-Aware APIs

```csharp
// ✅ Provide overloads with state parameter
public static class ListExtensions
{
    // Without state (for static lambdas or cached delegates)
    public static T Find<T>(this List<T> list, Predicate<T> predicate)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (predicate(list[i]))
                return list[i];
        }
        return default;
    }
    
    // With state (to avoid closures)
    public static T Find<T, TState>(
        this List<T> list, 
        TState state, 
        Func<T, TState, bool> predicate)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (predicate(list[i], state))
                return list[i];
        }
        return default;
    }
}
```

______________________________________________________________________

## 🔴 Detecting Delegate Allocations

### Regex Patterns

```bash
# Method groups in hot paths
rg '\.\w+\([A-Z][a-zA-Z]+\)' src/ --type cs | grep -v 'new\|\.Get\|\.Set'

# Lambda expressions (potential closures)
rg '=>' src/ --type cs

# Delegate assignments in loops
rg 'for\s*\(.*\)[^{]*\{[^}]*=>\s*' src/ --type cs
```

### Memory Profiler

Look for:

- `System.Action` allocations
- `System.Func` allocations
- Compiler-generated closure classes (`<>c__DisplayClass`)

______________________________________________________________________

## Quick Reference

| Pattern                   | When to Use                                   |
| ------------------------- | --------------------------------------------- |
| **Static readonly field** | Delegate with no instance state needed        |
| **Instance field**        | Delegate that captures instance state         |
| **Static lambda**         | Ad-hoc lambda that shouldn't capture anything |
| **State parameter**       | API design to avoid closures in callers       |
| **Explicit loop**         | When delegate overhead isn't worth it         |

| Delegate Source          | Allocation             | Fix                       |
| ------------------------ | ---------------------- | ------------------------- |
| `list.ForEach(x => ...)` | Yes (unless static)    | Static lambda or for loop |
| `list.Sort(comparison)`  | Yes (if new each time) | Cache Comparison<T>       |
| `event += Handler`       | Yes (method group)     | Cache EventHandler        |
| `Func<T> fn = Method`    | Yes                    | Cache in field            |

______________________________________________________________________

## Checklist

Before using delegates in hot paths:

- [ ] **Is it a static lambda?** (`static (x) => ...`)
- [ ] **Is it cached in a field?** (static or instance)
- [ ] **Does it capture any variables?** → Eliminate capture or cache
- [ ] **Is it in a loop?** → Move outside or use explicit loop
- [ ] **Can it be replaced with a direct call?** → Use for loop

______________________________________________________________________

## Resources

- [refactor-to-zero-alloc](refactor-to-zero-alloc.md) — General closure elimination
- [high-performance-csharp](high-performance-csharp.md) — Performance patterns
- [params-elimination](params-elimination.md) — Related allocation patterns
