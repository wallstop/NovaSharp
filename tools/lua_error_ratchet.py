#!/usr/bin/env python3
"""Ratchet unclassified Lua comparison both-error cases."""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Any

DEFAULT_BASELINE_PATH = Path("docs/testing/lua-error-ratchet.json")
MODULE_NOT_FOUND_PATTERN = re.compile(r"module '([^']+)' not found")
NO_FILE_SEARCH_PATH_PATTERN = re.compile(r"^([ \t]*no file )['\"]([^'\"]+)['\"]$")
REPO_ROOT_PATTERN = re.compile(
    r"(^|[\s:'\"])(?:[A-Za-z]:)?/"
    r"(?:workspaces/NovaSharp|home/runner/work/NovaSharp/NovaSharp|"
    r"Users/runner/work/NovaSharp/NovaSharp|a/NovaSharp/NovaSharp|github/workspace)"
    r"(?=/)",
    flags=re.MULTILINE,
)


def normalize_fixture_id(file: str | Path) -> str:
    """Normalize fixture IDs so reports compare consistently across OSes."""
    result = str(file).replace("\\", "/")
    while result.startswith("./"):
        result = result[2:]
    return result


def normalize_require_candidate_suffix(candidate: str, module_path: str) -> str:
    """Return a host-root-independent require candidate suffix."""
    basename = candidate.rsplit("/", 1)[-1]
    segments = [segment for segment in candidate.split("/") if segment]
    module_segments = [segment for segment in module_path.split("/") if segment]

    module_file = f"{module_path}.lua"
    module_init_file = f"{module_path}/init.lua"
    if candidate == module_file or candidate.endswith(f"/{module_file}"):
        return module_file
    if candidate == module_init_file or candidate.endswith(f"/{module_init_file}"):
        return module_init_file

    module_extension_prefix = f"{module_path}."
    if candidate == module_extension_prefix[:-1] or candidate.endswith(
        f"/{module_extension_prefix[:-1]}"
    ):
        return module_path
    if candidate.startswith(module_extension_prefix):
        return candidate
    module_extension_marker = f"/{module_extension_prefix}"
    module_extension_index = candidate.rfind(module_extension_marker)
    if module_extension_index >= 0:
        return candidate[module_extension_index + 1 :]

    module_directory_marker = f"/{module_path}/"
    module_directory_index = candidate.rfind(module_directory_marker)
    if module_directory_index >= 0:
        return candidate[module_directory_index + 1 :]
    if candidate.startswith(f"{module_path}/"):
        return candidate
    if candidate == module_path or candidate.endswith(f"/{module_path}"):
        return module_path

    if module_segments:
        first_module_segment = module_segments[0]
        for index, segment in enumerate(segments):
            if first_module_segment not in segment:
                continue
            remaining_segments = segments[index + 1 :]
            remaining_module_index = 1
            for remaining_segment in remaining_segments:
                if remaining_module_index >= len(module_segments):
                    break
                if module_segments[remaining_module_index] in remaining_segment:
                    remaining_module_index += 1
            if remaining_module_index >= len(module_segments):
                return "/".join(segments[index:])

    if basename.startswith("loadall."):
        return basename

    return f"<unmatched>/{basename}"


def normalize_search_path_lines(text: str) -> str:
    """Normalize Lua require search roots while preserving unique candidate shape/order."""
    module_match = MODULE_NOT_FOUND_PATTERN.search(text)
    module_path = module_match.group(1).replace(".", "/") if module_match else ""
    seen_search_candidates: set[str] = set()
    lines: list[str] = []

    for line in text.split("\n"):
        search_path_match = NO_FILE_SEARCH_PATH_PATTERN.match(line)
        if not search_path_match:
            lines.append(line)
            continue

        candidate = search_path_match.group(2).replace("\\", "/")
        suffix = normalize_require_candidate_suffix(candidate, module_path)

        normalized = f"{search_path_match.group(1)}'<search>/{suffix}'"
        # Lua builds vary in how many install roots they print for the same
        # candidate suffix; root multiplicity is host noise, not error cause.
        if normalized.strip() in seen_search_candidates:
            continue
        seen_search_candidates.add(normalized.strip())
        lines.append(normalized)

    return "\n".join(lines)


