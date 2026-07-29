#!/usr/bin/env python3
"""Fail when Markdown published by GitHub Pages contains fatal Liquid syntax.

GitHub Pages serves this repository from the ``main`` branch with the
``github-pages`` gem, whose plugin set includes ``jekyll-optional-front-matter``.
That plugin turns *every* published Markdown file into a Jekyll page, so each one
is parsed by Liquid before Markdown conversion. A Lua nested table constructor
written without inner spaces therefore reads as an unterminated Liquid variable
and aborts the whole site build:

    Liquid Exception: Liquid syntax error (line 965): Variable '{{n=2}' was not
    properly terminated with regexp: /\\}\\}/ in docs/lua-spec/lua-5.1-spec.md

A single such sequence takes the published site down, and the Pages workflow is
the only signal — no other CI leg reads Markdown as Liquid, and it only reports
after a push to ``main``. This check reproduces Liquid's own tokenizer failure
modes so the problem surfaces on the pull request instead.

The scanned set is derived from ``_config.yml``'s ``exclude`` list, so the guard
covers exactly what Pages renders and the two cannot drift apart.

Only the constructs Liquid treats as *fatal* are reported:

* ``{{`` that is not terminated by ``}}``      (``raise_missing_variable_terminator``)
* ``{%`` that is not terminated by ``%}``      (``raise_missing_tag_terminator``)
* ``{% name %}`` where ``name`` is not a tag available on GitHub Pages
  (``Liquid::SyntaxError: Unknown tag``)

Well-formed Liquid is left alone, and ``{% raw %}`` regions are skipped exactly
as Liquid skips them.

Usage:
    python3 scripts/ci/check_jekyll_liquid.py [--files FILE ...]

Exit codes:
    0  No fatal Liquid syntax found
    1  At least one file would abort the Jekyll build
"""

from __future__ import annotations

import argparse
import re
import subprocess
import sys

import yaml
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable, Sequence

REPO_ROOT = Path(__file__).resolve().parents[2]
JEKYLL_CONFIG = REPO_ROOT / "_config.yml"

# ``jekyll-optional-front-matter`` promotes these extensions to pages.
RENDERED_SUFFIXES = {".md", ".markdown"}

# Liquid + Jekyll + github-pages tags. A tag outside this set raises
# "Unknown tag" and fails the build, so an unrecognised name is an error rather
# than something to silently accept.
KNOWN_TAGS = frozenset(
    {
        # Liquid core
        "assign",
        "capture",
        "endcapture",
        "case",
        "when",
        "endcase",
        "comment",
        "endcomment",
        "cycle",
        "decrement",
        "for",
        "else",
        "elsif",
        "endfor",
        "break",
        "continue",
        "if",
        "endif",
        "ifchanged",
        "endifchanged",
        "increment",
        "raw",
        "endraw",
        "tablerow",
        "endtablerow",
        "unless",
        "endunless",
        # Jekyll
        "highlight",
        "endhighlight",
        "include",
        "include_relative",
        "link",
        "post_url",
        # github-pages plugins
        "gist",
        "seo",
    }
)

_ENDRAW_BODY = re.compile(r"\s*endraw(?!\w)")

_HINT = (
    "Liquid parses this before Markdown, including inside fenced code blocks. "
    "Separate the braces (`{ {n=2} }`), or wrap the block in `{% raw %}` / "
    "`{% endraw %}`."
)


@dataclass(frozen=True)
class Finding:
    """A fatal Liquid construct located in a rendered Markdown file."""

    path: str
    line: int
    column: int
    snippet: str
    message: str

    def format(self) -> str:
        return (
            f"{self.path}:{self.line}:{self.column}: {self.message}\n"
            f"    {self.snippet}\n"
            f"    {_HINT}"
        )


def parse_args(argv: Sequence[str] | None = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--files",
        nargs="+",
        help="Specific files to check. Defaults to every rendered Markdown file.",
    )
    return parser.parse_args(argv)


