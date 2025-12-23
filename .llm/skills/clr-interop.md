# Skill: CLR Interop (C# ↔ Lua Bridge)

**When to use**: Exposing C# types/methods to Lua or calling Lua from C#.

**Related Skills**: [lua-fixture-creation](lua-fixture-creation.md) (creating `@novasharp-only` fixtures for interop tests), [tunit-test-writing](tunit-test-writing.md) (isolation attributes)

______________________________________________________________________

## Overview

NovaSharp allows seamless interop between C# and Lua through the `UserData` system.

**Key namespace**: `WallstopStudios.NovaSharp.Interpreter.Interop`

______________________________________________________________________

## Registering C# Types

### Basic registration

```csharp
// Register a type before using it in Lua
UserData.RegisterType<MyClass>();

// Or with specific access mode
UserData.RegisterType<MyClass>(InteropAccessMode.LazyOptimized);
```

### Access modes

| Mode                  | Description                 | Use Case                |
| --------------------- | --------------------------- | ----------------------- |
| `Reflection`          | Pure reflection, no caching | Debugging, rare types   |
| `LazyOptimized`       | Lazy compilation + caching  | **Recommended default** |
| `Preoptimized`        | Eager compilation           | Hot paths, known types  |
| `BackgroundOptimized` | Background compilation      | Large type sets         |

______________________________________________________________________

## Exposing Types to Lua

### Simple class

```csharp
public class Player
{
    public string Name { get; set; }
    public int Health { get; set; }
    
    public void TakeDamage(int amount)
    {
        Health -= amount;
    }
}

// Registration
UserData.RegisterType<Player>();

// Usage in script
Script script = new Script();
script.Globals["Player"] = typeof(Player);
script.DoString(@"
    local p = Player()
    p.Name = 'Hero'
    p.Health = 100
    p:TakeDamage(25)
    print(p.Health)  -- 75
");
```

### Passing instances

```csharp
Player player = new Player { Name = "Hero", Health = 100 };
script.Globals["player"] = player;
script.DoString("player:TakeDamage(10)");
// player.Health is now 90
```

______________________________________________________________________

## Controlling Visibility

### Using attributes

```csharp
public class GameConfig
{
    // Visible to Lua (default for public members)
    public int MaxPlayers { get; set; }
    
    // Hidden from Lua
    [NovaSharpHidden]
    public string InternalSecret { get; set; }

    // Visible with different name
    [NovaSharpUserDataMetamethod("__tostring")]
    public string ToLuaString() => $"GameConfig({MaxPlayers})";
}
```

### Attribute reference

| Attribute                                 | Purpose                     |
| ----------------------------------------- | --------------------------- |
| `[NovaSharpHidden]`                       | Hide member from Lua        |
| `[NovaSharpVisible(true/false)]`          | Explicit visibility control |
| `[NovaSharpUserDataMetamethod("__name")]` | Expose as metamethod        |

______________________________________________________________________

## Calling Lua from C\#

### Execute code

```csharp
Script script = new Script();
DynValue result = script.DoString("return 1 + 2");
int value = (int)result.Number;  // 3
```

### Call Lua functions

```csharp
script.DoString(@"
    function greet(name)
        return 'Hello, ' .. name
    end
");

DynValue greetFunc = script.Globals.Get("greet");
DynValue result = script.Call(greetFunc, "World");
string message = result.String;  // "Hello, World"
```

### Pass callbacks

```csharp
script.Globals["log"] = (Action<string>)(msg => Console.WriteLine(msg));
script.DoString("log('Hello from Lua!')");
```

______________________________________________________________________

## Working with Tables

### Create table in C\#

```csharp
Table table = new Table(script);
table["name"] = "Item";
table["value"] = 42;
table[1] = "first";
table[2] = "second";

script.Globals["myTable"] = table;
```

### Read table from Lua

```csharp
script.DoString("result = { a = 1, b = 2, 'x', 'y' }");
Table result = script.Globals.Get("result").Table;

int a = (int)result.Get("a").Number;  // 1
string first = result.Get(1).String;   // "x"

// Iterate
foreach (TablePair pair in result.Pairs)
{
    Console.WriteLine($"{pair.Key} = {pair.Value}");
}
```

______________________________________________________________________

## DynValue Conversions

### C# to DynValue

```csharp
DynValue.NewNumber(42);
DynValue.NewString("hello");
DynValue.NewBoolean(true);
DynValue.NewTable(script);
DynValue.NewCallback(myFunc);
DynValue.Nil;
```

### DynValue to C\#

```csharp
double num = dynValue.Number;
string str = dynValue.String;
bool b = dynValue.Boolean;
Table t = dynValue.Table;
Closure f = dynValue.Function;

// Safe conversion
if (dynValue.Type == DataType.Number)
    double n = dynValue.Number;
```

______________________________________________________________________

## Best Practices

### 1. Register types early

```csharp
// Do this once at startup
UserData.RegisterType<Player>();
UserData.RegisterType<Enemy>();
UserData.RegisterType<GameConfig>();
```

### 2. Use LazyOptimized for performance

```csharp
UserData.RegisterType<FrequentlyUsedClass>(InteropAccessMode.LazyOptimized);
```

### 3. Prefer methods over properties for complex operations

```csharp
// Good: Method for expensive operation
public List<Item> GetInventory() { ... }

// Avoid: Property that does heavy work
public List<Item> Inventory => /* expensive */ 
```

### 4. Handle errors gracefully

```csharp
try
{
    script.DoString(luaCode);
}
catch (ScriptRuntimeException ex)
{
    Console.WriteLine($"Lua error: {ex.DecoratedMessage}");
}
catch (SyntaxErrorException ex)
{
    Console.WriteLine($"Syntax error: {ex.DecoratedMessage}");
}
```

______________________________________________________________________

## Testing Interop

```csharp
[Test]
[AllLuaVersions]
[UserDataIsolation]  // Important: isolate UserData registry
public async Task InteropTest(LuaCompatibilityVersion version)
{
    UserData.RegisterType<MyClass>();
    
    Script script = CreateScript(version);
    script.Globals["MyClass"] = typeof(MyClass);
    
    DynValue result = script.DoString("return MyClass().SomeMethod()");
    await Assert.That(result.Number).IsEqualTo(42).ConfigureAwait(false);
}
```

______________________________________________________________________

## Key Files

| File                           | Purpose              |
| ------------------------------ | -------------------- |
| `Interop/UserData.cs`          | Type registration    |
| `Interop/StandardDescriptors/` | Type descriptors     |
| `Interop/Converters/`          | Type conversion      |
| `DataTypes/DynValue.cs`        | Universal value type |
| `DataTypes/Table.cs`           | Lua table            |
