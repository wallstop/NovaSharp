#!/usr/bin/env python3
"""Tests for Agent Skills metadata and fail-closed index validation."""

import tempfile
import unittest
from pathlib import Path

from llm_skill_indexer import (
    SkillMetadata,
    check_index,
    extract_front_matter,
    generate_index,
    validate_discovery_aliases,
    validate_metadata,
)


def make_index(warnings: int = 0, errors: int = 0) -> dict:
    return {
        "validation_summary": {
            "total_warnings": warnings,
            "total_errors": errors,
        }
    }


def write_skill(
    repo_root: Path,
    name: str,
    *,
    description: str = "Use when testing Agent Skills behavior.",
    category: str = "testing",
    priority: str = "core",
    related: str = "",
) -> Path:
    skill_directory = repo_root / ".llm" / "skills" / name
    skill_directory.mkdir(parents=True, exist_ok=True)
    related_line = f"  related: {related}\n" if related else ""
    skill_file = skill_directory / "SKILL.md"
    skill_file.write_text(
        "---\n"
        f"name: {name}\n"
        f"description: {description}\n"
        "metadata:\n"
        f"  category: {category}\n"
        f"  priority: {priority}\n"
        f"{related_line}"
        "---\n"
        f"# {name}\n",
        encoding="utf-8",
    )
    return skill_file


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
    def test_accepts_agent_skills_front_matter(self) -> None:
        metadata, _ = extract_front_matter(
            "---\n"
            "name: test-skill\n"
            "description: Use when testing the Agent Skills layout.\n"
            "metadata:\n"
            "  category: testing\n"
            "  priority: core\n"
            "  related: deterministic-testing, change-path-verification\n"
            "---\n"
            "# Test Skill\n"
        )
        skill = SkillMetadata(
            name="test-skill",
            description=metadata["description"],
            file_path=".llm/skills/test-skill/SKILL.md",
            line_count=10,
            category=metadata["metadata"]["category"],
            priority=metadata["metadata"]["priority"],
            related=["deterministic-testing", "change-path-verification"],
            has_front_matter=True,
        )

        validate_metadata(skill, metadata, "test-skill")

        self.assertEqual([], skill.validation_warnings)
        self.assertEqual([], skill.validation_errors)

    def test_requires_name_and_description(self) -> None:
        skill = SkillMetadata(
            name="test",
            description="",
            file_path=".llm/skills/test/SKILL.md",
            line_count=4,
            has_front_matter=True,
        )

        validate_metadata(skill, {"metadata": {}}, "test")

        for field in ("name", "description"):
            self.assertTrue(
                any(f"'{field}'" in error for error in skill.validation_errors)
            )

    def test_rejects_unknown_front_matter_field(self) -> None:
        metadata = {
            "name": "test",
            "description": "Use when testing metadata validation.",
            "triggers": ["legacy"],
        }
        skill = SkillMetadata(
            name="test",
            description=metadata["description"],
            file_path=".llm/skills/test/SKILL.md",
            line_count=6,
            has_front_matter=True,
        )

        validate_metadata(skill, metadata, "test")

        self.assertTrue(any("triggers" in error for error in skill.validation_errors))

    def test_rejects_malformed_front_matter(self) -> None:
        metadata, _ = extract_front_matter("---\nname: [broken\n---\n# Test\n")
        skill = SkillMetadata(
            name="test",
            description="",
            file_path=".llm/skills/test/SKILL.md",
            line_count=4,
        )

        validate_metadata(skill, metadata, "test")

        self.assertTrue(any("front-matter" in error for error in skill.validation_errors))

    def test_rejects_name_that_does_not_match_parent_directory(self) -> None:
        metadata = {
            "name": "wrong-name",
            "description": "Use when testing name validation.",
        }
        skill = SkillMetadata(
            name="wrong-name",
            description=metadata["description"],
            file_path=".llm/skills/right-name/SKILL.md",
            line_count=6,
            has_front_matter=True,
        )

        validate_metadata(skill, metadata, "right-name")

        self.assertTrue(
            any("parent directory" in error for error in skill.validation_errors)
        )

    def test_rejects_cross_client_reserved_name(self) -> None:
        metadata = {
            "name": "claude-helper",
            "description": "Use when testing cross-client name validation.",
        }
        skill = SkillMetadata(
            name="claude-helper",
            description=metadata["description"],
            file_path=".llm/skills/claude-helper/SKILL.md",
            line_count=6,
            has_front_matter=True,
        )

        validate_metadata(skill, metadata, "claude-helper")

        self.assertTrue(
            any("reserved word" in error for error in skill.validation_errors)
        )

    def test_line_limits_warn_at_target_and_fail_above_maximum(self) -> None:
        metadata = {
            "name": "test",
            "description": "Use when testing line limits.",
        }
        warning_skill = SkillMetadata(
            name="test",
            description=metadata["description"],
            file_path=".llm/skills/test/SKILL.md",
            line_count=151,
            has_front_matter=True,
        )
        error_skill = SkillMetadata(
            name="test",
            description=metadata["description"],
            file_path=".llm/skills/test/SKILL.md",
            line_count=201,
            has_front_matter=True,
        )

        validate_metadata(warning_skill, metadata, "test")
        validate_metadata(error_skill, metadata, "test")

        self.assertTrue(
            any("150" in warning for warning in warning_skill.validation_warnings)
        )
        self.assertTrue(
            any("200" in error for error in error_skill.validation_errors)
        )

    def test_generation_discovers_standard_skill_directories(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            repo_root = Path(temporary_directory)
            write_skill(repo_root, "example-skill")

            index = generate_index(repo_root)

            (skill,) = index["skills"]
            self.assertEqual("example-skill", skill["name"])
            self.assertEqual(
                ".llm/skills/example-skill/SKILL.md",
                skill["file_path"],
            )

    def test_generation_is_deterministic(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            repo_root = Path(temporary_directory)
            write_skill(repo_root, "second")
            write_skill(repo_root, "first", category="core", priority="reference")

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
            write_skill(repo_root, "valid", related="missing")

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
            write_skill(repo_root, "target")
            write_skill(repo_root, "source", related="target")

            index = generate_index(repo_root)

            self.assertEqual(0, index["validation_summary"]["total_warnings"])

    def test_rejects_legacy_flat_layout(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            repo_root = Path(temporary_directory)
            skills_directory = repo_root / ".llm" / "skills"
            skills_directory.mkdir(parents=True)
            (skills_directory / "legacy.md").write_text("# Legacy\n", encoding="utf-8")

            index = generate_index(repo_root)

            self.assertEqual(1, index["validation_summary"]["total_errors"])
            self.assertTrue(index["validation_summary"]["structure_errors"])

    def test_skill_paths_use_posix_separators(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            repo_root = Path(temporary_directory)
            write_skill(repo_root, "example")

            index = generate_index(repo_root)

            (skill,) = index["skills"]
            self.assertEqual(".llm/skills/example/SKILL.md", skill["file_path"])
            self.assertNotIn("\\", skill["file_path"])

    def test_discovery_aliases_point_to_canonical_skills(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            repo_root = Path(temporary_directory)
            (repo_root / ".llm" / "skills").mkdir(parents=True)
            for directory in (".agents", ".claude"):
                (repo_root / directory).mkdir()
                (repo_root / directory / "skills").symlink_to("../.llm/skills")

            self.assertEqual([], validate_discovery_aliases(repo_root))

    def test_discovery_aliases_reject_missing_or_wrong_targets(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            repo_root = Path(temporary_directory)
            (repo_root / ".llm" / "skills").mkdir(parents=True)
            (repo_root / ".agents").mkdir()
            (repo_root / ".agents" / "skills").symlink_to("../wrong")

            errors = validate_discovery_aliases(repo_root)

            self.assertEqual(2, len(errors))
            self.assertTrue(any(".agents/skills" in error for error in errors))
            self.assertTrue(any(".claude/skills" in error for error in errors))


if __name__ == "__main__":
    unittest.main()
