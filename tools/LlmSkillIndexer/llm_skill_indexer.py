#!/usr/bin/env python3
"""
LLM Skill Indexer

Scans .llm/skills/*/SKILL.md for Agent Skills metadata and generates
a skills-index.json with descriptions, categorization, and validation.

Usage:
    python3 tools/LlmSkillIndexer/llm_skill_indexer.py [--check] [--verbose]

Options:
    --check     Validate metadata and the committed index without writing files
    --verbose   Show detailed output for each skill

Exit codes:
    0  Generation completed, or check mode found no issues
    1  Check mode found warnings, errors, a missing index, or a stale index
"""

import argparse
import json
import re
import sys
from dataclasses import dataclass, field, asdict
from pathlib import Path

import yaml


# Line count thresholds
LINE_WARNING_THRESHOLD = 150
LINE_ERROR_THRESHOLD = 200

# Valid category and priority values
VALID_CATEGORIES = {"core", "performance", "testing", "lua", "workflow", "meta"}
VALID_PRIORITIES = {"core", "recommended", "reference"}
VALID_FRONT_MATTER_KEYS = {
    "name",
    "description",
    "license",
    "compatibility",
    "metadata",
    "allowed-tools",
}
SKILL_NAME_PATTERN = re.compile(r"^[a-z0-9]+(?:-[a-z0-9]+)*$")
CLIENT_RESERVED_NAME_WORDS = {"anthropic", "claude"}


@dataclass
class SkillMetadata:
    """Metadata for a single skill file."""
    name: str
    description: str
    file_path: str
    line_count: int
    category: str = "uncategorized"
    related: list = field(default_factory=list)
    priority: str = "reference"
    has_front_matter: bool = False
    title: str = ""
    validation_warnings: list = field(default_factory=list)
    validation_errors: list = field(default_factory=list)


def extract_front_matter(content: str) -> tuple[dict, str]:
    """
    Extract YAML front-matter from markdown content.

    Returns (metadata_dict, remaining_content).
    Front-matter is between --- markers at the start of the file.
    """
    if not content.startswith("---"):
        return {}, content

    # Find the closing ---
    end_match = re.search(r'\n---\s*\n', content[3:])
    if not end_match:
        return {}, content

    front_matter_text = content[3:end_match.start() + 3]
    remaining = content[end_match.end() + 3:]

    try:
        metadata = yaml.safe_load(front_matter_text)
    except yaml.YAMLError:
        return {}, content

    return metadata if isinstance(metadata, dict) else {}, remaining


def extract_title(content: str) -> str:
    """Extract the first H1 heading from markdown content."""
    match = re.search(r'^#\s+(.+)$', content, re.MULTILINE)
    if match:
        return match.group(1).strip()
    return ""


def count_lines(content: str) -> int:
    """Count the number of lines in content."""
    return len(content.splitlines())


