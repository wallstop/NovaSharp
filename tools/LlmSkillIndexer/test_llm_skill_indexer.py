#!/usr/bin/env python3
"""Tests for LLM skill metadata and fail-closed index validation."""

import tempfile
import unittest
from pathlib import Path

from llm_skill_indexer import (
    SkillMetadata,
    check_index,
    extract_front_matter,
    generate_index,
    validate_metadata,
)


def make_index(warnings: int = 0, errors: int = 0) -> dict:
    return {
        "validation_summary": {
            "total_warnings": warnings,
            "total_errors": errors,
        }
    }


class CheckIndexTests(unittest.TestCase):
    def test_accepts_current_index_without_modifying_it(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            output_path = Path(temporary_directory) / "skills-index.json"
            output_path.write_text("current\n", encoding="utf-8")
            before = output_path.stat().st_mtime_ns

            self.assertEqual([], check_index(make_index(), output_path, "current\n"))
            self.assertEqual(before, output_path.stat().st_mtime_ns)

    def test_rejects_missing_index(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            output_path = Path(temporary_directory) / "skills-index.json"

            errors = check_index(make_index(), output_path, "current\n")

            self.assertTrue(any("missing" in error for error in errors))

    def test_rejects_stale_index(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            output_path = Path(temporary_directory) / "skills-index.json"
            output_path.write_text("stale\n", encoding="utf-8")

            errors = check_index(make_index(), output_path, "current\n")

            self.assertTrue(any("stale" in error for error in errors))

    def test_rejects_warnings_and_errors(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            output_path = Path(temporary_directory) / "skills-index.json"
            output_path.write_text("current\n", encoding="utf-8")

            errors = check_index(
                make_index(warnings=1, errors=1),
                output_path,
                "current\n",
            )

            self.assertEqual(2, len(errors))


class MetadataTests(unittest.TestCase):
    def test_requires_category_and_priority(self) -> None:
        metadata, _ = extract_front_matter(
            '---\ntriggers:\n  - "test"\n---\n# Test\n'
        )
        skill = SkillMetadata(
            name="test",
            file_path=".llm/skills/test.md",
            line_count=6,
            triggers=metadata["triggers"],
            has_front_matter=True,
        )

        validate_metadata(skill, metadata)

        self.assertTrue(any("'category'" in warning for warning in skill.validation_warnings))
        self.assertTrue(any("'priority'" in warning for warning in skill.validation_warnings))

    def test_rejects_empty_or_wrong_type_metadata_fields(self) -> None:
        metadata = {
            "triggers": [],
            "category": [],
            "priority": [],
            "related": "not-a-list",
        }
        skill = SkillMetadata(
            name="test",
            file_path=".llm/skills/test.md",
            line_count=6,
            triggers=[],
            category=metadata["category"],
            priority=metadata["priority"],
            related=[metadata["related"]],
            has_front_matter=True,
        )

        validate_metadata(skill, metadata)

        for field in ("triggers", "category", "priority", "related"):
            self.assertTrue(
                any(f"'{field}'" in warning for warning in skill.validation_warnings)
            )

    def test_rejects_unknown_metadata_field(self) -> None:
        metadata = {
            "triggers": ["test"],
            "category": "core",
            "priority": "core",
            "relations": ["unrecognised key"],
        }
        skill = SkillMetadata(
            name="test",
            file_path=".llm/skills/test.md",
            line_count=6,
            triggers=metadata["triggers"],
            category=metadata["category"],
            priority=metadata["priority"],
            has_front_matter=True,
        )

        validate_metadata(skill, metadata)

        self.assertTrue(
            any("'relations'" in warning for warning in skill.validation_warnings)
        )

    def test_rejects_malformed_front_matter(self) -> None:
        metadata, _ = extract_front_matter("---\ntriggers:\n  - test\n# Missing close\n")
        skill = SkillMetadata(
            name="test",
            file_path=".llm/skills/test.md",
            line_count=4,
        )

        validate_metadata(skill, metadata)

        self.assertTrue(any("front-matter" in warning for warning in skill.validation_warnings))

    def test_generation_is_deterministic(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            repo_root = Path(temporary_directory)
            skills_directory = repo_root / ".llm" / "skills"
            skills_directory.mkdir(parents=True)
            (skills_directory / "second.md").write_text(
                "---\ntriggers:\n  - second\ncategory: testing\npriority: core\n"
                "---\n# Second\n",
                encoding="utf-8",
            )
            (skills_directory / "first.md").write_text(
                "---\ntriggers:\n  - first\ncategory: core\npriority: reference\n"
                "---\n# First\n",
                encoding="utf-8",
            )

            first = generate_index(repo_root)
            second = generate_index(repo_root)

            self.assertEqual(first, second)
            self.assertEqual(
                ["first", "second"],
                [skill["name"] for skill in first["skills"]],
            )

    def test_related_skills_must_resolve(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            repo_root = Path(temporary_directory)
            skills_directory = repo_root / ".llm" / "skills"
            skills_directory.mkdir(parents=True)
            (skills_directory / "valid.md").write_text(
                "---\ntriggers:\n  - valid\ncategory: core\n"
                "related:\n  - missing\npriority: core\n---\n# Valid\n",
                encoding="utf-8",
            )

            index = generate_index(repo_root)

            self.assertEqual(1, index["validation_summary"]["total_warnings"])
            self.assertTrue(
                any(
                    "does not exist" in warning
                    for warning in index["skills"][0]["validation_warnings"]
                )
            )

    def test_related_skills_accept_existing_target(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            repo_root = Path(temporary_directory)
            skills_directory = repo_root / ".llm" / "skills"
            skills_directory.mkdir(parents=True)
            (skills_directory / "target.md").write_text(
                "---\ntriggers:\n  - target\ncategory: core\npriority: core\n"
                "---\n# Target\n",
                encoding="utf-8",
            )
            (skills_directory / "source.md").write_text(
                "---\ntriggers:\n  - source\ncategory: core\n"
                "related:\n  - target\npriority: core\n---\n# Source\n",
                encoding="utf-8",
            )

            index = generate_index(repo_root)

            self.assertEqual(0, index["validation_summary"]["total_warnings"])

    def test_wrong_type_category_warns_without_crashing_generation(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            repo_root = Path(temporary_directory)
            skills_directory = repo_root / ".llm" / "skills"
            skills_directory.mkdir(parents=True)
            (skills_directory / "invalid.md").write_text(
                "---\ntriggers:\n  - invalid\ncategory:\n  - core\n"
                "priority: core\n---\n# Invalid\n",
                encoding="utf-8",
            )

            index = generate_index(repo_root)

            self.assertEqual(["invalid"], index["categories"]["uncategorized"])
            self.assertGreater(index["validation_summary"]["total_warnings"], 0)

    def test_skill_paths_use_posix_separators(self) -> None:
        """`--check` compares the committed index byte-for-byte.

        A native-separator path would make an index regenerated on Windows look
        stale on Unix CI and vice versa.
        """
        with tempfile.TemporaryDirectory() as temporary_directory:
            repo_root = Path(temporary_directory)
            skills_directory = repo_root / ".llm" / "skills"
            skills_directory.mkdir(parents=True)
            (skills_directory / "example.md").write_text(
                "---\ntriggers:\n  - example\ncategory: core\npriority: core\n---\n"
                "# Example\n",
                encoding="utf-8",
            )

            index = generate_index(repo_root)

            (skill,) = index["skills"]
            self.assertEqual(".llm/skills/example.md", skill["file_path"])
            self.assertNotIn("\\", skill["file_path"])


if __name__ == "__main__":
    unittest.main()