def normalize_error_text(text: str) -> str:
    """Normalize volatile error text before hashing."""
    result = text.replace("\r\n", "\n").replace("\r", "\n")
    result = result.replace("\\", "/")
    result = re.sub(r"0x[0-9a-fA-F]+", "<addr>", result)
    result = re.sub(r"(?<=[:\s])[0-9A-F]{8,16}(?=[:\s\n]|$)", "<addr>", result)
    result = re.sub(r"([A-Za-z]:)?[^:\n]*?LuaFixtures/", "LuaFixtures/", result)
    result = REPO_ROOT_PATTERN.sub(r"\1<repo>", result)
    result = normalize_search_path_lines(result)
    result = re.sub(r"(\.lua):\d+:", r"\1:<line>:", result)
    result = re.sub(r"\[string \"[^\"]*\"\]:\d+:", "[string \"<chunk>\"]:<line>:", result)
    result = re.sub(
        r"^(?:[A-Za-z]:)?(?:[^:\n]*/)?lua(?:\d+(?:\.\d+)?)?(?:\.exe)?:\s*",
        "lua: ",
        result,
        flags=re.MULTILINE,
    )
    result = re.sub(
        r"^Unhandled exception\. WallstopStudios\.NovaSharp\.Interpreter\.Errors\.",
        "",
        result,
        flags=re.MULTILINE,
    )
    result = re.sub(r"^\s+at .*$", "  <stack-frame>", result, flags=re.MULTILINE)
    result = re.sub(r"(<stack-frame>\n)+", "<stack-trace>\n", result)
    result = "\n".join(line.rstrip() for line in result.split("\n"))
    result = re.sub(r"\n{3,}", "\n\n", result)
    return result.strip()


def hash_error_text(text: str) -> str:
    normalized = normalize_error_text(text)
    return hashlib.sha256(normalized.encode("utf-8")).hexdigest()


def make_excerpt(text: str, limit: int = 180) -> str:
    normalized = normalize_error_text(text)
    single_line = " | ".join(line for line in normalized.split("\n") if line)
    if len(single_line) <= limit:
        return single_line
    return f"{single_line[: limit - 3]}..."


def categorize_return_code(return_code: int | str) -> str:
    try:
        numeric = int(return_code)
    except (TypeError, ValueError):
        value = str(return_code).strip().lower()
        if value in {"zero", "nonzero", "timeout", "missing"}:
            return value
        return "nonzero"

    if numeric == 0:
        return "zero"
    if numeric == -1:
        return "timeout"
    return "nonzero"


@dataclass(frozen=True)
class BothErrorEntry:
    file: str
    lua_version: str
    lua_rc_category: str
    nova_rc_category: str
    lua_error_sha256: str
    nova_error_sha256: str
    lua_error_excerpt: str = ""
    nova_error_excerpt: str = ""
    classification: str = "unclassified"

    @property
    def key(self) -> tuple[str, str]:
        return (self.lua_version, normalize_fixture_id(self.file))

    @classmethod
    def from_errors(
        cls,
        file: str,
        lua_version: str,
        lua_rc: int,
        nova_rc: int,
        lua_error: str,
        nova_error: str,
        classification: str = "unclassified",
    ) -> "BothErrorEntry":
        return cls(
            file=normalize_fixture_id(file),
            lua_version=lua_version,
            lua_rc_category=categorize_return_code(lua_rc),
            nova_rc_category=categorize_return_code(nova_rc),
            lua_error_sha256=hash_error_text(lua_error),
            nova_error_sha256=hash_error_text(nova_error),
            lua_error_excerpt=make_excerpt(lua_error),
            nova_error_excerpt=make_excerpt(nova_error),
            classification=classification,
        )

    @classmethod
    def from_json(cls, data: dict[str, Any]) -> "BothErrorEntry":
        return cls(
            file=normalize_fixture_id(str(data["file"])),
            lua_version=str(data["lua_version"]),
            lua_rc_category=categorize_return_code(
                data.get("lua_rc_category", data.get("lua_rc", 1))
            ),
            nova_rc_category=categorize_return_code(
                data.get("nova_rc_category", data.get("nova_rc", 1))
            ),
            lua_error_sha256=str(data["lua_error_sha256"]),
            nova_error_sha256=str(data["nova_error_sha256"]),
            lua_error_excerpt=str(data.get("lua_error_excerpt", "")),
            nova_error_excerpt=str(data.get("nova_error_excerpt", "")),
            classification=str(data.get("classification", "unclassified")),
        )

    def to_json(self) -> dict[str, Any]:
        return {
            "file": self.file,
            "lua_version": self.lua_version,
            "classification": self.classification,
            "lua_rc_category": self.lua_rc_category,
            "nova_rc_category": self.nova_rc_category,
            "lua_error_sha256": self.lua_error_sha256,
            "nova_error_sha256": self.nova_error_sha256,
            "lua_error_excerpt": self.lua_error_excerpt,
            "nova_error_excerpt": self.nova_error_excerpt,
        }

    def same_signature(self, other: "BothErrorEntry") -> bool:
        return (
            self.lua_rc_category == other.lua_rc_category
            and self.nova_rc_category == other.nova_rc_category
            and self.lua_error_sha256 == other.lua_error_sha256
            and self.nova_error_sha256 == other.nova_error_sha256
        )