def load_excludes(config_path: Path = JEKYLL_CONFIG) -> tuple[str, ...]:
    """Return the ``exclude`` entries from ``_config.yml``, slashes stripped.

    A missing or unreadable config means Jekyll publishes everything, which is
    the state that broke the site — so fall back to excluding nothing rather
    than silently narrowing the scan.
    """
    try:
        config = yaml.safe_load(config_path.read_text(encoding="utf-8")) or {}
    except (OSError, yaml.YAMLError):
        return ()

    excludes = config.get("exclude") or []
    if not isinstance(excludes, list):
        return ()
    return tuple(str(entry).strip().strip("/") for entry in excludes if str(entry).strip())


def is_rendered(relative_path: Path, excludes: tuple[str, ...] = ()) -> bool:
    """Return True when Jekyll would render ``relative_path`` as a page.

    Jekyll's ``EntryFilter`` drops every entry whose name starts with ``.`` or
    ``_``, so those paths cannot break the build no matter what they contain. An
    ``exclude`` entry drops the file itself or any directory above it.
    """
    if relative_path.suffix.lower() not in RENDERED_SUFFIXES:
        return False
    if any(part.startswith((".", "_")) for part in relative_path.parts):
        return False

    posix = relative_path.as_posix()
    return not any(
        posix == entry or posix.startswith(f"{entry}/") for entry in excludes
    )


def discover_files(repo_root: Path) -> list[Path]:
    """Return every tracked Markdown file Jekyll would render, sorted."""
    result = subprocess.run(
        ["git", "ls-files", "-z", "*.md", "*.markdown"],
        cwd=repo_root,
        capture_output=True,
        check=True,
        text=True,
    )
    excludes = load_excludes(repo_root / "_config.yml")
    tracked = (Path(entry) for entry in result.stdout.split("\0") if entry)
    return sorted(path for path in tracked if is_rendered(path, excludes))


def _position(text: str, offset: int) -> tuple[int, int]:
    """Return the 1-based (line, column) of ``offset`` within ``text``."""
    line = text.count("\n", 0, offset) + 1
    line_start = text.rfind("\n", 0, offset) + 1
    return line, offset - line_start + 1


def _snippet(text: str, offset: int, limit: int = 80) -> str:
    end = text.find("\n", offset)
    if end == -1:
        end = len(text)
    return text[offset : min(end, offset + limit)].strip()


def scan_text(text: str, path: str) -> list[Finding]:
    """Return the fatal Liquid constructs in ``text``.

    Mirrors ``Liquid::BlockBody`` tokenisation: a ``{{`` token ends at the first
    ``}`` and must be followed by a second ``}``; a ``{%`` token must reach
    ``%}``; and ``{% raw %}`` suppresses parsing until ``{% endraw %}``.
    """
    findings: list[Finding] = []
    index = 0
    length = len(text)

    while index < length:
        variable_start = text.find("{{", index)
        tag_start = text.find("{%", index)
        if variable_start == -1 and tag_start == -1:
            break

        if tag_start == -1 or (variable_start != -1 and variable_start < tag_start):
            index = _scan_variable(text, path, variable_start, findings)
        else:
            index = _scan_tag(text, path, tag_start, findings)

    return findings


def _scan_variable(text: str, path: str, start: int, findings: list[Finding]) -> int:
    """Validate the ``{{ ... }}`` token at ``start``; return the next offset."""
    close = text.find("}", start + 2)
    if close != -1 and text[close : close + 2] == "}}":
        return close + 2

    line, column = _position(text, start)
    findings.append(
        Finding(
            path=path,
            line=line,
            column=column,
            snippet=_snippet(text, start),
            message="unterminated Liquid variable `{{` (expected a closing `}}`)",
        )
    )
    # Resume after the opening delimiter so a second defect on the same line is
    # still reported rather than swallowed by the first.
    return start + 2


