# CLR Interop (C# ↔ Lua Bridge) Reference

## Calling Lua from C\#

### Execute code

```csharp
Script script = new Script();
LuaValue result = script.DoString("return 1 + 2");
int value = checked((int)result.AsNumber());  // 3
```

### Call Lua functions

```csharp
script.DoString(@"
    function greet(name)
        return 'Hello, ' .. name
    end
");

LuaValue greetFunc = script.Globals.Get("greet");
LuaValue result = script.Call(greetFunc, "World");
string message = result.AsString();  // "Hello, World"
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
LuaTable result = script.Globals.Get("result").AsTable();

int a = (int)result.Get("a").AsNumber();  // 1
string first = result.Get(1).AsString();   // "x"
```

______________________________________________________________________

## LuaValue Conversions

### C# to LuaValue

```csharp
using LuaEngine engine = LuaEngine.Create();
LuaCallback myFunc = static (context, args) => LuaValue.Nil;

LuaValue number = LuaValue.FromNumber(42);
LuaValue integer = LuaValue.FromInteger(42);
LuaValue text = LuaValue.FromString("hello");
LuaValue boolean = LuaValue.FromBoolean(true);
LuaValue table = engine.CreateTable().ToValue();
LuaValue callback = engine.CreateCallback(myFunc);
LuaValue nil = LuaValue.Nil;
```

### LuaValue to C\#

```csharp
double num = luaValue.AsNumber();
long integer = luaValue.AsInteger();
string str = luaValue.AsString();
bool b = luaValue.AsBoolean();
LuaTable t = luaValue.AsTable();
LuaFunction f = luaValue.AsFunction();

// Safe conversion
if (luaValue.IsNumber)
{
    double n = luaValue.AsNumber();
}
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
    
    LuaValue result = script.DoString("return MyClass().SomeMethod()");
    await Assert.That(result.AsNumber()).IsEqualTo(42).ConfigureAwait(false);
}
```

______________________________________________________________________

## Key Files

| File                           | Purpose              |
| ------------------------------ | -------------------- |
| `Interop/UserData.cs`          | Type registration    |
| `Interop/StandardDescriptors/` | Type descriptors     |
| `Interop/Converters/`          | Type conversion      |
| `Api/LuaValue.cs`              | Universal value type |
| `DataTypes/Table.cs`           | Lua table            |
