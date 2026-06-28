#!/usr/bin/env python3
"""Focused tests for scripts/tests/run-lua-fixtures-fast.sh."""

from __future__ import annotations

import os
import shutil
import stat
import subprocess
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
SCRIPT = ROOT / "scripts" / "tests" / "run-lua-fixtures-fast.sh"


class RunLuaFixturesFastTests(unittest.TestCase):
    def setUp(self) -> None:
        self.work_dir = ROOT / "artifacts" / "test-run-lua-fixtures-fast"
        if self.work_dir.exists():
            shutil.rmtree(self.work_dir)
        self.fixtures_dir = self.work_dir / "LuaFixtures"
        self.output_dir = self.work_dir / "out"
        self.bin_dir = self.work_dir / "bin"
        self.fixtures_dir.mkdir(parents=True)
        self.bin_dir.mkdir(parents=True)

    def tearDown(self) -> None:
        if self.work_dir.exists():
            shutil.rmtree(self.work_dir)

    def write_fake_lua(self) -> Path:
        fake_lua = self.bin_dir / "fake-lua"
        fake_lua.write_text(
            "#!/usr/bin/env bash\n"
            "if [ ! -f \"$1\" ]; then\n"
            "  printf 'missing fixture: %s\\n' \"$1\" >&2\n"
            "  exit 91\n"
            "fi\n"
            "printf 'fixture=%s\\n' \"$1\"\n",
            encoding="utf-8",
        )
        fake_lua.chmod(fake_lua.stat().st_mode | stat.S_IXUSR)
        return fake_lua

    def write_fixture(self, relative_path: str, source: str = "print('ok')\n") -> Path:
        fixture = self.fixtures_dir / relative_path
        fixture.parent.mkdir(parents=True)
        fixture.write_text(source, encoding="utf-8")
        return fixture

    def run_script(self, *args: str) -> subprocess.CompletedProcess[str]:
        return subprocess.run(
            ["bash", str(SCRIPT.relative_to(ROOT)), *args],
            cwd=ROOT,
            text=True,
            capture_output=True,
            check=False,
        )

    def assert_success(self, result: subprocess.CompletedProcess[str]) -> None:
        self.assertEqual(
            0,
            result.returncode,
            msg=f"stdout:\n{result.stdout}\nstderr:\n{result.stderr}",
        )

    def test_reference_runner_writes_relative_output_paths(self) -> None:
        fixture = self.write_fixture("StringLibTUnitTests/FindPlain.lua")
        fake_lua = self.write_fake_lua()

        result = self.run_script(
            "--fixtures-dir",
            str(self.fixtures_dir.relative_to(ROOT)),
            "--output-dir",
            str(self.output_dir.relative_to(ROOT)),
            "--lua-version",
            "5.4",
            "--lua-cmd",
            str(fake_lua.relative_to(ROOT)),
            "--jobs",
            "1",
            "--skip-novasharp",
        )
        self.assert_success(result)

        output_file = (
            self.output_dir
            / "StringLibTUnitTests"
            / "FindPlain.lua5.4.out"
        )
        self.assertTrue(output_file.exists())
        self.assertIn("FindPlain.lua", output_file.read_text(encoding="utf-8"))

        expected_full_path = fixture.relative_to(ROOT).as_posix().encode("utf-8")
        expected_relative_path = b"StringLibTUnitTests/FindPlain.lua"

        file_list = (self.output_dir / "filelist.txt").read_bytes()
        self.assertEqual(expected_full_path + b"\n", file_list)

        reference_file_list = self.output_dir / "filelist-reference.bin"
        reference_records = reference_file_list.read_bytes().split(b"\0")
        self.assertEqual(
            [expected_full_path, expected_relative_path, b""],
            reference_records,
        )

    def test_batch_runner_consumes_lf_filelist_paths(self) -> None:
        fixture = self.write_fixture(
            "BatchRunnerTUnitTests/PrintOk.lua",
            "print('batch ok')\n",
        )

        result = self.run_script(
            "--fixtures-dir",
            str(self.fixtures_dir.relative_to(ROOT)),
            "--output-dir",
            str(self.output_dir.relative_to(ROOT)),
            "--lua-version",
            "5.4",
            "--jobs",
            "1",
            "--skip-lua",
        )
        self.assert_success(result)

        expected_full_path = fixture.relative_to(ROOT).as_posix().encode("utf-8")
        file_list = (self.output_dir / "filelist.txt").read_bytes()
        self.assertEqual(expected_full_path + b"\n", file_list)

        output_file = (
            self.output_dir
            / "BatchRunnerTUnitTests"
            / "PrintOk.nova.out"
        )
        rc_file = self.output_dir / "BatchRunnerTUnitTests" / "PrintOk.nova.rc"
        self.assertEqual("0", rc_file.read_text(encoding="utf-8").strip())
        self.assertEqual("batch ok", output_file.read_text(encoding="utf-8").strip())


if __name__ == "__main__":
    unittest.main()
