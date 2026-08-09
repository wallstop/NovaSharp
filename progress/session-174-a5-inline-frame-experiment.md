# Session 174 — A5 inline call-frame experiment

Date: 2026-08-09

This session tested issue #108's bounded hypothesis that storing `CallStackItem` inline in the growable execution stack would outperform the thread-local class pool. The measured implementation did not demonstrate a repeatable speed improvement and failed the memory acceptance gate, so the runtime rewrite was reverted rather than merged as a nominal PLAN checkbox.

## Experiment

- Converted the existing complete call-frame payload to a mutable struct stored directly in `FastStack<CallStackItem>`.
- Added ref-based stack access and initialized frames in their final slots.
- Removed `CallStackItemPool` and its lifecycle/statistics surface.
- Preserved exact-once return of pooled local scopes, close lists, and close-index sets.
- Reacquired frame slots by absolute index around re-entrant Lua calls so execution-stack growth could not leave stale refs.
- Removed full-frame copies from normal return, CLR return, tail-call reuse, and guarded stack-pop paths before the final measurement.

The implementation and its pool deletions were intentionally reverted after measurement. The published baseline is `artifacts/benchmarkdotnet/a5-exit-fibonacci`, a clean non-incremental rerun is under `a5-struct-frame-valid-baseline-fib`, and experimental artifacts are under `a5-struct-frame-post-*`. An earlier `a5-struct-frame-base-fib` artifact was excluded after independent review showed that its stale incremental binary ran every engine 40-48x faster than the known `fib(30)` workload.

## Results

The comparison rows are ratios from the same BenchmarkDotNet run, which avoids treating the container's varying clock rate as a speedup or regression.

| Gate | Main observations | Inline-frame experiment | Result |
| --- | ---: | ---: | --- |
| `fib(30)` / NLua | published 20.70x; clean rerun 18.97x | 20.20x | within observed baseline variation; no demonstrated win |
| Lua 5.4 Default `new Script()` | 311.95 KiB | 321.45 KiB | +9,728 B per processor; reject |
| Lua 5.4 Complete `new Script()` | 327.59 KiB | 337.09 KiB | +9,728 B per processor; reject |

The construction delta is exactly 152 B for each of the 64 initially reserved call-frame slots. Inline storage therefore pays for the full cold debugger/error/close payload at processor construction even when no Lua call uses it. Removing redundant copies recovered only about 5% of the initial experimental Fibonacci time, but the final ratio remained inside observed baseline variation and does not justify the construction regression or the added ref-lifetime complexity.

## Retained guardrail

The experiment exposed a correctness hazard that remains useful for future frame-layout work: a live ref into the execution-stack array cannot survive a Lua callback because `__close` and `xpcall` message handlers can recursively grow and replace that array. `ReentrantCloseAndMessageHandlersSurviveExecutionStackGrowth` now drives both handlers through 96 non-tail calls, beyond the initial 64-frame capacity, and checks exact-once handler execution and results against Lua 5.4 and 5.5.

## Decision

The A5 struct-frame PLAN item and issue #108 remain open. The post-change Hanoi and coroutine create/resume/ping-pong measurements were not run after Fibonacci failed to demonstrate a repeatable improvement and script construction had already failed its memory gate; they remain required evidence for any renewed implementation. A renewed attempt must first produce a compact hot frame containing only ordinary call/return state, with debugger, error-handler, continuation, and to-be-closed ownership moved behind cold side storage. It must retain the new re-entrancy fixture and repeat same-run Fibonacci/Hanoi plus script-construction and coroutine allocation gates before adoption.

## Verification

- Direct reference Lua 5.4 and 5.5 runs satisfied the retained fixture's internal assertions for `done`, `1`, `false`, `handled:boom:96`, and `1`.
- The targeted TUnit regression passed for both applicable compatibility versions.
- `./scripts/build/quick.sh` completed successfully and the full TUnit suite passed 15,225/15,225 tests.
- The corpus extractor dry run reports 1,970 existing fixtures and zero new fixtures after reconciling three previously omitted comparable snippets and one stale manifest-only entry found during review.
- Full-corpus Lua 5.1-5.5 enforcement reported zero mismatches, zero one-sided or missing outputs, and no new or changed both-error ratchet entries.
- CSharpier, Markdown formatting, and the repository pre-commit checks completed successfully.
- Independent reviews found and corrected a stale incremental benchmark artifact and generated-corpus manifest drift in the first draft of this report. PR #109 CI passed CSharpier plus the complete Tests workflow, including coverage, all three OS test jobs, all 15 Lua comparison lanes, and the aggregate report; the benchmark workflow was correctly path-filtered because no runtime or benchmark implementation remains in the diff.
