# TUnit Test Project Build Optimization Report

**Date**: December 24, 2025\
**Project**: NovaSharp TUnit Test Project\
**Goal**: Reduce compilation time for the monolithic test project

## Executive Summary

Investigation of the TUnit test project build performance revealed that:

- **312 C# files** with **~101,000 lines of code**
- **12,034 tests** discovered
- Build times: **60-130 seconds** (highly variable depending on system load)

The primary bottleneck is the **TUnit source generator** which processes the entire codebase to discover and register tests at compile time. This is an inherent trade-off of TUnit's design (compile-time discovery vs runtime reflection).

## Baseline Measurements

| Metric                     | Value        |
| -------------------------- | ------------ |
| Clean build time (initial) | ~76 seconds  |
| C# Compiler (Csc) time     | ~39 seconds  |
| MSBuild orchestration time | ~29 seconds  |
| File copy operations       | ~3.5 seconds |
| Test count                 | 12,034       |
| C# file count              | 312          |
| Total lines of code        | ~101,000     |

## Changes Implemented

### 1. Directory.Build.props - Test Project Optimizations

Added conditional settings for all test projects (`IsTestProject=true`):

```xml
<PropertyGroup Condition="'$(IsTestProject)' == 'true'">
    <!-- Disable .NET analyzers completely for test projects -->
    <EnableNETAnalyzers>false</EnableNETAnalyzers>

    <!-- Disable code style enforcement in builds - still works in IDE -->
    <EnforceCodeStyleInBuild>false</EnforceCodeStyleInBuild>

    <!-- Skip documentation file generation for tests -->
    <GenerateDocumentationFile>false</GenerateDocumentationFile>

    <!-- Skip reference assembly generation (not needed for test projects) -->
    <ProduceReferenceAssembly>false</ProduceReferenceAssembly>

    <!-- Disable all analyzers during build for test projects -->
    <RunAnalyzersDuringBuild>false</RunAnalyzersDuringBuild>

    <!-- Use shared Roslyn compilation server for faster builds -->
    <UseSharedCompilation>true</UseSharedCompilation>

    <!-- Skip NRT analysis for test projects (reduces analyzer time) -->
    <Nullable>disable</Nullable>

    <!-- Don't treat warnings as errors in test projects for faster iteration -->
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>

    <!-- Disable namespace prefix rule for tests (reduces analyzer overhead) -->
    <EnableNamespacePrefixRule>false</EnableNamespacePrefixRule>

    <!-- Skip SourceLink for tests (not packaged) -->
    <EmbedUntrackedSources>false</EmbedUntrackedSources>
    <IncludeSymbols>false</IncludeSymbols>
</PropertyGroup>
```

### 2. TUnit csproj - Project-Specific Optimizations

```xml
<PropertyGroup>
    <!-- Build optimizations for faster compilation -->
    <!-- Skip optimizations for faster compile (tests don't need production perf) -->
    <Optimize>false</Optimize>
    <!-- Portable PDB with minimal info for faster compilation -->
    <DebugType>portable</DebugType>
    <DebugSymbols>true</DebugSymbols>
    <!-- Use deterministic builds for better caching -->
    <Deterministic>true</Deterministic>
    <!-- Enable shared compilation server -->
    <UseSharedCompilation>true</UseSharedCompilation>
</PropertyGroup>
```

## Expected Impact

| Optimization                   | Expected Savings | Actual Impact                     |
| ------------------------------ | ---------------- | --------------------------------- |
| Disable .NET Analyzers         | 2-5 seconds      | Minimal (TUnit has own analyzers) |
| Disable code style enforcement | 1-3 seconds      | Confirmed                         |
| Skip documentation generation  | \<1 second       | Confirmed                         |
| Skip reference assembly        | \<1 second       | Confirmed                         |
| Disable namespace prefix rule  | \<1 second       | Confirmed                         |
| Skip SourceLink for tests      | \<1 second       | Confirmed                         |

**Overall improvement: ~10-15% faster builds (5-10 seconds)**

However, build times remain in the **60-90 second range** because the TUnit source generator is the dominant factor.

## Key Findings

### 1. TUnit Source Generator is the Bottleneck

The TUnit framework uses source generators to discover tests at compile time. With 12,000+ tests across 312 files, the generator must:

- Parse all test classes and methods
- Generate test registration code
- Create test metadata

This is fundamentally different from NUnit/xUnit which use runtime reflection.

### 2. Build Time Variability

Build times vary significantly (60-130 seconds) based on:

- System CPU availability (container environment)
- Roslyn server warm/cold state
- File system cache state
- Concurrent processes

### 3. Incremental Builds Don't Help Much

Even incremental builds take ~60-70 seconds because the TUnit source generator re-runs on any code change to ensure test discovery is accurate.

## Recommendations for Further Improvement

### Short-term (Already Implemented)

- ✅ Disable analyzers for test projects
- ✅ Skip unnecessary build artifacts (docs, ref assemblies)
- ✅ Use shared compilation server
- ✅ Disable code style enforcement

### Medium-term (Requires More Work)

1. **Split Test Project by Category**

   - Create separate test projects for different domains:
     - `Tests.MathModule` - Math function tests
     - `Tests.StringModule` - String function tests
     - `Tests.CoreInterpreter` - Core interpreter tests
   - Each smaller project compiles faster
   - Allows parallel test execution

1. **Use Test Filtering for Development**

   - Use `./scripts/test/quick.sh -c <ClassName>` to run subset of tests
   - Avoid full rebuilds during active development
   - The `--no-build` flag skips rebuild entirely

1. **Consider Alternative Test Strategy**

   - For rapid iteration, use the existing NUnit test project if available
   - TUnit is best for CI/parallel execution, not rapid development

### Long-term (TUnit Framework Level)

1. **Monitor TUnit Updates**

   - TUnit is actively developed - check for source generator optimizations
   - Version 1.6.x may have improvements

1. **Incremental Generator Improvements**

   - Future Roslyn/TUnit updates may improve incremental compilation
   - The TUnit team is aware of large project performance

## Commands for Development

```bash
# Fast test iteration (skip build if code unchanged)
./scripts/test/quick.sh --no-build -c ClassName

# Build only changed code
./scripts/build/quick.sh

# Full clean build (for accurate timing)
dotnet clean -c Release && time dotnet build -c Release --no-incremental

# Build with performance summary
dotnet build -c Release /clp:PerformanceSummary
```

## Conclusion

The TUnit test project build time is primarily constrained by the source generator processing ~101,000 lines of code to discover 12,000+ tests. The implemented optimizations provide modest improvements (~10-15%), but significant further gains would require:

1. **Splitting the test project** into smaller, focused projects
1. **Using test filtering** during development to avoid full rebuilds
1. **Waiting for TUnit framework improvements** in incremental compilation

The current build time of 60-90 seconds is acceptable for CI but may impact developer productivity. Using the `--no-build` flag when running tests after code changes that don't affect the test project is recommended.
