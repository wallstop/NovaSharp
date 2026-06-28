#!/usr/bin/env python3
"""
Checks local tooling setup stays aligned across devcontainer, hooks, and CI.

The repository uses global.json for the .NET SDK and .config/dotnet-tools.json
for .NET CLI tools. This guard catches setup drift before it breaks fresh clones
or devcontainer rebuilds.
"""

from __future__ import annotations

import json
import re
import subprocess
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]
GLOBAL_JSON = REPO_ROOT / "global.json"
DOTNET_TOOLS_JSON = REPO_ROOT / ".config" / "dotnet-tools.json"
DEVCONTAINER_DOCKERFILE = REPO_ROOT / ".devcontainer" / "Dockerfile"
DEVCONTAINER_JSON = REPO_ROOT / ".devcontainer" / "devcontainer.json"


def repo_path(path: Path) -> str:
    return path.relative_to(REPO_ROOT).as_posix()


def read_text(path: Path) -> str:
    try:
        return path.read_text(encoding="utf-8")
    except OSError as exc:
        raise SystemExit(f"Unable to read {repo_path(path)}: {exc}") from exc


def load_json(path: Path) -> dict:
    try:
        return json.loads(read_text(path))
    except json.JSONDecodeError as exc:
        raise SystemExit(f"Unable to parse {repo_path(path)}: {exc}") from exc


def get_tracked_shell_entrypoints() -> list[Path]:
    try:
        result = subprocess.run(
            ["git", "ls-files", "*.sh", ".githooks/*"],
            cwd=REPO_ROOT,
            check=True,
            capture_output=True,
            text=True,
        )
    except subprocess.CalledProcessError as exc:
        print(f"git ls-files failed: {exc.stderr}", file=sys.stderr)
        raise SystemExit(1) from exc
    except FileNotFoundError as exc:
        raise SystemExit("git is required to run this check.") from exc

    paths: list[Path] = []
    for raw_path in result.stdout.splitlines():
        if not raw_path:
            continue

        path = REPO_ROOT / raw_path
        if path.is_file():
            paths.append(path)

    return sorted(paths)


def strip_shell_lines(path: Path) -> list[tuple[int, str]]:
    return [(line_number, line.strip()) for line_number, line in enumerate(read_text(path).splitlines(), start=1)]


def split_shell_functions(lines: list[tuple[int, str]]) -> tuple[list[tuple[int, str]], dict[str, list[tuple[int, str]]]]:
    top_level: list[tuple[int, str]] = []
    functions: dict[str, list[tuple[int, str]]] = {}
    current_function = ""
    current_body: list[tuple[int, str]] = []

    function_start_pattern = re.compile(r"^([A-Za-z_][A-Za-z0-9_]*)\s*\(\)\s*\{\s*$")

    for line_number, stripped in lines:
        if current_function:
            if stripped == "}":
                functions[current_function] = current_body
                current_function = ""
                current_body = []
            else:
                current_body.append((line_number, stripped))
            continue

        match = function_start_pattern.match(stripped)
        if match:
            current_function = match.group(1)
            current_body = []
            continue

        top_level.append((line_number, stripped))

    if current_function:
        functions[current_function] = current_body

    return top_level, functions


def is_diagnostic_shell_line(stripped: str) -> bool:
    return re.match(r"^(?:echo|printf|log|log_error|log_success)\b", stripped) is not None and "$(" not in stripped


def required_sdk_feature_band() -> str:
    sdk_version = load_json(GLOBAL_JSON).get("sdk", {}).get("version", "")
    match = re.match(r"^(\d+\.\d+)\.", sdk_version)
    if not match:
        raise SystemExit("global.json must pin sdk.version to a major.minor.patch value.")
    return match.group(1)


def local_tool_commands() -> set[str]:
    tools_json = load_json(DOTNET_TOOLS_JSON)
    commands: set[str] = set()
    for tool in tools_json.get("tools", {}).values():
        for command in tool.get("commands", []):
            commands.add(command)
    return commands


def dockerfile_instructions(text: str) -> list[str]:
    instructions: list[str] = []
    current = ""

    for raw_line in text.splitlines():
        stripped = raw_line.strip()
        if not stripped or stripped.startswith("#"):
            continue

        if current:
            current += " " + stripped
        else:
            current = stripped

        if current.endswith("\\"):
            current = current[:-1].rstrip()
            continue

        instructions.append(current)
        current = ""

    if current:
        instructions.append(current)

    return instructions


def dotnet_sdk_packages_from_install_instructions(dockerfile: str) -> set[str]:
    installed_sdks: set[str] = set()
    sdk_args: dict[str, str] = {}

    for instruction in dockerfile_instructions(dockerfile):
        arg_match = re.match(r"ARG\s+([A-Za-z_][A-Za-z0-9_]*)=(\d+\.\d+)\s*$", instruction)
        if arg_match:
            sdk_args[arg_match.group(1)] = arg_match.group(2)
            continue

        if "apt-get install" not in instruction or "dotnet-sdk-" not in instruction:
            continue

        for literal_sdk in re.findall(r"\bdotnet-sdk-(\d+\.\d+)\b", instruction):
            installed_sdks.add(literal_sdk)

        for arg_name in re.findall(r"dotnet-sdk-\$\{([A-Za-z_][A-Za-z0-9_]*)\}", instruction):
            if arg_name in sdk_args:
                installed_sdks.add(sdk_args[arg_name])

    return installed_sdks


