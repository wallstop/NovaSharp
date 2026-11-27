# GitHub Interaction

Route every GitHub-hosted read or write through the first capable interface,
and spend as few API calls as the task allows. This workflow adapts the
MCP-first routing and credential discipline used by the sibling IshoBoy and
DoxReloaded repositories.

## Route order

1. **GitHub connector or MCP** for remote state: issues, pull requests,
   reviews, checks, workflow runs, releases, and repository metadata.
1. **Local Git** for facts already in the checkout: history, branches, diffs,
   and object transport (`fetch`, `pull`, `push`).
1. **`gh`** only when the connector and MCP expose no operation for the task.

Do not fall back because a CLI command is shorter or because a previous,
different call failed. A quota error is a clock, not a missing capability:
REST and GraphQL have separate pools, so switching interfaces changes the pool
rather than fixing an outage.

## Reuse reads

- Inventory issues, pull requests, or runs once per session. Request
  `per_page=100` and follow pages until a page returns fewer than 100 entries;
  a result of exactly 100 is not proof of completion.
- Save the raw JSON under `/tmp/opencode/<session>-audit/` and filter the saved
  file. Do not repeat a list call to answer a second question.
- Read the named object directly when the task names one issue, pull request,
  commit, or run. Do not reconstruct it from repeated list calls.
- Batch independent reads into one command or one API request where the
  interface permits it. Keep each result attributable.

## Poll CI once

- Push only after local gates pass, and batch commits so one push triggers one
  CI run. Do not push to probe formatting.
- Observe every run for the pushed head commit before reporting CI status. Start
  one background watch per run, such as `gh run watch <run-id> --exit-status`, or
  read the pull request's check rollup until no required check is pending.
- A pending rollup may be read again while required checks are still running.
  Stop once one complete rollup reports success or a named failure; a background
  watch is preferred over a foreground sleep loop.
- Do not loop over `gh pr checks`, `gh run list`, or the API on a timer. Each
  poll is a request; a foreground sleep loop also occupies the session.
- Re-read checks after a new push or to inspect a named failed job. Read the
  failing job log before naming a cause.

## Mutations

- Start from an exact object number and expected state. Read it first when the
  action is destructive or state-dependent.
- After a timeout, dropped connection, or 5xx, read the object before retrying.
  The first write may have succeeded; a blind retry can duplicate a comment,
  review, or pull request.
- Prefer one consolidated pull request per session. Do not open stacked pull
  requests for one slice.
- Do not trigger reviewers or rerun workflows unless the task requires it.
- Never rewrite shared history, force-push, or change repository or organization
  settings without an explicit request.

## Credentials

- Use the connector, MCP, or existing `gh` authentication. Never print, echo,
  search, commit, or pass a token on a command line.
- A failed call does not justify exporting a token into the shell or writing one
  to a new file. Report the blocked operation instead.
- The devcontainer helper scripts under `scripts/dev/github-auth-*.sh` are the
  only repository-owned credential entry points.

## Evidence to keep

Record the one inventory command and count, the pull request number and head
SHA, and the final check rollup. Cite those results in the progress brief and
pull request instead of re-querying them for each summary.
