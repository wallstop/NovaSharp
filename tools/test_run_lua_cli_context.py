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


if __name__ == "__main__":
    unittest.main()
