#!/usr/bin/env python3
"""Tests for scripts/benchmarks/run-lua-cli-context.py."""

from __future__ import annotations

import importlib.util
import os
import sys
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
SCRIPT = ROOT / "scripts" / "benchmarks" / "run-lua-cli-context.py"


def load_module():
    spec = importlib.util.spec_from_file_location("run_lua_cli_context", SCRIPT)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"Unable to load {SCRIPT}")

    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class RunLuaCliContextTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.module = load_module()

    def test_resolve_lua_command_returns_none_for_missing_command(self) -> None:
        result = self.module.resolve_lua_command("definitely-not-a-real-lua-command")

        self.assertIsNone(result)

    def test_resolve_lua_command_accepts_executable_path(self) -> None:
        result = self.module.resolve_lua_command(sys.executable)

        self.assertEqual(os.path.expanduser(sys.executable), result)

    def test_resolve_lua_command_rejects_missing_path(self) -> None:
        result = self.module.resolve_lua_command("./definitely-not-real-lua")

        self.assertIsNone(result)

    def test_benchmark_record_marks_lua_cli_wall_time_report_only(self) -> None:
        result = self.module.benchmark_record(
            "NumericLoops",
            [100, 200, 300],
            "/usr/bin/lua5.4",
            "Lua 5.4.6",
        )

        self.assertEqual("Lua CLI wall-time", result["RuntimeDisplayName"])
        self.assertEqual("LuaCliWallTime", result["RuntimeKind"])
        self.assertFalse(result["ShowDeltaPercent"])


if __name__ == "__main__":
    unittest.main()
