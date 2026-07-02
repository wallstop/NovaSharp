#!/usr/bin/env python3
"""Tests for scripts/benchmarks/run-lua-cli-context.py."""

from __future__ import annotations

import importlib.util
import os
import shutil
import sys
import tempfile
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

        self.assertEqual(str(Path(os.path.expanduser(sys.executable)).resolve()), result)

    def test_resolve_lua_command_resolves_relative_executable_path(self) -> None:
        artifacts_root = ROOT / "artifacts"
        artifacts_root.mkdir(exist_ok=True)
        with tempfile.TemporaryDirectory(dir=artifacts_root) as temp_dir:
            temp_path = Path(temp_dir) / "test-python"
            shutil.copy2(sys.executable, temp_path)
            temp_path.chmod(0o755)
            relative_path = os.path.relpath(temp_path, Path.cwd())

            result = self.module.resolve_lua_command(relative_path)

            self.assertEqual(str(temp_path.resolve()), result)

    def test_resolve_lua_command_resolves_relative_path_hit(self) -> None:
        artifacts_root = ROOT / "artifacts"
        artifacts_root.mkdir(exist_ok=True)
        original_cwd = Path.cwd()
        original_path = os.environ.get("PATH")
        with tempfile.TemporaryDirectory(dir=artifacts_root) as temp_dir_text:
            temp_dir = Path(temp_dir_text)
            relbin = temp_dir / "relbin"
            relbin.mkdir()
            fake_lua = relbin / "fake-lua"
            shutil.copy2(sys.executable, fake_lua)
            fake_lua.chmod(0o755)

            try:
                os.chdir(temp_dir)
                os.environ["PATH"] = "relbin"

                result = self.module.resolve_lua_command("fake-lua")
            finally:
                os.chdir(original_cwd)
                if original_path is None:
                    os.environ.pop("PATH", None)
                else:
                    os.environ["PATH"] = original_path

            self.assertEqual(str(fake_lua.resolve()), result)

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
