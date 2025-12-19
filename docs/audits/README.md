# Audit Reports

This directory contains tracked audit log files that CI verifies on every run to ensure code quality standards are maintained.

## Contents

| File                      | Description                                              | Tool                                              |
| ------------------------- | -------------------------------------------------------- | ------------------------------------------------- |
| `documentation_audit.log` | Missing XML documentation comments on public API members | `tools/DocumentationAudit/documentation_audit.py` |
| `naming_audit.log`        | Naming convention violations (PascalCase, \_camelCase)   | `tools/NamingAudit/naming_audit.py`               |
| `spelling_audit.log`      | Spelling issues detected by codespell                    | `tools/SpellingAudit/spelling_audit.py`           |

## Refreshing Logs

The pre-commit hook automatically refreshes these logs. To manually refresh:

```bash
# Documentation audit
python tools/DocumentationAudit/documentation_audit.py --write-log docs/audits/documentation_audit.log

# Naming audit
python tools/NamingAudit/naming_audit.py --write-log docs/audits/naming_audit.log

# Spelling audit
python tools/SpellingAudit/spelling_audit.py --write-log docs/audits/spelling_audit.log
```

## CI Verification

CI verifies these logs match current audit results. If a log is stale, the workflow fails with instructions to refresh.