def validate_metadata(
    skill: SkillMetadata,
    metadata: dict,
    directory_name: str = "",
) -> None:
    """Validate extracted metadata and add warnings/errors."""
    for unknown_key in sorted(set(metadata) - VALID_FRONT_MATTER_KEYS):
        skill.validation_errors.append(
            f"Unknown front-matter field '{unknown_key}'."
        )

    for required_key in ("name", "description"):
        if required_key not in metadata:
            skill.validation_errors.append(
                f"Missing required front-matter field '{required_key}'."
            )

    name = metadata.get("name")
    if name is not None:
        if not isinstance(name, str) or not SKILL_NAME_PATTERN.fullmatch(name):
            skill.validation_errors.append(
                "Front-matter field 'name' must contain only lowercase letters, "
                "digits, and single hyphens."
            )
        elif len(name) > 64:
            skill.validation_errors.append(
                "Front-matter field 'name' exceeds 64 characters."
            )
        elif CLIENT_RESERVED_NAME_WORDS.intersection(name.split("-")):
            skill.validation_errors.append(
                "Front-matter field 'name' contains a Claude Code reserved word."
            )
        if directory_name and name != directory_name:
            skill.validation_errors.append(
                f"Skill name '{name}' does not match parent directory "
                f"'{directory_name}'."
            )

    description = metadata.get("description")
    if description is not None:
        if not isinstance(description, str) or not description.strip():
            skill.validation_errors.append(
                "Front-matter field 'description' must be a non-empty string."
            )
        elif len(description) > 1024:
            skill.validation_errors.append(
                "Front-matter field 'description' exceeds 1024 characters."
            )

    compatibility = metadata.get("compatibility")
    if compatibility is not None and (
        not isinstance(compatibility, str)
        or not compatibility.strip()
        or len(compatibility) > 500
    ):
        skill.validation_errors.append(
            "Front-matter field 'compatibility' must be a non-empty string "
            "of at most 500 characters."
        )

    license_value = metadata.get("license")
    if license_value is not None and (
        not isinstance(license_value, str) or not license_value.strip()
    ):
        skill.validation_errors.append(
            "Front-matter field 'license' must be a non-empty string."
        )

    allowed_tools = metadata.get("allowed-tools")
    if allowed_tools is not None and (
        not isinstance(allowed_tools, str) or not allowed_tools.strip()
    ):
        skill.validation_errors.append(
            "Front-matter field 'allowed-tools' must be a non-empty string."
        )

    project_metadata = metadata.get("metadata", {})
    if not isinstance(project_metadata, dict):
        skill.validation_errors.append(
            "Front-matter field 'metadata' must be a string-to-string mapping."
        )
    else:
        for key, value in project_metadata.items():
            if not isinstance(key, str) or not isinstance(value, str):
                skill.validation_errors.append(
                    "Front-matter field 'metadata' must be a string-to-string mapping."
                )
                break

    # Check category
    if isinstance(skill.category, str) and skill.category and skill.category not in VALID_CATEGORIES:
        skill.validation_warnings.append(
            f"Unknown category '{skill.category}'. Valid: {', '.join(sorted(VALID_CATEGORIES))}"
        )

    # Check priority
    if isinstance(skill.priority, str) and skill.priority and skill.priority not in VALID_PRIORITIES:
        skill.validation_warnings.append(
            f"Unknown priority '{skill.priority}'. Valid: {', '.join(sorted(VALID_PRIORITIES))}"
        )

    # Check line count
    if skill.line_count > LINE_ERROR_THRESHOLD:
        skill.validation_errors.append(
            f"File exceeds {LINE_ERROR_THRESHOLD} lines ({skill.line_count} lines). "
            "Move details into the skill's references/ directory."
        )
    elif skill.line_count > LINE_WARNING_THRESHOLD:
        skill.validation_warnings.append(
            f"File exceeds {LINE_WARNING_THRESHOLD} lines ({skill.line_count} lines). "
            "Target at most 150 lines by moving details into references/."
        )

    # Check for missing front-matter
    if not skill.has_front_matter:
        skill.validation_errors.append(
            "Missing YAML front-matter. Add Agent Skills name and description metadata."
        )


def process_skill_file(file_path: Path, repo_root: Path) -> SkillMetadata:
    """Process a single skill markdown file."""
    content = file_path.read_text(encoding='utf-8')

    # Extract base info
    directory_name = file_path.parent.name
    # POSIX separators unconditionally. This lands in the committed
    # `.llm/skills-index.json`, and `--check` now compares that file byte-for-byte,
    # so a native-separator path would make an index regenerated on Windows look
    # stale on Unix CI and vice versa.
    rel_path = file_path.relative_to(repo_root).as_posix()
    line_count = count_lines(content)

    front_matter, remaining = extract_front_matter(content)
    project_metadata = front_matter.get("metadata", {})
    if not isinstance(project_metadata, dict):
        project_metadata = {}
    related_value = project_metadata.get("related", "")
    related = (
        [item.strip() for item in related_value.split(",") if item.strip()]
        if isinstance(related_value, str)
        else []
    )

    skill = SkillMetadata(
        name=front_matter.get("name", directory_name),
        description=front_matter.get("description", ""),
        file_path=rel_path,
        line_count=line_count,
        category=project_metadata.get("category", "uncategorized"),
        related=related,
        priority=project_metadata.get("priority", "reference"),
    )

    skill.has_front_matter = bool(front_matter)

    # Extract title from content
    skill.title = extract_title(remaining if remaining else content)

    # Validate
    validate_metadata(skill, front_matter, directory_name)

    return skill


