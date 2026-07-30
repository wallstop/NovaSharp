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

The scanned set is derived from ``_config.yml``'s ``exclude`` and ``markdown_ext``,
so the guard covers exactly what Pages renders and the two cannot drift apart.
Note that ``markdown_ext`` defaults to five extensions, not just ``.md``.

Only the constructs Liquid treats as *fatal* are reported:

* ``{{`` that is not terminated by ``}}``  (``raise_missing_variable_terminator``)
* ``{%`` that is not terminated by ``%}``  (``raise_missing_tag_terminator``)
* a tag name Jekyll cannot resolve         (``Unknown tag``)
* a block-only tag outside its block — an orphan ``end*``, ``else``, ``elsif``, or
  ``when``, since Liquid registers those on the parent block and not on the
  document (``Unknown tag`` / ``Unexpected outer 'else' tag``)
* a closer that does not match the innermost open block
  (``'endfor' is not a valid delimiter for if``)
* a block that is never closed             (``'if' tag was never closed``)

Well-formed Liquid is left alone. ``{% raw %}`` regions are skipped as Liquid
skips them, and ``{% comment %}`` swallows tag errors but *not* variable errors,
because ``Liquid::Comment#unknown_tag`` is a no-op while its body is still
tokenised for variables.

Every rule above was checked against a real ``github-pages`` v232 build, one build
per case, guard verdict compared to Jekyll's exit code.
``scripts/ci/test_check_jekyll_liquid.py`` pins every case.

**Deliberately out of scope: resource resolution.** ``{% include missing.html %}``
and ``{% link missing.md %}`` are syntactically valid and abort the build anyway::

    Could not locate the included file 'nope.html' in any of [...]
    Could not find document 'nope.md' in tag 'link'.

Catching those means resolving ``_includes/`` and the site's document set, which is
a different job from tokenising Liquid. No published page uses any Jekyll tag
today, so this cannot fire until someone writes the first one. The gap is pinned
by ``UNCOVERED_CASES`` below so it stays visible rather than silent.

Also out of scope: an ``.html`` page carrying real YAML front matter is
Liquid-rendered but is not scanned — there are none here, and one written
deliberately would be using Liquid on purpose.

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
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable, Sequence

import yaml

REPO_ROOT = Path(__file__).resolve().parents[2]
JEKYLL_CONFIG = REPO_ROOT / "_config.yml"

# ``jekyll-optional-front-matter`` promotes every extension in Jekyll's
# ``markdown_ext`` to a page, and Jekyll's default is five of them — not just
# ``.md``. Covering fewer would leave a silent hole exactly where this guard is
# supposed to have none.
DEFAULT_MARKDOWN_EXT = "markdown,mkdown,mkdn,mkd,md"

# Liquid does not register block-only names on the document. `else`, `elsif`,
# `when`, and every `end*` name are accepted *only* inside their parent block, so
# a flat allowlist lets an orphan `{% endfor %}` or `{% else %}` through while
# Pages fails with "Unknown tag". These tables model the nesting instead, and
# every entry below was checked against a real github-pages v232 build.
#
# Block openers, mapped to the inner tags each one accepts.
BLOCK_TAGS: dict[str, frozenset[str]] = {
    "if": frozenset({"else", "elsif"}),
    "unless": frozenset({"else", "elsif"}),
    "case": frozenset({"when", "else"}),
    "for": frozenset({"else"}),
    "tablerow": frozenset(),
    "capture": frozenset(),
    "comment": frozenset(),
    "ifchanged": frozenset(),
    "highlight": frozenset(),
}

# Valid only inside a parent block that accepts them.
INNER_TAGS = frozenset({"else", "elsif", "when"})

# Registered globally, so legal anywhere — including at document level. `break`
# and `continue` belong here rather than under `for`: Liquid registers them
# globally and they do not fail outside a loop.
STANDALONE_TAGS = frozenset(
    {
        # Liquid core
        "assign",
        "break",
        "continue",
        "cycle",
        "decrement",
        "increment",
        "include",
        # Jekyll
        "include_relative",
        "link",
        "post_url",
        # github-pages plugins
        "gist",
        "seo",
    }
)

_ENDRAW_BODY = re.compile(r"\s*endraw(?!\w)")

