# Compare guidance on NovaSharp tasks

Run this pilot manually when changing shared policy or consolidating skills. Compare
two committed guidance revisions within each frontend. Keep model-backed trials
outside CI.

## Prepare each pair

From the assessor checkout, record the baseline and candidate commit IDs. For
each task and each condition, run the following with a fresh destination outside
the repository. Pass the baseline revision as both revision arguments for the
baseline trial. Alternate which condition runs first across tasks.

```sh
bash tools/LlmSkillIndexer/prepare-pilot.sh TASK BASELINE_REV GUIDANCE_REV EMPTY_DEST
```

Each snapshot contains the same source within a task, guidance from its assigned
revision, and knowledge from the source revision. It includes a task-local plan
and goal with the same scope and delivery rules in both conditions. Other frontend settings come from the fixed source revision in both arms. Each trial starts with one local commit and no Git remote. Only mount the resulting
snapshot into the trial container. Keep the assessor checkout, later commits,
other trial outputs, and grading fixtures outside it. Start a fresh client session
with personal memory and remote repository search disabled. Allow the same local
tools, repository scripts, SDK cache and reference Lua binaries for both arms.
Record client/build, model, effort, tool settings, starting revision, and timeout
before starting. Use a 30-minute cap per trial.

## Prompts and assessor checks

| Task      | Give the agent this prompt                                                                                                                                               | Grade outside the agent session                                                                                                                                                                                                                                                                                                                                                                               |
| --------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| numeral   | Fix numeric literal scanning so accepted forms and malformed-number diagnostics match the target Lua version, including hex floats and malformed trailing text.          | Compare the submitted patch against stock Lua 5.1–5.5 on the version-eligible ParserTUnitTests Lua fixtures introduced at a906b22f. Grade acceptance, returned value, or quoted malformed numeral as applicable. Run the parser suite. Accept corrections to historical assertions when a stock Lua trace demonstrates the original expectation was wrong, including the Lua 5.1 hex-float success assertion. |
| bit32     | Reduce managed allocations on calls to the Lua 5.2 bit32 library. Show a before-and-after measurement and match reference Lua 5.2 semantics across the exported library. | Exercise all exported functions against Lua 5.2, including fractional operands, shifts and omitted widths. Require a lower median allocation on the fixed workload below. Check the registered callback path as exercised by the held-out RegisteredBit32CallbacksUseArgumentViews fixture at f31d973e.                                                                                                       |
| inventory | Give and run one read-only shell command to list the names of every repository skill, one per line. Return the command and its output.                                   | Compare sorted output to the directories containing canonical SKILL.md files. Grade read-only behavior separately. Inspect the command and tool trace for any write, including regeneration of identical bytes. Check for duplicates and compare before/after file hashes, including the index.                                                                                                               |

For the Bit32 allocation oracle, compile the Lua chunk below once per process.
Run it in-process in Release with the same SDK, runtime, and Lua profile. Warm
it with five invocations, then take eleven samples, each covering one invocation.
Measure the change in managed bytes on the calling thread around execution,
exclude compilation and logging, and divide by 30,000 callbacks. Compare median
bytes per callback between the historical source and the submitted patch.

```lua
local band, bxor, lshift = bit32.band, bit32.bxor, bit32.lshift
local value = 0
for i = 1, 10000 do
    value = bxor(band(i, 255), lshift(value, 1))
end
return value
```

For each trial record semantic result, critical policy omissions, unnecessary
skill activations, guidance files opened, elapsed time, and reported input tokens
and cost where exposed. Mark unavailable metrics individually. A provider or
environment error is an unscored trial. Inspect discovery separately with native
client diagnostics. In Claude Code the stream-json init event lists skills and
the local /context command reports project memory files and estimated token use.

Start with one pair per frontend and task. Repeat any apparent effect in two
fresh pairs. Report per frontend. Keep the change when all three load the shared
policy and expected skills, correctness holds, and apparent efficiency gains
repeat. Treat mixed outcomes as inconclusive. Keep raw receipts outside the
agent snapshot and summarize outcomes in the session report.
