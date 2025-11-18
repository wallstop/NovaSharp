# Naming Audit Helper

Provides a lightweight audit that scans C# source files under `src/` and reports
identifiers that do not follow the PascalCase expectations captured in
`.editorconfig` and the standard C# naming guidance.

The script currently checks:

1. File names (stems without `.cs`) – they should be PascalCase unless the file
   is in the allowlist (generated artefacts, compatibility shims, etc.).
1. Type declarations (`class`, `struct`, `interface`, `enum`, `record`) – any
   non-PascalCase type is flagged along with the line number.

As the Milestone F naming sweep progresses, we can expand the audit with method
and variable checks or wire it into CI similarly to the `NamespaceAudit`.

## Usage

```bash
python3 tools/NamingAudit/naming_audit.py
```

The script exits with a non-zero status when issues are found, making it easy to
hook into CI once the outstanding violations are cleaned up.