_VARIABLE_HINT = (
    "Liquid parses this before Markdown, including inside fenced code blocks. "
    "Separate the braces (`{ {n=2} }`), or wrap the block in `{% raw %}` / "
    "`{% endraw %}`."
)

_TAG_HINT = (
    "Liquid parses this before Markdown, including inside fenced code blocks. "
    "Close or remove the tag, or wrap the block in `{% raw %}` / `{% endraw %}` "
    "if it is meant to be shown literally."
)


@dataclass(frozen=True)
class _OpenBlock:
    """A block tag awaiting its closer, kept so an unclosed one can be reported."""

    name: str
    line: int
    column: int
    snippet: str


@dataclass(frozen=True)
class Finding:
    """A fatal Liquid construct located in a rendered Markdown file."""

    path: str
    line: int
    column: int
    snippet: str
    message: str
    # Brace advice is nonsense for a block-nesting error, so the hint follows the
    # kind of defect rather than being one string for everything.
    hint: str = _VARIABLE_HINT

    def format(self) -> str:
        return (
            f"{self.path}:{self.line}:{self.column}: {self.message}\n"
            f"    {self.snippet}\n"
            f"    {self.hint}"
        )


def parse_args(argv: Sequence[str] | None = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--files",
        nargs="+",
        help="Specific files to check. Defaults to every rendered Markdown file.",
    )
    return parser.parse_args(argv)


def load_config(config_path: Path = JEKYLL_CONFIG) -> dict:
    """Return ``_config.yml`` as a dict, or empty when it cannot be read."""
    try:
        config = yaml.safe_load(config_path.read_text(encoding="utf-8"))
    except (OSError, yaml.YAMLError):
        return {}
    return config if isinstance(config, dict) else {}


def load_excludes(config_path: Path = JEKYLL_CONFIG) -> tuple[str, ...]:
    """Return the ``exclude`` entries from ``_config.yml``, slashes stripped.

    A missing or unreadable config means Jekyll publishes everything, which is
    the state that broke the site — so fall back to excluding nothing rather
    than silently narrowing the scan.
    """
    excludes = load_config(config_path).get("exclude") or []
    if not isinstance(excludes, list):
        return ()
    return tuple(str(entry).strip().strip("/") for entry in excludes if str(entry).strip())


def load_rendered_suffixes(config_path: Path = JEKYLL_CONFIG) -> frozenset[str]:
    """Return the file suffixes Jekyll renders, from ``markdown_ext``."""
    raw = load_config(config_path).get("markdown_ext") or DEFAULT_MARKDOWN_EXT
    return frozenset(
        f".{entry.strip().lstrip('.').lower()}"
        for entry in str(raw).split(",")
        if entry.strip().strip(".")
    )


def is_rendered(
    relative_path: Path,
    excludes: tuple[str, ...] = (),
    suffixes: frozenset[str] | None = None,
) -> bool:
    """Return True when Jekyll would render ``relative_path`` as a page.

    Jekyll's ``EntryFilter`` drops every entry whose name starts with ``.`` or
    ``_``, so those paths cannot break the build no matter what they contain. An
    ``exclude`` entry drops the file itself or any directory above it.

    Not covered: an ``.html`` page carrying real YAML front matter is also
    Liquid-rendered. There are none in this repository, and one written
    deliberately would be using Liquid on purpose.
    """
    if suffixes is None:
        suffixes = load_rendered_suffixes()
    if relative_path.suffix.lower() not in suffixes:
        return False
    if any(part.startswith((".", "_")) for part in relative_path.parts):
        return False

    posix = relative_path.as_posix()
    return not any(
        posix == entry or posix.startswith(f"{entry}/") for entry in excludes
    )


