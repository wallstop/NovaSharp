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

if ! command -v python3 >/dev/null 2>&1; then
    echo "build-unity-package.sh requires python3 for portable path canonicalization." >&2
    echo "Install Python 3 or add python3 to PATH before running this script." >&2
    exit 1
fi

echo "🔨 Building NovaSharp Unity package v$VERSION"
echo "   Output: $OUTPUT_DIR"
echo "   Configuration: $CONFIGURATION"
echo ""

path_is_within_or_equal() {
    local candidate="$1"
    local root="$2"
    local candidate_full
    local root_full

    candidate_full="$(canonical_path "$candidate")"
    root_full="$(canonical_path "$root")"

    [[ "$candidate_full" == "$root_full" || "$candidate_full" == "$root_full"/* ]]
}

canonical_path() {
    python3 -c 'import pathlib, sys; print(pathlib.Path(sys.argv[1]).resolve(strict=False))' "$1"
}

paths_overlap() {
    local left="$1"
    local right="$2"

    path_is_within_or_equal "$left" "$right" || path_is_within_or_equal "$right" "$left"
}

PACKAGE_TEMPLATE_ROOT="$REPO_ROOT/src/unity/com.wallstop-studios.novasharp"
SAMPLES_SOURCE="$PACKAGE_TEMPLATE_ROOT/Samples~"
PACKAGE_ROOT="$OUTPUT_DIR/com.wallstop-studios.novasharp"
SAMPLES_TARGET="$PACKAGE_ROOT/Samples~"

if [[ ! -d "$SAMPLES_SOURCE" ]]; then
    echo "Missing Unity package sample templates: $SAMPLES_SOURCE" >&2
    exit 1
fi

if paths_overlap "$PACKAGE_ROOT" "$PACKAGE_TEMPLATE_ROOT"; then
    echo "Unity package output must not overlap tracked package templates: $PACKAGE_ROOT" >&2
    exit 1
fi

if paths_overlap "$SAMPLES_TARGET" "$SAMPLES_SOURCE"; then
    echo "Unity package sample output must not overlap tracked sample templates: $SAMPLES_TARGET" >&2
    exit 1
fi

# Create output directories
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
  "licensesUrl": "https://github.com/wallstop/NovaSharp/blob/main/LICENSE",
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
    },
    {
      "displayName": "IL2CPP Spot Check",
      "description": "Minimal stopwatch scene for smoke-testing NovaSharp in IL2CPP player builds",
      "path": "Samples~/IL2CPPSpotCheck"
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

# Copy package samples from tracked templates.
rm -rf "$SAMPLES_TARGET"
cp -R "$SAMPLES_SOURCE" "$PACKAGE_ROOT/"

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
echo "  \"com.wallstop-studios.novasharp\": \"file:$(canonical_path "$PACKAGE_ROOT")\""