@dataclass(frozen=True)
class ChangedBothErrorEntry:
    baseline: BothErrorEntry
    current: BothErrorEntry

    def to_json(self) -> dict[str, Any]:
        return {
            "file": self.current.file,
            "lua_version": self.current.lua_version,
            "baseline": self.baseline.to_json(),
            "current": self.current.to_json(),
        }


@dataclass(frozen=True)
class RatchetResult:
    baseline_count: int
    current_count: int
    unchanged_count: int
    duplicate_baseline_keys: tuple[tuple[str, str], ...]
    duplicate_current_keys: tuple[tuple[str, str], ...]
    new_entries: list[BothErrorEntry]
    changed_entries: list[ChangedBothErrorEntry]
    removed_entries: list[BothErrorEntry]
    missing_entries: list[BothErrorEntry]

    @property
    def passed(self) -> bool:
        return (
            not self.duplicate_baseline_keys
            and not self.duplicate_current_keys
            and not self.new_entries
            and not self.changed_entries
            and not self.missing_entries
        )

    def to_json(self) -> dict[str, Any]:
        return {
            "passed": self.passed,
            "baseline_count": self.baseline_count,
            "current_count": self.current_count,
            "unchanged_count": self.unchanged_count,
            "removed_count": len(self.removed_entries),
            "new_count": len(self.new_entries),
            "changed_count": len(self.changed_entries),
            "missing_count": len(self.missing_entries),
            "duplicate_baseline_keys": [
                {"lua_version": key[0], "file": key[1]} for key in self.duplicate_baseline_keys
            ],
            "duplicate_current_keys": [
                {"lua_version": key[0], "file": key[1]} for key in self.duplicate_current_keys
            ],
            "new_entries": [entry.to_json() for entry in self.new_entries[:50]],
            "changed_entries": [entry.to_json() for entry in self.changed_entries[:50]],
            "removed_entries": [entry.to_json() for entry in self.removed_entries[:50]],
            "missing_entries": [entry.to_json() for entry in self.missing_entries[:50]],
        }


def _dedupe_index(entries: list[BothErrorEntry]) -> tuple[dict[tuple[str, str], BothErrorEntry], tuple[tuple[str, str], ...]]:
    index: dict[tuple[str, str], BothErrorEntry] = {}
    duplicates: list[tuple[str, str]] = []
    for entry in entries:
        if entry.key in index:
            duplicates.append(entry.key)
            continue
        index[entry.key] = entry
    return index, tuple(sorted(set(duplicates)))


def load_baseline(data: dict[str, Any]) -> list[BothErrorEntry]:
    entries = data.get("entries", [])
    result: list[BothErrorEntry] = []

    if isinstance(entries, list):
        return [BothErrorEntry.from_json(entry) for entry in entries]

    if isinstance(entries, dict):
        for lua_version, version_entries in entries.items():
            if isinstance(version_entries, list):
                for entry in version_entries:
                    merged = dict(entry)
                    merged.setdefault("lua_version", lua_version)
                    result.append(BothErrorEntry.from_json(merged))
            elif isinstance(version_entries, dict):
                for file, entry in version_entries.items():
                    merged = dict(entry)
                    merged.setdefault("lua_version", lua_version)
                    merged.setdefault("file", file)
                    result.append(BothErrorEntry.from_json(merged))

    return result


