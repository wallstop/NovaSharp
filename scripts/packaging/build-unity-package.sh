#!/usr/bin/env bash
#
# build-unity-package.sh - Build NovaSharp Unity package
#
# Usage: ./scripts/packaging/build-unity-package.sh [--version VERSION] [--output DIR]
#
# Builds the NovaSharp assemblies for Unity (netstandard2.1) and creates a
# Unity-compatible package structure with proper meta files and package.json.
#

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

# Default values
VERSION="3.0.0"
OUTPUT_DIR="$REPO_ROOT/artifacts/unity"
CONFIGURATION="Release"

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --version)
            VERSION="$2"
            shift 2
            ;;
        --output)
            OUTPUT_DIR="$2"
            shift 2
            ;;
        --configuration)
            CONFIGURATION="$2"
            shift 2
            ;;
        -h|--help)
            echo "Usage: $0 [--version VERSION] [--output DIR] [--configuration CONFIG]"
            echo ""
            echo "Options:"
            echo "  --version VERSION      Package version (default: 3.0.0)"
            echo "  --output DIR           Output directory (default: artifacts/unity)"
            echo "  --configuration CONFIG Build configuration (default: Release)"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

echo "🔨 Building NovaSharp Unity package v$VERSION"
echo "   Output: $OUTPUT_DIR"
echo "   Configuration: $CONFIGURATION"
echo ""

# Create output directories
PACKAGE_ROOT="$OUTPUT_DIR/com.wallstop-studios.novasharp"
mkdir -p "$PACKAGE_ROOT/Runtime"
mkdir -p "$PACKAGE_ROOT/Runtime/Debuggers"
mkdir -p "$PACKAGE_ROOT/Editor"
mkdir -p "$PACKAGE_ROOT/Documentation~"

# Build and publish assemblies
echo "📦 Building assemblies..."

dotnet publish "$REPO_ROOT/src/runtime/WallstopStudios.NovaSharp.Interpreter/WallstopStudios.NovaSharp.Interpreter.csproj" \
    -c "$CONFIGURATION" \
    -f netstandard2.1 \
    -o "$PACKAGE_ROOT/Runtime" \
    -p:Version="$VERSION" \
    /p:DebugType=portable

dotnet publish "$REPO_ROOT/src/debuggers/WallstopStudios.NovaSharp.RemoteDebugger/WallstopStudios.NovaSharp.RemoteDebugger.csproj" \
    -c "$CONFIGURATION" \
    -f netstandard2.1 \
    -o "$PACKAGE_ROOT/Runtime/Debuggers" \
    -p:Version="$VERSION" \
    /p:DebugType=portable

dotnet publish "$REPO_ROOT/src/debuggers/WallstopStudios.NovaSharp.VsCodeDebugger/WallstopStudios.NovaSharp.VsCodeDebugger.csproj" \
    -c "$CONFIGURATION" \
    -f netstandard2.1 \
    -o "$PACKAGE_ROOT/Runtime/Debuggers" \
    -p:Version="$VERSION" \
    /p:DebugType=portable

# Clean up unnecessary files from publish output
echo "🧹 Cleaning up..."
find "$PACKAGE_ROOT" -name "*.deps.json" -delete 2>/dev/null || true
find "$PACKAGE_ROOT" -name "*.runtimeconfig.json" -delete 2>/dev/null || true

# Move dependencies to avoid duplication in Debuggers folder
# (They're already in Runtime from the Interpreter publish)
rm -f "$PACKAGE_ROOT/Runtime/Debuggers/WallstopStudios.NovaSharp.Interpreter.dll" 2>/dev/null || true
rm -f "$PACKAGE_ROOT/Runtime/Debuggers/WallstopStudios.NovaSharp.Interpreter.pdb" 2>/dev/null || true
rm -f "$PACKAGE_ROOT/Runtime/Debuggers/WallstopStudios.NovaSharp.Interpreter.xml" 2>/dev/null || true
rm -f "$PACKAGE_ROOT/Runtime/Debuggers/WallstopStudios.NovaSharp.Interpreter.Infrastructure.dll" 2>/dev/null || true
rm -f "$PACKAGE_ROOT/Runtime/Debuggers/WallstopStudios.NovaSharp.Interpreter.Infrastructure.pdb" 2>/dev/null || true
rm -f "$PACKAGE_ROOT/Runtime/Debuggers/WallstopStudios.NovaSharp.Interpreter.Infrastructure.xml" 2>/dev/null || true
# Remove duplicate third-party dependencies from Debuggers
rm -f "$PACKAGE_ROOT/Runtime/Debuggers/CommunityToolkit.HighPerformance.dll" 2>/dev/null || true
rm -f "$PACKAGE_ROOT/Runtime/Debuggers/Microsoft.Extensions.ObjectPool.dll" 2>/dev/null || true
rm -f "$PACKAGE_ROOT/Runtime/Debuggers/System.Text.Json.dll" 2>/dev/null || true
rm -f "$PACKAGE_ROOT/Runtime/Debuggers/ZString.dll" 2>/dev/null || true

# Copy documentation
cp "$REPO_ROOT/README.md" "$PACKAGE_ROOT/Documentation~/README.md"
cp "$REPO_ROOT/LICENSE" "$PACKAGE_ROOT/LICENSE"
cp "$REPO_ROOT/docs/UnityIntegration.md" "$PACKAGE_ROOT/Documentation~/UnityIntegration.md"
cp "$REPO_ROOT/docs/ThirdPartyLicenses.md" "$PACKAGE_ROOT/Documentation~/ThirdPartyLicenses.md"

