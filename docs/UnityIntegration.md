# Unity Integration Guide

NovaSharp is developed and tested in a modern .NET environment (netstandard2.1 / net8.0) and then consumed from Unity. This guide explains how to turn a local build into Unity-ready assets without relying on legacy synchronisation scripts.

## 1. Build the Runtime for Unity

Target `netstandard2.1`, which is supported by Unity 2021+ (including IL2CPP players):

```powershell
dotnet publish src/runtime/NovaSharp.Interpreter/NovaSharp.Interpreter.csproj `
    -c Release `
    -f netstandard2.1 `
    -o artifacts/unity/Interpreter
```

This produces:

- `NovaSharp.Interpreter.dll` – core runtime (required)
- `NovaSharp.Interpreter.pdb` – optional, include for better stack traces in development builds
- `NovaSharp.Interpreter.xml` – optional XML documentation for IDE hints inside Unity

Repeat for companion assemblies that ship with the interpreter:

```powershell
dotnet publish src/debuggers/NovaSharp.RemoteDebugger/NovaSharp.RemoteDebugger.csproj `
    -c Release -f netstandard2.1 -o artifacts/unity/RemoteDebugger

dotnet publish src/debuggers/NovaSharp.VsCodeDebugger/NovaSharp.VsCodeDebugger.csproj `
    -c Release -f netstandard2.1 -o artifacts/unity/VsCodeDebugger
```

> **Tip:** Use a staging folder such as `artifacts/unity` so the copies you drag into Unity are clearly separated from other build artefacts.

## 2. Copy into a Unity Project

Inside your Unity project, create (or reuse) the following layout:

```
Assets/
  Plugins/
    NovaSharp/
      Interpreter/
      Debugger/
```

Copy the published DLLs into the matching folders:

- `assets/Plugins/NovaSharp/Interpreter/` → `NovaSharp.Interpreter.dll` (+ optional PDB/XML)
- `assets/Plugins/NovaSharp/Debugger/` → `NovaSharp.RemoteDebugger.dll`, `NovaSharp.VsCodeDebugger.dll`

For IL2CPP builds, keep the PDBs out of the final player by leaving their `Inspector → Assembly Definition` settings unchecked, or omit them entirely once debugging is complete.

### Apply Manifest Compatibility Before Spinning Up Scripts

Mods that ship a `mod.json` next to their entry point can declare the Lua profile they require. Call the runtime helper before instantiating each `Script` so the correct modules are registered (Lua 5.2 vs 5.4 features, `warn`, `table.move`, etc.):

```csharp
using NovaSharp.Interpreter;
using NovaSharp.Interpreter.Loaders;
using NovaSharp.Interpreter.Modding;
using UnityEngine;

string modRoot = Path.GetDirectoryName(entryPointPath); // e.g., Application.streamingAssetsPath + "/Mods/SampleMod"
ScriptOptions baseOptions = new ScriptOptions(Script.DefaultOptions)
{
    ScriptLoader = new FileSystemScriptLoader(),
};

Script script = ModManifestCompatibility.CreateScriptFromDirectory(
    modRoot,
    CoreModules.PresetComplete,
    baseOptions,
    info => Debug.Log($"[NovaSharp] {info}"),
    warning => Debug.LogWarning($"[NovaSharp] {warning}")
);

script.DoFile(entryPointPath);
```

When no manifest is present, the helper returns the original options unchanged (it simply calls `TryApplyFromDirectory` under the hood), so it is safe to invoke for every mod load.

#### Remote Debugger Quick Start

When hosting the remote debugger inside Unity (or any .NET game host), you can let the debugger service create and attach manifest-aware scripts directly:

```csharp
using System.Diagnostics;
using NovaSharp.Interpreter;
using NovaSharp.Interpreter.Loaders;
using NovaSharp.Interpreter.Modules;
using NovaSharp.Interpreter.Modding;
using NovaSharp.RemoteDebugger;
using UnityEngine;

RemoteDebuggerService debugger = new RemoteDebuggerService();
ScriptOptions debugOptions = new ScriptOptions(Script.DefaultOptions)
{
    ScriptLoader = new FileSystemScriptLoader(),
};

string modRoot = Path.GetDirectoryName(entryPointPath);
Script script = debugger.AttachFromDirectory(
    modRoot,
    "Streaming Assets Mod",
    CoreModules.PresetComplete,
    debugOptions,
    info => Debug.Log($"[NovaSharp] {info}"),
    warning => Debug.LogWarning($"[NovaSharp] {warning}")
);

script.DoFile(entryPointPath);
string debuggerUrl = debugger.HttpUrlStringLocalHost;
if (!string.IsNullOrEmpty(debuggerUrl))
{
    Application.OpenURL(debuggerUrl); // or surface the URL in your own UI
}
```

This keeps the debugger pipeline in sync with `mod.json` declarations while reusing the same sinks/logging you already expose for the general manifest helper.

## 3. Reference the Assemblies

1. In Unity, select each DLL and ensure **Any Platform** is enabled (or restrict as needed).
1. For editor-only tools (e.g., the VS Code debugger), tick **Editor** only.
1. If you are using assembly definition files (`.asmdef`), add references to the NovaSharp assemblies so your scripts can access the runtime.

## 4. Optional: Generate a Unity Package

For team distribution, wrap the copied files in a Unity package:

```powershell
nuget install unity-nuget -OutputDirectory tools # optional helper
# Or simply zip the Assets/Plugins/NovaSharp folder and share it.
```

Document the NovaSharp version/hash alongside the package so consumers know which runtime they are using.

## 5. Keep Tests in .NET, Not Unity

All interpreter tests now live in `src/tests/NovaSharp.Interpreter.Tests` and execute via `dotnet test`. Keep Unity projects focused on runtime consumption; run the NUnit suite from the command line (CI already does this) to avoid pulling the full test harness into Unity.

______________________________________________________________________

For additional context on the build/test pipeline, see:

- `docs/Testing.md` – how to execute the consolidated NUnit suite and generate coverage.
- `docs/Modernization.md` – broader modernization milestones.