def discover_files(repo_root: Path) -> list[Path]:
    """Return every tracked Markdown file Jekyll would render, sorted."""
    config_path = repo_root / "_config.yml"
    suffixes = load_rendered_suffixes(config_path)
    patterns = [f"*{suffix}" for suffix in sorted(suffixes)]
    result = subprocess.run(
        ["git", "ls-files", "-z", *patterns],
        cwd=repo_root,
        capture_output=True,
        check=True,
        text=True,
    )
    excludes = load_excludes(config_path)
    tracked = (Path(entry) for entry in result.stdout.split("\0") if entry)
    return sorted(path for path in tracked if is_rendered(path, excludes, suffixes))


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
    open_blocks: list[_OpenBlock] = []
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
            index = _scan_tag(text, path, tag_start, findings, open_blocks)

    # Liquid raises when a block's tokens run out before its closer, so an
    # unclosed block is fatal even though nothing about it looks malformed.
    for block in reversed(open_blocks):
        findings.append(
            Finding(
                path=path,
                line=block.line,
                column=block.column,
                snippet=block.snippet,
                message=(
                    f"`{{% {block.name} %}}` is never closed by "
                    f"`{{% end{block.name} %}}`"
                ),
                hint=_TAG_HINT,
            )
        )

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


def _scan_tag(
    text: str,
    path: str,
    start: int,
    findings: list[Finding],
    open_blocks: list[_OpenBlock],
) -> int:
    """Validate the ``{% ... %}`` token at ``start``; return the next offset."""
    close = text.find("%}", start + 2)
    if close == -1:
        _add(
            findings,
            path,
            text,
            start,
            "unterminated Liquid tag `{%` (expected a closing `%}`)",
        )
        return start + 2

    name = _tag_name(text[start + 2 : close])

    # `Liquid::Comment#unknown_tag` is a no-op, so a name Liquid cannot resolve —
    # an unknown tag, an orphan `end*`, an orphan inner tag — is discarded when
    # the *innermost* open block is a comment. Everything else still applies:
    # registered block openers are parsed inside a comment body and still require
    # their closers, and a nested block makes its own `unknown_tag` raise again.
    unresolved_is_ignored = bool(open_blocks) and open_blocks[-1].name == "comment"

    if name == "raw":
        endraw = _find_endraw(text, close + 2)
        if endraw == -1:
            _add(
                findings,
                path,
                text,
                start,
                "`{% raw %}` is never closed by `{% endraw %}`",
            )
            return len(text)
        return endraw

    if name.startswith("end") and name[3:]:
        closing = name[3:]
        if not open_blocks:
            _add(findings, path, text, start, _unknown_tag_message(name))
        elif open_blocks[-1].name != closing:
            if not unresolved_is_ignored:
                _add(
                    findings,
                    path,
                    text,
                    start,
                    f"`{name}` is not a valid delimiter for "
                    f"`{{% {open_blocks[-1].name} %}}`",
                )
        else:
            open_blocks.pop()
        return close + 2

    if name in BLOCK_TAGS:
        line, column = _position(text, start)
        open_blocks.append(_OpenBlock(name, line, column, _snippet(text, start)))
        return close + 2

    if name in INNER_TAGS:
        if not open_blocks:
            _add(findings, path, text, start, _unknown_tag_message(name))
        elif (
            name not in BLOCK_TAGS[open_blocks[-1].name]
            and not unresolved_is_ignored
        ):
            _add(
                findings,
                path,
                text,
                start,
                f"`{{% {name} %}}` is not valid inside "
                f"`{{% {open_blocks[-1].name} %}}`",
            )
        return close + 2

    if name not in STANDALONE_TAGS and not unresolved_is_ignored:
        _add(findings, path, text, start, _unknown_tag_message(name))

    return close + 2


def _unknown_tag_message(name: str) -> str:
    if name == "else":
        return "unexpected outer `{% else %}`; Liquid rejects it outside a block"
    return (
        f"unknown Liquid tag `{name or '(empty)'}`; "
        "Jekyll aborts the build on tags it cannot resolve"
    )


def _add(
    findings: list[Finding], path: str, text: str, start: int, message: str
) -> None:
    line, column = _position(text, start)
    findings.append(
        Finding(
            path=path,
            line=line,
            column=column,
            snippet=_snippet(text, start),
            message=message,
            hint=_TAG_HINT,
        )
    )


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
        suffixes = load_rendered_suffixes()
        candidates = []
        for raw in args.files:
            path = Path(raw)
            relative = (
                path.resolve().relative_to(REPO_ROOT)
                if path.is_absolute()
                else Path(raw)
            )
            if is_rendered(relative, excludes, suffixes):
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
