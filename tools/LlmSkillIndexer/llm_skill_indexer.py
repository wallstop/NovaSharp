#!/usr/bin/env python3
"""
LLM Skill Indexer

Scans .llm/skills/*.md for YAML front-matter metadata and generates
a skills-index.json with categorization, triggers, and validation.

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
import os
import re
import sys
from dataclasses import dataclass, field, asdict
from pathlib import Path
from typing import Optional


# Line count thresholds
LINE_WARNING_THRESHOLD = 300
LINE_ERROR_THRESHOLD = 500

# Valid category and priority values
VALID_CATEGORIES = {"core", "performance", "testing", "lua", "workflow", "meta"}
VALID_PRIORITIES = {"core", "recommended", "reference"}
VALID_METADATA_KEYS = {"triggers", "category", "related", "priority"}


@dataclass
class SkillMetadata:
    """Metadata for a single skill file."""
    name: str
    file_path: str
    line_count: int
    triggers: list = field(default_factory=list)
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

    # Parse simple YAML (we don't need full YAML parsing)
    metadata = {}
    current_key = None
    current_list = None

    for line in front_matter_text.strip().split('\n'):
        # Skip empty lines
        if not line.strip():
            continue

        # Check for list item
        if line.startswith('  - ') or line.startswith('- '):
            if current_key and current_list is not None:
                item = line.lstrip(' -').strip().strip('"\'')
                current_list.append(item)
            continue

        # Check for key: value or key: start of list
        match = re.match(r'^(\w+):\s*(.*)', line)
        if match:
            key = match.group(1)
            value = match.group(2).strip()

            if not value:
                # Start of list
                current_key = key
                current_list = []
                metadata[key] = current_list
            elif value.startswith('[') and value.endswith(']'):
                # Inline list
                items = [item.strip().strip('"\'') for item in value[1:-1].split(',') if item.strip()]
                metadata[key] = items
                current_key = None
                current_list = None
            else:
                # Simple value
                metadata[key] = value.strip('"\'')
                current_key = None
                current_list = None

    return metadata, remaining


def extract_title(content: str) -> str:
    """Extract the first H1 heading from markdown content."""
    match = re.search(r'^#\s+(.+)$', content, re.MULTILINE)
    if match:
        return match.group(1).strip()
    return ""


def count_lines(content: str) -> int:
    """Count the number of lines in content."""
    return len(content.split('\n'))


def validate_metadata(skill: SkillMetadata, metadata: dict) -> None:
    """Validate extracted metadata and add warnings/errors."""
    for unknown_key in sorted(set(metadata) - VALID_METADATA_KEYS):
        skill.validation_warnings.append(
            f"Unknown front-matter field '{unknown_key}'."
        )

    for required_key in ("triggers", "category", "priority"):
        if required_key not in metadata:
            skill.validation_warnings.append(
                f"Missing required front-matter field '{required_key}'."
            )

    triggers = metadata.get("triggers")
    if triggers is not None and (
        not isinstance(triggers, list)
        or not triggers
        or any(not isinstance(trigger, str) or not trigger.strip() for trigger in triggers)
    ):
        skill.validation_warnings.append(
            "Front-matter field 'triggers' must be a non-empty list of strings."
        )

    category = metadata.get("category")
    if category is not None and (
        not isinstance(category, str) or not category.strip()
    ):
        skill.validation_warnings.append(
            "Front-matter field 'category' must be a non-empty string."
        )

    priority = metadata.get("priority")
    if priority is not None and (
        not isinstance(priority, str) or not priority.strip()
    ):
        skill.validation_warnings.append(
            "Front-matter field 'priority' must be a non-empty string."
        )

    related = metadata.get("related")
    if related is not None and (
        not isinstance(related, list)
        or any(not isinstance(item, str) or not item.strip() for item in related)
    ):
        skill.validation_warnings.append(
            "Front-matter field 'related' must be a list of non-empty strings."
        )

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
            "Consider splitting into multiple skills or extracting code samples to .llm/code-samples/"
        )
    elif skill.line_count > LINE_WARNING_THRESHOLD:
        skill.validation_warnings.append(
            f"File exceeds {LINE_WARNING_THRESHOLD} lines ({skill.line_count} lines). "
            "Consider extracting reusable code samples to .llm/code-samples/"
        )

    # Check for missing front-matter
    if not skill.has_front_matter:
        skill.validation_warnings.append(
            "Missing YAML front-matter. Add triggers, category, and priority metadata."
        )
    elif not skill.triggers:
        skill.validation_warnings.append(
            "No trigger keywords defined. Add 'triggers:' to front-matter."
        )


def process_skill_file(file_path: Path, repo_root: Path) -> SkillMetadata:
    """Process a single skill markdown file."""
    content = file_path.read_text(encoding='utf-8')

    # Extract base info
    name = file_path.stem
    rel_path = str(file_path.relative_to(repo_root))
    line_count = count_lines(content)

    skill = SkillMetadata(
        name=name,
        file_path=rel_path,
        line_count=line_count,
    )

    # Extract front-matter
    front_matter, remaining = extract_front_matter(content)
    skill.has_front_matter = bool(front_matter)

    # Apply front-matter metadata
    if 'triggers' in front_matter:
        skill.triggers = front_matter['triggers'] if isinstance(front_matter['triggers'], list) else [front_matter['triggers']]

    if 'category' in front_matter:
        skill.category = front_matter['category']

    if 'related' in front_matter:
        skill.related = front_matter['related'] if isinstance(front_matter['related'], list) else [front_matter['related']]

    if 'priority' in front_matter:
        skill.priority = front_matter['priority']

    # Extract title from content
    skill.title = extract_title(remaining if remaining else content)

    # Validate
    validate_metadata(skill, front_matter)

    return skill


def generate_index(repo_root: Path) -> dict:
    """Generate the complete skills index."""
    skills_dir = repo_root / '.llm' / 'skills'

    if not skills_dir.exists():
        raise FileNotFoundError(f"Skills directory not found: {skills_dir}")

    skills = []
    skill_files = sorted(skills_dir.glob('*.md'))

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
        "version": "1.0.0",
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
        print(f"WARNING: {summary['files_missing_front_matter']} file(s) missing YAML front-matter", file=sys.stderr)

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
        description="Generate skills index from .llm/skills/*.md files"
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
        index = generate_index(repo_root)
    except Exception as e:
        print(f"ERROR: Failed to generate index: {e}", file=sys.stderr)
        sys.exit(1)

    # Print validation report
    print_validation_report(index, verbose=args.verbose)

    output_path = Path(args.output) if args.output else (repo_root / '.llm' / 'skills-index.json')
    rendered_index = json.dumps(index, indent=2) + '\n'

    if args.check:
        check_errors = check_index(index, output_path, rendered_index)
        if check_errors:
            for check_error in check_errors:
                print(f"\nERROR: {check_error}", file=sys.stderr)
            sys.exit(1)
        print(f"\nIndex is current: {output_path}", file=sys.stderr)
        sys.exit(0)

    output_path.write_text(rendered_index, encoding='utf-8')
    print(f"\nWrote index to: {output_path}", file=sys.stderr)

    sys.exit(0)


if __name__ == "__main__":
    main()