# Create package.json for Unity Package Manager
echo "📝 Creating package.json..."
cat > "$PACKAGE_ROOT/package.json" << EOF
{
  "name": "com.wallstop-studios.novasharp",
  "version": "$VERSION",
  "displayName": "NovaSharp Lua Interpreter",
  "description": "Multi-version Lua interpreter (5.1, 5.2, 5.3, 5.4) for Unity. Features comprehensive Lua compatibility, debugging support, bytecode dump/load, and seamless CLR interop. Supports IL2CPP/AOT builds.",
  "unity": "2021.3",
  "unityRelease": "0f1",
  "documentationUrl": "https://github.com/wallstop/NovaSharp",
  "changelogUrl": "https://github.com/wallstop/NovaSharp/releases",
  "licensesUrl": "https://github.com/wallstop/NovaSharp/blob/master/LICENSE",
  "license": "MIT",
  "keywords": [
    "lua",
    "interpreter",
    "scripting",
    "modding",
    "moonsharp"
  ],
  "author": {
    "name": "Wallstop Studios",
    "url": "https://github.com/wallstop"
  },
  "repository": {
    "type": "git",
    "url": "https://github.com/wallstop/NovaSharp.git"
  },
  "samples": [
    {
      "displayName": "Basic Usage",
      "description": "Basic examples of running Lua scripts from C#",
      "path": "Samples~/BasicUsage"
    }
  ]
}
EOF

# Create assembly definition for Runtime
echo "📝 Creating assembly definitions..."
cat > "$PACKAGE_ROOT/Runtime/WallstopStudios.NovaSharp.Runtime.asmdef" << 'EOF'
{
    "name": "WallstopStudios.NovaSharp.Runtime",
    "rootNamespace": "WallstopStudios.NovaSharp",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "WallstopStudios.NovaSharp.Interpreter.dll",
        "WallstopStudios.NovaSharp.Interpreter.Infrastructure.dll",
        "CommunityToolkit.HighPerformance.dll",
        "Microsoft.Extensions.ObjectPool.dll",
        "System.Text.Json.dll",
        "ZString.dll"
    ],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": true
}
EOF

cat > "$PACKAGE_ROOT/Runtime/Debuggers/WallstopStudios.NovaSharp.Debuggers.asmdef" << 'EOF'
{
    "name": "WallstopStudios.NovaSharp.Debuggers",
    "rootNamespace": "WallstopStudios.NovaSharp",
    "references": [
        "WallstopStudios.NovaSharp.Runtime"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "WallstopStudios.NovaSharp.RemoteDebugger.dll",
        "WallstopStudios.NovaSharp.VsCodeDebugger.dll"
    ],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": true
}
EOF

# Create basic sample
mkdir -p "$PACKAGE_ROOT/Samples~/BasicUsage"
cat > "$PACKAGE_ROOT/Samples~/BasicUsage/BasicUsage.cs" << 'EOF'
// Basic NovaSharp usage example for Unity
// Copy this file to your Assets folder to use

using UnityEngine;
using WallstopStudios.NovaSharp.Interpreter;

public class NovaSharpBasicUsage : MonoBehaviour
{
    void Start()
    {
        // Create a new Lua script instance
        Script script = new Script();

        // Execute a simple Lua script
        script.DoString(@"
            print('Hello from Lua!')
            
            function greet(name)
                return 'Hello, ' .. name .. '!'
            end
        ");

        // Call a Lua function from C#
        DynValue result = script.Call(script.Globals["greet"], "Unity");
        Debug.Log(result.String); // Outputs: Hello, Unity!

        // Set a global variable accessible from Lua
        script.Globals["unityVersion"] = DynValue.NewString(Application.unityVersion);

        // Execute more Lua code that uses the variable
        script.DoString("print('Running on Unity ' .. unityVersion)");
    }
}
EOF

cat > "$PACKAGE_ROOT/Samples~/BasicUsage/BasicUsage.cs.meta" << 'EOF'
fileFormatVersion: 2
guid: a1b2c3d4e5f6789012345678abcdef01
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
EOF

# Create CHANGELOG
cat > "$PACKAGE_ROOT/CHANGELOG.md" << EOF
# Changelog

## [$VERSION] - $(date +%Y-%m-%d)

### Added
- Initial Unity Package Manager release
- NovaSharp Lua interpreter with multi-version support (5.1, 5.2, 5.3, 5.4)
- Remote debugger for browser-based debugging
- VS Code debugger adapter for IDE integration
- Comprehensive CLR interop support
- Sandbox infrastructure with instruction/memory limits

### Notes
- Requires Unity 2021.3 or later
- Supports IL2CPP/AOT builds
- See documentation for detailed usage instructions
EOF

# Create LICENSE.md (Unity prefers .md extension in packages)
cp "$REPO_ROOT/LICENSE" "$PACKAGE_ROOT/LICENSE.md"

echo ""
echo "✅ Unity package built successfully!"
echo ""
echo "Package contents:"
find "$PACKAGE_ROOT" -type f | sort | sed "s|$PACKAGE_ROOT/|  |"
echo ""
echo "To use in Unity:"
echo "  1. Open Unity Package Manager (Window > Package Manager)"
echo "  2. Click '+' > Add package from disk..."
echo "  3. Navigate to: $PACKAGE_ROOT/package.json"
echo ""
echo "Or add to your project's manifest.json:"
echo "  \"com.wallstop-studios.novasharp\": \"file:$(realpath "$PACKAGE_ROOT")\""