def load_baseline_file(path: Path) -> list[BothErrorEntry]:
    with path.open("r", encoding="utf-8") as file:
        data = json.load(file)
    return load_baseline(data)


def check_both_error_ratchet(
    baseline_entries: list[BothErrorEntry],
    current_entries: list[BothErrorEntry],
    current_keys: set[tuple[str, str]] | None = None,
    current_versions: set[str] | None = None,
) -> RatchetResult:
    baseline_index, duplicate_baseline_keys = _dedupe_index(baseline_entries)
    current_index, duplicate_current_keys = _dedupe_index(current_entries)
    has_full_current_keys = current_keys is not None
    if not has_full_current_keys:
        current_keys = set(baseline_index.keys()) | set(current_index.keys())
    if current_versions is None:
        current_versions = {entry.lua_version for entry in current_entries}

    new_entries: list[BothErrorEntry] = []
    changed_entries: list[ChangedBothErrorEntry] = []
    removed_entries: list[BothErrorEntry] = []
    missing_entries: list[BothErrorEntry] = []
    unchanged_count = 0

    for key, current in sorted(current_index.items()):
        baseline = baseline_index.get(key)
        if baseline is None:
            new_entries.append(current)
        elif baseline.same_signature(current):
            unchanged_count += 1
        else:
            changed_entries.append(ChangedBothErrorEntry(baseline=baseline, current=current))

    for key, baseline in sorted(baseline_index.items()):
        if current_versions and key[0] not in current_versions:
            continue
        if key not in current_index and key in current_keys:
            removed_entries.append(baseline)
        elif has_full_current_keys and key not in current_index and key not in current_keys:
            missing_entries.append(baseline)

    return RatchetResult(
        baseline_count=len(baseline_entries),
        current_count=len(current_entries),
        unchanged_count=unchanged_count,
        duplicate_baseline_keys=duplicate_baseline_keys,
        duplicate_current_keys=duplicate_current_keys,
        new_entries=new_entries,
        changed_entries=changed_entries,
        removed_entries=removed_entries,
        missing_entries=missing_entries,
    )


def entries_from_comparison_report(data: dict[str, Any]) -> list[BothErrorEntry]:
    lua_version = str(data.get("lua_version", ""))
    entries: list[BothErrorEntry] = []
    for item in data.get("both_errors", []):
        if "lua_error_sha256" in item and "nova_error_sha256" in item:
            merged = dict(item)
            merged.setdefault("lua_version", lua_version)
            entries.append(BothErrorEntry.from_json(merged))
            continue

        entries.append(
            BothErrorEntry.from_errors(
                file=str(item["file"]),
                lua_version=str(item.get("lua_version", lua_version)),
                lua_rc=int(item.get("lua_rc", 1)),
                nova_rc=int(item.get("nova_rc", 1)),
                lua_error=str(item.get("lua_error", "")),
                nova_error=str(item.get("nova_error", "")),
            )
        )
    return entries


def compared_keys_from_comparison_report(data: dict[str, Any]) -> set[tuple[str, str]]:
    lua_version = str(data.get("lua_version", ""))
    keys: set[tuple[str, str]] = set()
    for section in (
        "matches",
        "mismatches",
        "both_errors",
        "known_divergences",
        "lua_only",
        "nova_only",
    ):
        for item in data.get(section, []):
            file = item.get("file") if isinstance(item, dict) else None
            if file:
                keys.add((str(item.get("lua_version", lua_version)), normalize_fixture_id(str(file))))
    for item in data.get("result_statuses", []):
        if not isinstance(item, dict):
            continue
        if item.get("status") in {"skipped", "missing_outputs"}:
            continue
        file = item.get("file")
        if file:
            keys.add((str(item.get("lua_version", lua_version)), normalize_fixture_id(str(file))))
    return keys


