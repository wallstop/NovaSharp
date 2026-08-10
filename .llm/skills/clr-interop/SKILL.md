---
name: clr-interop
description: "Implement and verify NovaSharp C# and Lua interoperability through UserData, descriptors, registration, conversions, and calls. Use when exposing C# to Lua or calling Lua from C#."
metadata:
  category: lua
  priority: reference
  related: lua-fixture-creation, tunit-test-writing
---
# Skill: CLR Interop (C# ↔ Lua Bridge)

**Related Skills**: [lua-fixture-creation](../lua-fixture-creation/SKILL.md) (creating `@novasharp-only` fixtures for interop tests), [tunit-test-writing](../tunit-test-writing/SKILL.md) (isolation attributes)

______________________________________________________________________

## Overview

NovaSharp allows seamless interop between C# and Lua through the `UserData` system.

**Key namespaces**: `NovaSharp` for the public value facade and `WallstopStudios.NovaSharp.Interpreter.Interop` for userdata registration.

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

## Additional guidance

Read [the detailed reference](references/REFERENCE.md) for Calling Lua from C#, Working with Tables, LuaValue Conversions, Best Practices, and later sections.
