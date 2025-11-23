# Naming Audit Helper

Provides a lightweight audit that scans C# source files under `src/` and reports
identifiers that do not follow the PascalCase expectations captured in
`.editorconfig` and the standard C# naming guidance. The audit now backs the CI
“Enforce naming conventions” step as well as the tracked `naming_audit.log`
artifact.

The script currently checks:

1. File names (stems without `.cs`) – they should be PascalCase unless the file
   is in the allowlist (generated artefacts, compatibility shims, etc.).
1. Type declarations (`class`, `struct`, `interface`, `enum`, `record`) – any
   non-PascalCase type is flagged along with the line number.
1. Methods, properties, and public/protected fields – any identifier that is not
   PascalCase (or `_camelCase` for private fields) is flagged unless explicitly
   allowlisted for Lua interop purposes.

## Usage

```bash
python3 tools/NamingAudit/naming_audit.py
```

The script exits with a non-zero status when issues are found, making it easy to
hook into CI once the outstanding violations are cleaned up.

### Generating or verifying `naming_audit.log`

To refresh the committed report after addressing naming issues:

```bash
python3 tools/NamingAudit/naming_audit.py --write-log naming_audit.log
```

CI verifies that `naming_audit.log` matches the current audit results via:

```bash
python3 tools/NamingAudit/naming_audit.py --verify-log naming_audit.log
```

Run the `--write-log` command whenever you adjust naming allowlists or rename
members so the log remains in sync with the enforced rules.
