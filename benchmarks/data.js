window.BENCHMARK_DATA = {
  "lastUpdate": 1782623121848,
  "repoUrl": "https://github.com/wallstop/NovaSharp",
  "entries": {
    "NovaSharp Benchmarks": [
      {
        "commit": {
          "author": {
            "email": "wallstop@wallstopstudios.com",
            "name": "Eli Pinkerton",
            "username": "wallstop"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "d9ce3497b5b700fef623256862982067bae93568",
          "message": "Initial work 3 (#19)\n\nCo-authored-by: Claude <noreply@anthropic.com>",
          "timestamp": "2025-12-11T14:08:31-08:00",
          "tree_id": "bf5c33ec8f2645faef1217923399d5a26018d7bf",
          "url": "https://github.com/wallstop/NovaSharp/commit/d9ce3497b5b700fef623256862982067bae93568"
        },
        "date": 1765491104659,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks.ExecuteScenario(ScenarioName: \"CoroutinePipeline\")",
            "value": 619.732,
            "unit": "ns",
            "extra": "P95: 0.000μs"
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks.ExecuteScenario(ScenarioName: \"NumericLoops\")",
            "value": 354.457,
            "unit": "ns",
            "extra": "P95: 0.000μs"
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks.ExecuteScenario(ScenarioName: \"TableMutation\")",
            "value": 7.2,
            "unit": "μs",
            "extra": "P95: 0.000μs"
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks.ExecuteScenario(ScenarioName: \"UserDataInterop\")",
            "value": 679.457,
            "unit": "ns",
            "extra": "P95: 0.000μs"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "wallstop@wallstopstudios.com",
            "name": "Eli Pinkerton",
            "username": "wallstop"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "23896f70c6172799aa577949b9d262f21f92cb0b",
          "message": "Next Batch of Work (#30)\n\n## Summary\n\n- Provide a short summary of the change.\n\n## Testing\n\n- Describe the tests you ran (commands, platforms).\n\n## Analyzer Coverage\n\n- [x] Ran `dotnet build src/NovaSharp.sln -c Release -nologo`\n- Additional analyzer/build/test commands (list each, remove this line\nif none beyond the solution build):\n- _example: `dotnet build\nsrc/debuggers/NovaSharp.RemoteDebugger/NovaSharp.RemoteDebugger.csproj\n-c Release -nologo`_\n\n## Checklist\n\n- [ ] Updated relevant docs (`docs/README.md`, feature-specific\nMarkdown) when adding or changing functionality.\n- [ ] Updated `scripts/README.md` and the subfolder README when\nadding/modifying helper scripts.\n- [ ] Verified CI-critical helpers (tests, coverage, branding) still\nwork locally or via CI.\n\n<!-- CURSOR_SUMMARY -->\n---\n\n> [!NOTE]\n> **Low Risk**\n> Changes are mostly devcontainer, documentation, and CI orchestration;\nruntime interpreter behavior is not the focus. CI workflow expansion\nincreases pipeline surface area but does not alter auth or production\ndeployment paths.\n> \n> **Overview**\n> This batch **replaces the stock .NET devcontainer** with a custom\n**Dockerfile** that pins the SDK from `global.json`, installs **Lua\n5.1–5.5**, **actionlint**, **yamllint**, and related CLI tooling, and\nsplits lifecycle into **`on-create.sh`** (restore before the C#\nextension) vs **`post-create.sh`** (Python `.venv`, hooks,\nverification). **`devcontainer.json`** adds NuGet/build cache mounts,\nexpanded VS Code settings, and **`update-content.sh`** /\n**`init-host.sh`** for pulls and host prep.\n> \n> **Contributor and agent guidance** moves into **`.cursorrules`**,\n**`.llm/context.md`**, skills, code samples, and a generated\n**`skills-index.json`**. **`.github/copilot-instructions.md`** and the\n**PR template** are tightened around scripted build/test and honest\nverification reporting.\n> \n> **CI and local gates** align workflows on **`global.json`**, add\n**concurrency/timeouts**, **NuGet caching**, and\n**`check-tooling-consistency`**. The **lua-comparison** job is a\n**matrix over OS × Lua 5.1–5.5**, builds reference Lua from source\n(including Windows MSVC), and runs **`run-lua-fixtures-fast.sh`** with\nricher comparison summaries; lint gains **Python harness self-tests**.\nAudit log paths move under **`docs/audits/`**. A new\n**`.githooks/pre-push`** mirrors key CI checks (optional build via\n`SKIP_BUILD_ON_PUSH`).\n> \n> Smaller tooling tweaks: **CSharpier 1.2.4**, **`.csharpierignore`**\nfor `PlatformAccessorBase.cs`, **`.dockerignore`**, and\n**gitattributes** for devcontainer scripts.\n> \n> <sup>Reviewed by [Cursor Bugbot](https://cursor.com/bugbot) for commit\n0a7d6987c2e6bb4b6b337b821423e841929683fd. Bugbot is set up for automated\ncode reviews on this repo. Configure\n[here](https://www.cursor.com/dashboard/bugbot).</sup>\n<!-- /CURSOR_SUMMARY -->",
          "timestamp": "2026-06-27T22:01:54-07:00",
          "tree_id": "bc446bba5d5f7788cc44c228c41d5b869f3456e0",
          "url": "https://github.com/wallstop/NovaSharp/commit/23896f70c6172799aa577949b9d262f21f92cb0b"
        },
        "date": 1782623121227,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks.ExecuteScenario(ScenarioName: \"CoroutinePipeline\")",
            "value": 586.223,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks.ExecuteScenario(ScenarioName: \"NumericLoops\")",
            "value": 416.694,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks.ExecuteScenario(ScenarioName: \"TableMutation\")",
            "value": 7.03,
            "unit": "μs",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks.ExecuteScenario(ScenarioName: \"UserDataInterop\")",
            "value": 733.144,
            "unit": "ns",
            "extra": ""
          }
        ]
      }
    ]
  }
}