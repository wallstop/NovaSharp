#!/usr/bin/env python3
"""Focused tests for check-plan-hygiene.py."""

from __future__ import annotations

import importlib.util
import unittest
from pathlib import Path

SCRIPT_PATH = Path(__file__).with_name("check-plan-hygiene.py")
SPEC = importlib.util.spec_from_file_location("check_plan_hygiene", SCRIPT_PATH)
if SPEC is None or SPEC.loader is None:
    raise RuntimeError(f"Could not load {SCRIPT_PATH}")
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)


class PlanHygieneTests(unittest.TestCase):
    def test_accepts_lean_active_queue(self) -> None:
        content = "# Plan\n\n## Now\n\n- [ ] Deliver outcome.\n"
        self.assertEqual([], MODULE.find_violations(content))

    def test_rejects_excess_lines(self) -> None:
        content = "\n".join(f"line {index}" for index in range(MODULE.MAX_LINES + 1))
        self.assertIn("maximum", MODULE.find_violations(content)[0])

    def test_rejects_completed_checkbox(self) -> None:
        violations = MODULE.find_violations("# Plan\n\n- [x] Done\n")
        self.assertTrue(any("completed checklist" in value for value in violations))

    def test_rejects_all_unordered_completed_checkbox_markers(self) -> None:
        content = "* [x] First\n+ [X] Second\n"
        violations = MODULE.find_violations(content)
        self.assertIn("contains 2 completed checklist item(s)", violations)

    def test_rejects_ordered_completed_checkbox_markers(self) -> None:
        content = "1. [x] First\n2) [X] Second\n"
        violations = MODULE.find_violations(content)
        self.assertIn("contains 2 completed checklist item(s)", violations)

    def test_rejects_blockquoted_completed_checkboxes(self) -> None:
        content = "> - [x] First\n> 1. [X] Second\n> > * [x] Third\n"
        violations = MODULE.find_violations(content)
        self.assertIn("contains 3 completed checklist item(s)", violations)

    def test_rejects_session_history_link(self) -> None:
        content = (
            "See [old work](progress/session-001-old-work.md) and "
            "[more work](progress/session-002-more-work.md).\n"
        )
        violations = MODULE.find_violations(content)
        self.assertIn("contains 2 session-history link(s)", violations)

    def test_rejects_archive_heading(self) -> None:
        content = "# Plan\n\n## Completed initiatives\n\n> ## Past results\n"
        violations = MODULE.find_violations(content)
        self.assertIn("contains 2 archive-style heading(s)", violations)

    def test_rejects_done_heading(self) -> None:
        violations = MODULE.find_violations("# Plan\n\n## Done\n")
        self.assertIn("contains 1 archive-style heading(s)", violations)

    def test_rejects_complete_status_heading(self) -> None:
        content = (
            "# Plan\n\n## Lua parity ✅ COMPLETE\n"
            "## Lua comparison failure ✅ **RESOLVED**\n"
            "### Flaky test ✅ **FIXED**\n"
            "### Version parsing ✅ **INCORPORATED**\n"
        )
        violations = MODULE.find_violations(content)
        self.assertIn("contains 4 archive-style heading(s)", violations)

    def test_rejects_past_work_and_closed_headings(self) -> None:
        content = (
            "# Plan\n\n## Past work\n\n> ## Closed initiatives\n\n## Previous work\n"
            "\n## **Past work**\n\n## *Past work*\n\n## _Past work_\n"
        )
        violations = MODULE.find_violations(content)
        self.assertIn("contains 6 archive-style heading(s)", violations)

    def test_accepts_active_headings_containing_done_or_closed(self) -> None:
        content = (
            "# Plan\n\n## Definition of Done\n\n## Closed-world source generation\n"
            "\n## Progress blockers\n\n## Results required\n"
            "\n## Archive file-format migration\n\n## *History API replacement*\n"
        )
        self.assertEqual([], MODULE.find_violations(content))

    def test_rejects_common_archive_headings(self) -> None:
        content = (
            "# Plan\n\n## Progress\n\n## Results\n\n## Done ✅\n"
            "\n## Completion Summary\n\n## Validation receipt\n"
            "\n## **Completed work**\n\n## Finished work\n"
        )
        violations = MODULE.find_violations(content)
        self.assertIn("contains 7 archive-style heading(s)", violations)

    def test_rejects_dated_result(self) -> None:
        content = "## Now\n\n> > 2026-08-26 — all tests passed.\n"
        violations = MODULE.find_violations(content)
        self.assertIn("contains 1 date-led line(s)", violations)

    def test_rejects_list_prefixed_dated_result(self) -> None:
        content = (
            "## Now\n\n- 2026-08-26: tests passed.\n"
            "2026-08-26: build succeeded.\n"
            "2026-08-26: after the fix, tests passed.\n"
        )
        violations = MODULE.find_violations(content)
        self.assertIn("contains 3 date-led line(s)", violations)

    def test_rejects_emphasized_completed_result(self) -> None:
        content = "## Now\n\n**Completed**: 2026-08-26\n"
        violations = MODULE.find_violations(content)
        self.assertIn("contains 1 dated completion result line(s)", violations)

    def test_rejects_progress_narrative(self) -> None:
        content = (
            "## Now\n\n"
            "**Progress**: Basic migration completed on 2026-08-26; tests passed.\n"
        )
        violations = MODULE.find_violations(content)
        self.assertIn("contains 1 progress narrative(s)", violations)

    def test_rejects_status_narratives(self) -> None:
        content = (
            "**Status**: ✅ Complete — shipped.\n"
            "**Current Status**: 2026-08-22 reported 0 mismatches.\n"
            "Status: ✅ Complete — shipped without emphasis.\n"
        )
        violations = MODULE.find_violations(content)
        self.assertIn("contains 3 progress narrative(s)", violations)

    def test_rejects_list_completion_status(self) -> None:
        content = (
            "- ✅ Complete: shipped the slice.\n"
            "Completed: 2026-08-26\n"
            "✅ **Extractor metadata hazard resolved** (2026-07-29): done.\n"
            "- ✅ **Phase 1: Metamethods** — Created constants.\n"
            "- Done — shipped the slice.\n"
        )
        violations = MODULE.find_violations(content)
        self.assertIn("contains 5 completion status line(s)", violations)

    def test_accepts_future_deadline_without_a_result(self) -> None:
        content = "## Next\n\n- Ship the selected milestone by 2026-09-01.\n"
        self.assertEqual([], MODULE.find_violations(content))

    def test_rejects_date_led_future_test_schedule_as_ambiguous(self) -> None:
        content = "## Next\n\n- 2026-09-01: run tests for the release.\n"
        self.assertIn("contains 1 date-led line(s)", MODULE.find_violations(content))

    def test_rejects_date_led_future_ci_gate_as_ambiguous(self) -> None:
        content = "## Next\n\n- 2026-09-01: ship once CI has passed.\n"
        self.assertIn("contains 1 date-led line(s)", MODULE.find_violations(content))

    def test_rejects_date_led_failed_test_diagnosis_as_ambiguous(self) -> None:
        content = "## Next\n\n- 2026-09-01: diagnose failed tests.\n"
        self.assertIn("contains 1 date-led line(s)", MODULE.find_violations(content))

    def test_rejects_date_led_future_result_verification_as_ambiguous(self) -> None:
        content = (
            "2026-09-01: verify the release build passed.\n"
            "2026-09-01: confirm CI is green before release.\n"
            "2026-09-01: ensure migration completed before release.\n"
            "2026-09-01: release once CI is green.\n"
        )
        self.assertIn("contains 4 date-led line(s)", MODULE.find_violations(content))


if __name__ == "__main__":
    unittest.main()