def check_devcontainer_sdk(violations: list[str]) -> None:
    required_sdk = required_sdk_feature_band()
    dockerfile = read_text(DEVCONTAINER_DOCKERFILE)
    installed_sdks = dotnet_sdk_packages_from_install_instructions(dockerfile)

    if required_sdk not in installed_sdks:
        installed_display = ", ".join(sorted(installed_sdks)) or "none"
        violations.append(
            f"{repo_path(DEVCONTAINER_DOCKERFILE)} installs dotnet SDK package(s) "
            f"{installed_display}, but global.json requires dotnet-sdk-{required_sdk}."
        )


def check_devcontainer_uses_local_tools(violations: list[str]) -> None:
    dockerfile = read_text(DEVCONTAINER_DOCKERFILE)
    devcontainer = read_text(DEVCONTAINER_JSON)

    if re.search(r"\bdotnet\s+tool\s+(?:install|update)\s+--global\b", dockerfile):
        violations.append(
            f"{repo_path(DEVCONTAINER_DOCKERFILE)} installs global dotnet tools. "
            "Use the repository local tool manifest with `dotnet tool restore` and `dotnet tool run`."
        )

    if re.search(r"target=/[^,\"]*\.dotnet/tools\b", devcontainer):
        violations.append(
            f"{repo_path(DEVCONTAINER_JSON)} mounts over a .dotnet/tools directory. "
            "That can hide image-baked shims; keep dotnet tools manifest-local instead."
        )


def check_shell_dotnet_tool_restore(violations: list[str]) -> None:
    tool_commands = "|".join(re.escape(command) for command in sorted(local_tool_commands()))
    if not tool_commands:
        return

    dotnet_tool_run_pattern = re.compile(rf"\bdotnet\s+tool\s+run\s+(?:{tool_commands})\b")
    dotnet_tool_restore_pattern = re.compile(r"\bdotnet\s+tool\s+restore\b")

    def is_restoring_function_call(stripped: str, restoring_functions: set[str]) -> bool:
        for function_name in restoring_functions:
            if re.match(rf"^(?:if\s+!\s+)?{re.escape(function_name)}(?:\s|;|$)", stripped):
                return True
        return False

    def line_has_restore_before_run(stripped: str, run_start: int, restoring_functions: set[str]) -> bool:
        if any(match.start() < run_start for match in dotnet_tool_restore_pattern.finditer(stripped)):
            return True

        for function_name in restoring_functions:
            call_match = re.search(rf"\b{re.escape(function_name)}\b", stripped)
            if call_match and call_match.start() < run_start:
                return True

        return False

    def check_ordered_lines(
        path: Path,
        lines: list[tuple[int, str]],
        restoring_functions: set[str],
        context: str,
    ) -> None:
        saw_restore = False
        for line_number, stripped in lines:
            if not stripped or stripped.startswith("#"):
                continue

            if is_diagnostic_shell_line(stripped):
                continue

            run_matches = list(dotnet_tool_run_pattern.finditer(stripped))
            if run_matches:
                for run_match in run_matches:
                    if not saw_restore and not line_has_restore_before_run(
                        stripped,
                        run_match.start(),
                        restoring_functions,
                    ):
                        violations.append(
                            f"{repo_path(path)}:{line_number} runs a local dotnet tool before restoring "
                            f"the tool manifest{context}. Call `dotnet tool restore` first or delegate to "
                            "a shared CI helper that does."
                        )
                        break

            if dotnet_tool_restore_pattern.search(stripped) or is_restoring_function_call(
                stripped,
                restoring_functions,
            ):
                saw_restore = True

    for path in get_tracked_shell_entrypoints():
        top_level, functions = split_shell_functions(strip_shell_lines(path))
        restoring_functions = {
            function_name
            for function_name, body_lines in functions.items()
            if any(
                dotnet_tool_restore_pattern.search(stripped)
                for _, stripped in body_lines
                if stripped and not stripped.startswith("#") and not is_diagnostic_shell_line(stripped)
            )
        }

        check_ordered_lines(path, top_level, restoring_functions, "")
        for function_name, body_lines in functions.items():
            check_ordered_lines(path, body_lines, restoring_functions, f" inside `{function_name}`")


def check_unpinned_template_installs(violations: list[str]) -> None:
    install_pattern = re.compile(r"\bdotnet\s+new\s+install\s+([^\s;&|]+)")
    for path in get_tracked_shell_entrypoints():
        if not repo_path(path).startswith(".devcontainer/"):
            continue

        for line_number, line in enumerate(read_text(path).splitlines(), start=1):
            stripped = line.strip()
            if not stripped or stripped.startswith("#"):
                continue

            match = install_pattern.search(stripped)
            if match and "::" not in match.group(1) and "@" not in match.group(1):
                violations.append(
                    f"{repo_path(path)}:{line_number} installs a dotnet template package without a "
                    "version. Pin the package version or remove the devcontainer setup step."
                )


def main() -> int:
    violations: list[str] = []

    check_devcontainer_sdk(violations)
    check_devcontainer_uses_local_tools(violations)
    check_shell_dotnet_tool_restore(violations)
    check_unpinned_template_installs(violations)

    if violations:
        print("Tooling setup consistency violations detected:\n", file=sys.stderr)
        for violation in violations:
            print(f"  - {violation}", file=sys.stderr)
        print(
            "\nKeep the devcontainer, hooks, and CI aligned with global.json and "
            ".config/dotnet-tools.json.",
            file=sys.stderr,
        )
        return 1

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
