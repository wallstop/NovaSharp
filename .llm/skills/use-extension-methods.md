# Skill: Use Extension Methods

## Trigger

Use existing extension methods when:

- Working with `ReadOnlySpan<char>` for whitespace handling
- Normalizing file paths across platforms
- Converting `DynValue` collections to CLR types
- Serializing Lua tables to string format
- Checking flags on enums (`CoreModules`, `MemberDescriptorAccess`)
- Getting CLR visibility metadata from reflection types
- Working with `DataType` enum conversions

## Available Extensions

| Class                               | Key Methods                                                           | Use Case                                      |
| ----------------------------------- | --------------------------------------------------------------------- | --------------------------------------------- |
| `StringSpanExtensions`              | `TrimWhitespace()`, `HasContent()`                                    | Zero-alloc span whitespace handling           |
| `PathSpanExtensions`                | `SliceAfterLastSeparator()`, `NormalizeDirectorySeparators()`         | Cross-platform path normalization             |
| `LinqHelpers`                       | `Convert<T>()`, `OfDataType()`, `AsObjects()`                         | Filter/convert `IEnumerable<DynValue>`        |
| `SerializationExtensions`           | `Serialize()`, `SerializeValue()`                                     | Table/DynValue → Lua source string            |
| `JsonTableConverter`                | `TableToJson()`                                                       | Table → JSON string                           |
| `IListSortExtensions`               | `Sort<T,TComparer>()`                                                 | Zero-alloc list sorting with struct comparers |
| `DataTypeExtensions`                | `ToLuaTypeString()`, `ToErrorTypeString()`, `CanHaveTypeMetatables()` | DataType → Lua type names                     |
| `DescriptorHelpers`                 | `IsDelegateType()`, `GetClrVisibility()`, `IsPropertyInfoPublic()`    | Reflection type analysis                      |
| `IMemberDescriptorExtensions`       | `CanRead()`, `CanWrite()`, `CanExecute()`                             | Member descriptor access checks               |
| `CoreModulesExtensionMethods`       | `Has()`                                                               | Flag checking on `CoreModules` enum           |
| `ModuleRegister`                    | `RegisterCoreModules()`, `RegisterModuleType()`                       | Module registration on `Table`                |
| `IScriptPrivateResourceExtensions`  | `CheckScriptOwnership()`                                              | Script ownership validation                   |
| `LuaCompatibilityProfileExtensions` | `GetFeatureSummary()`                                                 | Profile feature description                   |
| `AsyncExtensions`                   | `DoStringAsync()`, `DoFileAsync()`, `CallAsync()`                     | Async wrappers for Script/Closure             |

## Collection Extensions

**LinqHelpers** (allocating, yields):

```csharp
IEnumerable<DynValue> values = table.GetValues();
IEnumerable<string> strings = values.Convert<string>(DataType.String);
IEnumerable<DynValue> tables = values.OfDataType(DataType.Table);
IEnumerable<object> objects = values.AsObjects();
```

**IListSortExtensions** (zero-alloc with struct comparer):

```csharp
list.Sort(new MyStructComparer());  // No boxing
list.Sort(index, count, comparer);  // Range sort
```

## String Extensions

Limited. Only path-related helpers exist:

```csharp
string filename = path.SliceAfterLastSeparator();  // Get filename
string normalized = path.NormalizeDirectorySeparators('/');  // Unix-style
```

## Span Extensions

**StringSpanExtensions** (zero-alloc):

```csharp
ReadOnlySpan<char> trimmed = span.TrimWhitespace();  // No string alloc
bool hasText = span.HasContent();  // Fast non-whitespace check
```

**PathSpanExtensions** helper (internal):

```csharp
PathSpanExtensions.CopyReplacingDirectorySeparators(source, destination, '/');
```

## Performance Notes

### Zero-Allocation

- `StringSpanExtensions.TrimWhitespace()` — operates on spans
- `StringSpanExtensions.HasContent()` — simple iteration
- `IListSortExtensions.Sort<T,TComparer>()` — generic struct comparer avoids boxing
- `PathSpanExtensions.SliceAfterLastSeparator()` — returns original string when possible
- `CoreModulesExtensionMethods.Has()` — inline bitwise check

### Allocating

- `LinqHelpers.*` — yield-based, allocates iterator
- `SerializationExtensions.Serialize()` — builds string via `ZStringBuilder`
- `PathSpanExtensions.NormalizeDirectorySeparators()` — allocates only when normalization needed
- `DescriptorHelpers.GetConversionMethodName()` — builds string
- `AsyncExtensions.*` — wraps in `Task` via thread pool

## What's Missing

Could benefit from additional extensions:

- **Dictionary extensions** — `GetOrAdd()`, `TryRemove()` patterns
- **StringBuilder extensions** — common append patterns
- **Array/List pool extensions** — `RentAndReturn()` helpers
- **Numeric span parsing** — zero-alloc number parsing from spans
- **String interning helpers** — `InternIfKnown()` for common Lua keywords