def comparison_report_has_full_statuses(data: dict[str, Any]) -> bool:
    return isinstance(data.get("result_statuses"), list)


def write_baseline(path: Path, entries: list[BothErrorEntry]) -> None:
    data = {
        "schema": 1,
        "description": (
            "Current unclassified both-error comparison signatures. Compared entries that no "
            "longer both-error are allowed as reductions; unobserved baseline keys, new "
            "entries, and changed entries fail the ratchet."
        ),
        "entries": [entry.to_json() for entry in sorted(entries, key=lambda entry: entry.key)],
    }
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8") as file:
        json.dump(data, file, indent=2)
        file.write("\n")


def _load_comparison_files(paths: list[Path]) -> list[BothErrorEntry]:
    entries: list[BothErrorEntry] = []
    for path in paths:
        with path.open("r", encoding="utf-8") as file:
            entries.extend(entries_from_comparison_report(json.load(file)))
    return entries


def _load_comparison_context(
    paths: list[Path],
) -> tuple[list[BothErrorEntry], set[tuple[str, str]] | None, set[str]]:
    entries: list[BothErrorEntry] = []
    compared_keys: set[tuple[str, str]] = set()
    lua_versions: set[str] = set()
    has_full_statuses = True
    for path in paths:
        with path.open("r", encoding="utf-8") as file:
            data = json.load(file)
        lua_version = str(data.get("lua_version", ""))
        if lua_version:
            lua_versions.add(lua_version)
        entries.extend(entries_from_comparison_report(data))
        if comparison_report_has_full_statuses(data):
            compared_keys.update(compared_keys_from_comparison_report(data))
        else:
            has_full_statuses = False
    return entries, compared_keys if has_full_statuses else None, lua_versions


def _print_result(result: RatchetResult) -> None:
    print("=== Lua Both-Error Ratchet ===")
    print(f"Baseline entries: {result.baseline_count}")
    print(f"Current entries:  {result.current_count}")
    print(f"Unchanged:        {result.unchanged_count}")
    print(f"Removed:          {len(result.removed_entries)}")
    print(f"New:              {len(result.new_entries)}")
    print(f"Changed:          {len(result.changed_entries)}")
    print(f"Missing:          {len(result.missing_entries)}")
    if result.duplicate_baseline_keys:
        print(f"Duplicate baseline keys: {len(result.duplicate_baseline_keys)}")
    if result.duplicate_current_keys:
        print(f"Duplicate current keys: {len(result.duplicate_current_keys)}")

    for entry in result.new_entries[:10]:
        print(f"[NEW] {entry.lua_version} {entry.file}")
    for entry in result.changed_entries[:10]:
        print(f"[CHANGED] {entry.current.lua_version} {entry.current.file}")
    for entry in result.missing_entries[:10]:
        print(f"[MISSING] {entry.lua_version} {entry.file}")


def main() -> int:
    parser = argparse.ArgumentParser(description="Check the Lua both-error ratchet")
    parser.add_argument(
        "--baseline",
        type=Path,
        default=DEFAULT_BASELINE_PATH,
        help="Ratchet baseline JSON file",
    )
    parser.add_argument(
        "--comparison-file",
        type=Path,
        action="append",
        required=True,
        help="Comparison report JSON file; pass once per Lua version",
    )
    parser.add_argument(
        "--write-baseline",
        action="store_true",
        help="Write baseline from comparison file entries instead of checking",
    )
    parser.add_argument("--json", action="store_true", help="Print machine-readable result JSON")
    args = parser.parse_args()

    current_entries, compared_keys, lua_versions = _load_comparison_context(args.comparison_file)

    if args.write_baseline:
        write_baseline(args.baseline, current_entries)
        print(f"Wrote {len(current_entries)} entries to {args.baseline}")
        return 0

    baseline_entries = load_baseline_file(args.baseline)
    result = check_both_error_ratchet(
        baseline_entries,
        current_entries,
        current_keys=compared_keys,
        current_versions=lua_versions,
    )

    if args.json:
        print(json.dumps(result.to_json(), indent=2))
    else:
        _print_result(result)

    return 0 if result.passed else 1


if __name__ == "__main__":
    sys.exit(main())
