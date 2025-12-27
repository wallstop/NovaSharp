# Spelling Audit

This helper wraps [`codespell`](https://github.com/codespell-project/codespell) so spelling regressions are caught just like the naming and namespace audits.

## Usage

Install the repository tooling requirements once per clone:

```bash
python -m pip install --upgrade pip
python -m pip install -r requirements.tooling.txt
```

Run the audit locally and refresh the committed log:

```bash
python tools/SpellingAudit/spelling_audit.py --write-log docs/audits/spelling_audit.log
```

To verify (used by CI), call:

```bash
python tools/SpellingAudit/spelling_audit.py --verify-log docs/audits/spelling_audit.log
```

The script supports additional arguments (e.g., extra skip globs or specific paths). Run with `--help` for the full set of options.

Domain-specific words can be added to `allowlist.txt` (one per line, `#` comments allowed) when they are correct spellings that would otherwise trigger false positives.
