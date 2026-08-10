# Documentation and Changelog Management Reference

## Documentation Update Workflow

1. **Implement feature/fix** with inline comments for non-obvious logic
1. **Add XML documentation** to all affected public members
1. **Create/update code samples** and verify they work
1. **Update `docs/*.md`** files if user guides are affected
1. **Add CHANGELOG entry** under `[Unreleased]`
1. **Run verification**: `dotnet build -c Release -warnaserror:CS1591` and `lychee --no-progress docs/**/*.md`

### Documentation File Locations

| Content Type  | Location                 |
| ------------- | ------------------------ |
| API reference | XML docs in source files |
| User guides   | `docs/`                  |
| Changelog     | `CHANGELOG.md`           |

______________________________________________________________________

## Common Documentation Mistakes

- **Copy-paste untested examples** — All samples must be verified to work
- **Document implementation, not behavior** — Users care WHAT it does, not HOW
- **Stale documentation** — Update when behavior changes

______________________________________________________________________

## Resources

- [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) — Changelog format specification
- [Semantic Versioning](https://semver.org/) — Version numbering standard
- [Microsoft XML Documentation](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/) — XML doc reference
- [docs/](../../../../docs/) — NovaSharp documentation folder