def _tag_name(raw_body: str) -> str:
    """Return the tag name from a ``{% ... %}`` body, honouring `{%-` / `-%}`."""
    body = raw_body.strip()
    if body.startswith("-"):
        body = body[1:]
    if body.endswith("-"):
        body = body[:-1]
    body = body.strip()
    return body.split(None, 1)[0] if body else ""


def _scan_tag(text: str, path: str, start: int, findings: list[Finding]) -> int:
    """Validate the ``{% ... %}`` token at ``start``; return the next offset."""
    close = text.find("%}", start + 2)
    if close == -1:
        line, column = _position(text, start)
        findings.append(
            Finding(
                path=path,
                line=line,
                column=column,
                snippet=_snippet(text, start),
                message="unterminated Liquid tag `{%` (expected a closing `%}`)",
            )
        )
        return start + 2

    name = _tag_name(text[start + 2 : close])
    if name not in KNOWN_TAGS:
        line, column = _position(text, start)
        findings.append(
            Finding(
                path=path,
                line=line,
                column=column,
                snippet=_snippet(text, start),
                message=(
                    f"unknown Liquid tag `{name or '(empty)'}`; "
                    "Jekyll aborts the build on tags it cannot resolve"
                ),
            )
        )
        return close + 2

    if name == "raw":
        endraw = _find_endraw(text, close + 2)
        if endraw == -1:
            line, column = _position(text, start)
            findings.append(
                Finding(
                    path=path,
                    line=line,
                    column=column,
                    snippet=_snippet(text, start),
                    message="`{% raw %}` is never closed by `{% endraw %}`",
                )
            )
            return len(text)
        return endraw

    return close + 2


def _find_endraw(text: str, start: int) -> int:
    """Return the offset just past the next ``endraw`` tag, or -1.

    ``Liquid::Raw#parse`` closes the region with its own regex,
    ``/\\A(.*)\\{\\%\\s*(\\w+)\\s*(.*)?\\%\\}\\z/om``, which — unlike the tag
    parsing in ``Liquid::BlockBody`` — has no whitespace-control branch. So
    ``{%- endraw -%}`` does *not* close a raw region and Liquid raises
    "'raw' tag was never closed". Only the undecorated form counts here.
    """
    index = start
    while True:
        candidate = text.find("{%", index)
        if candidate == -1:
            return -1
        close = text.find("%}", candidate + 2)
        if close == -1:
            return -1
        if _ENDRAW_BODY.match(text[candidate + 2 : close]):
            return close + 2
        index = close + 2


def check_files(paths: Iterable[Path], repo_root: Path) -> list[Finding]:
    findings: list[Finding] = []
    for relative in paths:
        absolute = repo_root / relative
        try:
            text = absolute.read_text(encoding="utf-8")
        except (OSError, UnicodeDecodeError) as error:  # pragma: no cover - I/O guard
            print(f"{relative}: could not read file: {error}", file=sys.stderr)
            continue
        findings.extend(scan_text(text, relative.as_posix()))
    return findings


def main(argv: Sequence[str] | None = None) -> int:
    args = parse_args(argv)

    if args.files:
        excludes = load_excludes()
        candidates = []
        for raw in args.files:
            path = Path(raw)
            relative = (
                path.resolve().relative_to(REPO_ROOT)
                if path.is_absolute()
                else Path(raw)
            )
            if is_rendered(relative, excludes):
                candidates.append(relative)
    else:
        candidates = discover_files(REPO_ROOT)

    findings = check_files(candidates, REPO_ROOT)

    if findings:
        print(
            f"Fatal Liquid syntax in {len(findings)} location(s); "
            "the GitHub Pages build would fail:",
            file=sys.stderr,
        )
        for finding in findings:
            print(finding.format(), file=sys.stderr)
        return 1

    print(f"Checked {len(candidates)} rendered Markdown file(s); no fatal Liquid syntax.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
