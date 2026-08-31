# Documentation and Release-Note Management Reference

## Documentation Update Workflow

1. **Implement feature/fix** with inline comments for non-obvious logic
1. **Add XML documentation** to all affected public members
1. **Create/update code samples** and verify they work
1. **Update `docs/*.md`** files if user guides are affected
1. **Prepare a release-note line** for the next GitHub Release when the change is
   user-visible. Edit the release description only when release work is in scope;
   otherwise include the line in the PR description or handoff.
1. **Run verification**: build the affected projects and run
   `python3 scripts/ci/check_markdown_links.py --files path/to/file.md` for every
   edited Markdown file.

### Documentation File Locations

| Content Type       | Location                                                          |
| ------------------ | ----------------------------------------------------------------- |
| API reference      | XML docs in source files                                          |
| User guides        | `docs/`                                                           |
| Release-note truth | [GitHub Releases](https://github.com/wallstop/NovaSharp/releases) |

There is no root `CHANGELOG.md`. Unity package builds create an artifact-local
`CHANGELOG.md`; it is packaging output, not the source of release history.

______________________________________________________________________

## Common Documentation Mistakes

- **Copy-paste untested examples** — All samples must be verified to work
- **Document implementation, not behavior** — Users care WHAT it does, not HOW
- **Stale documentation** — Update when behavior changes
- **Invented changelog authority** — Do not add a root changelog when GitHub
  Releases are the configured authority
- **Host API deprecation windows** — Remove superseded APIs and update
  repository-owned consumers atomically; do not document or preserve a staged
  compatibility layer

______________________________________________________________________

## Resources

- [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) — Changelog format specification
- [Semantic Versioning](https://semver.org/) — Version numbering standard
- [Microsoft XML Documentation](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/) — XML doc reference
- [docs/](../../../../docs/) — NovaSharp documentation folder