def validate_discovery_aliases(repo_root: Path) -> list[str]:
    """Validate the client discovery aliases that expose canonical `.llm` skills."""
    errors = []
    expected_target = Path("../.llm/skills")
    canonical_directory = (repo_root / ".llm" / "skills").resolve()

    for alias_name in (".agents/skills", ".claude/skills"):
        alias_path = repo_root / alias_name
        if not alias_path.is_symlink():
            errors.append(
                f"Discovery alias '{alias_name}' must be a symlink to "
                "'../.llm/skills'."
            )
            continue
        if alias_path.readlink() != expected_target:
            errors.append(
                f"Discovery alias '{alias_name}' targets "
                f"'{alias_path.readlink()}', expected '../.llm/skills'."
            )
            continue
        if alias_path.resolve() != canonical_directory:
            errors.append(
                f"Discovery alias '{alias_name}' does not resolve to '.llm/skills'."
            )

    return errors


def generate_index(repo_root: Path, validate_aliases: bool = False) -> dict:
    """Generate the complete skills index."""
    skills_dir = repo_root / '.llm' / 'skills'

    if not skills_dir.exists():
        raise FileNotFoundError(f"Skills directory not found: {skills_dir}")

    skills = []
    skill_files = sorted(skills_dir.glob('*/SKILL.md'))
    structure_errors = []

    for child in sorted(skills_dir.iterdir()):
        if child.is_file():
            structure_errors.append(
                f"Legacy flat skill '{child.relative_to(repo_root).as_posix()}' must "
                "move to '<name>/SKILL.md'."
            )
        elif child.is_dir() and not (child / "SKILL.md").is_file():
            structure_errors.append(
                f"Skill directory '{child.relative_to(repo_root).as_posix()}' is "
                "missing SKILL.md."
            )

    if validate_aliases:
        structure_errors.extend(validate_discovery_aliases(repo_root))

    for skill_file in skill_files:
        skill = process_skill_file(skill_file, repo_root)
        skills.append(skill)

    skill_names = {skill.name for skill in skills}
    for skill in skills:
        for related_name in skill.related:
            if related_name not in skill_names:
                skill.validation_warnings.append(
                    f"Related skill '{related_name}' does not exist."
                )

    # Build index structure
    index = {
        "version": "2.0.0",
        "generated_by": "tools/LlmSkillIndexer/llm_skill_indexer.py",
        "skills_count": len(skills),
        "categories": {},
        "skills": [],
        "validation_summary": {
            "total_warnings": 0,
            "total_errors": 0,
            "files_over_warning_threshold": 0,
            "files_over_error_threshold": 0,
            "files_missing_front_matter": 0,
            "structure_errors": structure_errors,
        }
    }

    # Build category index
    for skill in skills:
        cat = (
            skill.category
            if isinstance(skill.category, str) and skill.category
            else "uncategorized"
        )
        if cat not in index["categories"]:
            index["categories"][cat] = []
        index["categories"][cat].append(skill.name)

        # Update validation summary
        index["validation_summary"]["total_warnings"] += len(skill.validation_warnings)
        index["validation_summary"]["total_errors"] += len(skill.validation_errors)

        if skill.line_count > LINE_ERROR_THRESHOLD:
            index["validation_summary"]["files_over_error_threshold"] += 1
        elif skill.line_count > LINE_WARNING_THRESHOLD:
            index["validation_summary"]["files_over_warning_threshold"] += 1

        if not skill.has_front_matter:
            index["validation_summary"]["files_missing_front_matter"] += 1

        # Add to skills list (convert dataclass to dict)
        skill_dict = asdict(skill)
        index["skills"].append(skill_dict)

    # Sort categories
    index["categories"] = dict(sorted(index["categories"].items()))
    index["validation_summary"]["total_errors"] += len(structure_errors)

    return index


def print_validation_report(index: dict, verbose: bool = False) -> None:
    """Print a validation report to stderr."""
    summary = index["validation_summary"]

    print("\n=== LLM Skills Index Validation Report ===\n", file=sys.stderr)
    print(f"Total skills: {index['skills_count']}", file=sys.stderr)
    print(f"Categories: {', '.join(sorted(index['categories'].keys()))}", file=sys.stderr)
    print(file=sys.stderr)

    # Line count warnings
    if summary["files_over_error_threshold"] > 0:
        print(f"ERROR: {summary['files_over_error_threshold']} file(s) exceed {LINE_ERROR_THRESHOLD} lines", file=sys.stderr)
    if summary["files_over_warning_threshold"] > 0:
        print(f"WARNING: {summary['files_over_warning_threshold']} file(s) exceed {LINE_WARNING_THRESHOLD} lines", file=sys.stderr)
    if summary["files_missing_front_matter"] > 0:
        print(f"ERROR: {summary['files_missing_front_matter']} file(s) missing YAML front-matter", file=sys.stderr)
    for structure_error in summary["structure_errors"]:
        print(f"ERROR: {structure_error}", file=sys.stderr)

    # Detailed issues
    if verbose or summary["total_errors"] > 0 or summary["total_warnings"] > 0:
        print("\n--- Detailed Issues ---\n", file=sys.stderr)

        for skill in index["skills"]:
            if skill["validation_errors"] or skill["validation_warnings"]:
                print(f"{skill['name']} ({skill['file_path']}):", file=sys.stderr)
                for error in skill["validation_errors"]:
                    print(f"  ERROR: {error}", file=sys.stderr)
                for warning in skill["validation_warnings"]:
                    print(f"  WARNING: {warning}", file=sys.stderr)
                print(file=sys.stderr)

    # Summary
    if summary["total_errors"] == 0 and summary["total_warnings"] == 0:
        print("\nAll skills validated successfully!", file=sys.stderr)
    else:
        print(f"\nTotal: {summary['total_errors']} error(s), {summary['total_warnings']} warning(s)", file=sys.stderr)


def check_index(index: dict, output_path: Path, rendered_index: str) -> list[str]:
    """Return fail-closed check-mode errors without modifying the index."""
    errors = []
    summary = index["validation_summary"]

    if summary["total_errors"] > 0:
        errors.append("Skill metadata has validation errors.")
    if summary["total_warnings"] > 0:
        errors.append("Skill metadata has validation warnings.")
    if not output_path.exists():
        errors.append(f"Generated index is missing: {output_path}")
    elif output_path.read_text(encoding='utf-8') != rendered_index:
        errors.append(
            "Generated index is stale. "
            "Run tools/LlmSkillIndexer/llm_skill_indexer.py and commit the result."
        )

    return errors


def main():
    parser = argparse.ArgumentParser(
        description="Generate an index from .llm/skills/*/SKILL.md files"
    )
    parser.add_argument(
        '--check',
        action='store_true',
        help='Validate metadata and committed index without writing files'
    )
    parser.add_argument(
        '--verbose',
        action='store_true',
        help='Show detailed output for each skill'
    )
    parser.add_argument(
        '--output',
        type=str,
        default=None,
        help='Output file path (default: .llm/skills-index.json)'
    )
    args = parser.parse_args()

    # Find repo root (directory containing .llm/)
    script_path = Path(__file__).resolve()
    repo_root = script_path.parent.parent.parent

    # Verify we found the right directory
    if not (repo_root / '.llm').exists():
        print(f"ERROR: Could not find .llm/ directory. Expected at {repo_root / '.llm'}", file=sys.stderr)
        sys.exit(1)

    # Generate index
    try:
        index = generate_index(repo_root, validate_aliases=True)
    except Exception as e:
        print(f"ERROR: Failed to generate index: {e}", file=sys.stderr)
        sys.exit(1)

    # Print validation report
    print_validation_report(index, verbose=args.verbose)

    output_path = Path(args.output) if args.output else (repo_root / '.llm' / 'skills-index.json')
    rendered_index = json.dumps(index, indent=2) + '\n'
    display_path = (
        output_path.relative_to(repo_root)
        if output_path.is_relative_to(repo_root)
        else output_path
    )

    if args.check:
        check_errors = check_index(index, output_path, rendered_index)
        if check_errors:
            for check_error in check_errors:
                print(f"\nERROR: {check_error}", file=sys.stderr)
            sys.exit(1)
        print(f"\nIndex is current: {display_path}", file=sys.stderr)
        sys.exit(0)

    output_path.write_text(rendered_index, encoding='utf-8')
    print(f"\nWrote index to: {display_path}", file=sys.stderr)

    sys.exit(0)


if __name__ == "__main__":
    main()
