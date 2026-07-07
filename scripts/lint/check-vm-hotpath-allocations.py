#!/usr/bin/env python3
"""Guard VM opcode and Lua-call hot paths against new allocation traps."""

from __future__ import annotations

import argparse
import os
import re
import sys
from dataclasses import dataclass
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]


@dataclass(frozen=True)
class Rule:
    rule_id: str
    pattern: re.Pattern[str]
    description: str


@dataclass(frozen=True)
class AllowedMatch:
    rule_id: str
    path: str
    symbol: str
    pattern: re.Pattern[str]
    reason: str


@dataclass(frozen=True)
class Finding:
    path: str
    line_number: int
    symbol: str
    rule: Rule
    line: str


TARGET_TYPED_RETURN_NEW_PATTERN = re.compile(r"(?:\breturn|=>)\s+new\s*\(")
TARGET_TYPED_RETURN_ARRAY_NEW_PATTERN = re.compile(r"(?:\breturn|=>)\s+new\s*\[\s*\]")
CALLBACK_INVOKE_START_PATTERN = re.compile(r"\w+(?:\.\w+)*\.Invoke\s*\(\s*$")
CALLBACK_INVOKE_OPEN_PATTERN = re.compile(r"\w+(?:\.\w+)*\.Invoke\s*\(")
CALLBACK_CONTEXT_NAMED_ARGUMENT_PATTERN = re.compile(
    r"\b(?:context|executionContext)\s*:\s*new\s*\("
)
IMPLICIT_ARRAY_NEW_PATTERN = re.compile(r"\bnew\s*\[\s*\]")
QUALIFIED_DYNVALUE = r"(?:(?:global::)?(?:\w+\.)*)?DynValue"
QUALIFIED_LIST_DYNVALUE = (
    rf"(?:(?:global::)?System\.Collections\.Generic\.)?List\s*<\s*{QUALIFIED_DYNVALUE}\s*>"
)
DYNVALUE_MEMBER_PATTERN = re.compile(rf"\b{QUALIFIED_DYNVALUE}\.")
TARGET_TYPED_DYNVALUE_PUSH_PATTERN = re.compile(r"\b\w+(?:\.\w+)*\.Push\s*\(\s*new\s*\(")
TARGET_TYPED_NEW_EXPRESSION_PATTERN = re.compile(
    r"^\s*(?:(?:context|executionContext)\s*:\s*)?new\s*\("
)
RAW_STRING_PREFIX_PATTERN = re.compile(r"\$*\"{3,}")
CONTEXT_BEFORE_LINES = 5
CONTEXT_AFTER_LINES = 7

RULES = [
    Rule(
        "new-dynvalue",
        re.compile(
            rf"\b(?:"
            rf"new\s+{QUALIFIED_DYNVALUE}\s*\(|"
            rf"{QUALIFIED_DYNVALUE}\s+\w+\s*=\s*new\s*\()"
        ),
        "new DynValue in a VM opcode/call path allocates a Lua scalar wrapper.",
    ),
    Rule(
        "dynvalue-new-number",
        re.compile(r"\bDynValue\.NewNumber\s*\("),
        "DynValue.NewNumber in a VM opcode/call path allocates a numeric wrapper.",
    ),
    Rule(
        "dynvalue-new-integer",
        re.compile(r"\bDynValue\.NewInteger\s*\("),
        "DynValue.NewInteger in a VM opcode/call path allocates an integer wrapper.",
    ),
    Rule(
        "new-list-dynvalue",
        re.compile(
            rf"\b(?:"
            rf"new\s+{QUALIFIED_LIST_DYNVALUE}\s*\(|"
            rf"{QUALIFIED_LIST_DYNVALUE}\s+\w+\s*=\s*new\s*\()"
        ),
        "new List<DynValue> in a VM opcode/call path allocates a collection.",
    ),
    Rule(
        "new-dynvalue-array",
        re.compile(
            rf"\b(?:new\s+{QUALIFIED_DYNVALUE}\s*\[|"
            rf"{QUALIFIED_DYNVALUE}\s*\[\s*\]\s+\w+\s*=\s*new\s*\[\s*\]|"
            rf"new\s*\[\s*\]\s*\{{[^;\n]*\b{QUALIFIED_DYNVALUE}\.)"
        ),
        "new DynValue[] in a VM opcode/call path allocates an argument or return buffer.",
    ),
    Rule(
        "new-script-execution-context",
        re.compile(
            r"\b(?:new\s+(?:[\w.]+\.)?ScriptExecutionContext\s*\(|"
            r"(?:[\w.]+\.)?ScriptExecutionContext\s+\w+\s*=\s*new\s*\(|"
            r"\w+(?:\.\w+)*\.Invoke\s*\(\s*(?:\w+\s*:\s*)?new\s*\(|"
            r"\w+(?:\.\w+)*\.Invoke\s*\([^;\n]*\b(?:context|executionContext)\s*:\s*new\s*\()"
        ),
        "new ScriptExecutionContext in a VM opcode/call path allocates callback context.",
    ),
]

RULES_BY_ID = {rule.rule_id: rule for rule in RULES}

TARGET_FILE_GLOBS = [
    "src/runtime/WallstopStudios.NovaSharp.Interpreter/DataTypes/CallbackFunction.cs",
    "src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/ScriptExecutionContext.cs",
    "src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/VM/**/*.cs",
]

ALLOWLIST = [
    AllowedMatch(
        "new-dynvalue-array",
        "src/runtime/WallstopStudios.NovaSharp.Interpreter/DataTypes/CallbackFunction.cs",
        "InvokeLegacySpan",
        re.compile(r"new\s+DynValue\s*\[\s*args\.Length\s*\]"),
        "Legacy callback fallback copies escaped arguments until A5 span callbacks replace this path.",
    ),
    AllowedMatch(
        "new-dynvalue-array",
        "src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/ScriptExecutionContext.cs",
        "CreateCallMetamethodArguments",
        re.compile(r"new\s+DynValue\s*\[\s*args\.Length\s*\+\s*1\s*\]"),
        "Callable metamethod slow path materializes adjusted arguments until A5 stack windows land.",
    ),
    AllowedMatch(
        "new-dynvalue-array",
        "src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/VM/Processor/Processor.cs",
        "ToTuple",
        re.compile(r"new\s+DynValue\s*\[\s*_count\s*\]"),
        "Escaped tuple materialization remains allowed for coroutine resume and vararg tuple cases.",
    ),
    AllowedMatch(
        "new-dynvalue-array",
        "src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/VM/Processor/Processor.cs",
        "ToTuple",
        re.compile(r"new\s+DynValue\s*\[\s*_count\s*\]"),
        "Escaped tuple materialization remains allowed for coroutine resume and vararg tuple cases.",
    ),
    AllowedMatch(
        "new-dynvalue-array",
        "src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/VM/Processor/Processor.cs",
        "CreateTupleFromSpan",
        re.compile(r"new\s+DynValue\s*\[\s*values\.Length\s*\]"),
        "Escaped tuple materialization remains allowed for caller-owned spans with more than five values.",
    ),
    AllowedMatch(
        "new-dynvalue-array",
        "src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/VM/Processor/ProcessorInstructionLoop.cs",
        "ExecRet",
        re.compile(r"new\s+DynValue\s*\[\]\s*\{\s*continuationArgument\s*\}"),
        "Continuation resume materializes a one-value escaped argument until A5 removes this allocation.",
    ),
    AllowedMatch(
        "new-dynvalue-array",
        "src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/VM/Processor/ProcessorUtilityFunctions.cs",
        "InternalAdjustTuple",
        re.compile(r"new\s+DynValue\s*\[\s*baseLen\s*\]"),
        "Tuple expansion returns escaped multi-result arrays by design until a return-buffer API lands.",
    ),
    AllowedMatch(
        "new-dynvalue-array",
        "src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/VM/Processor/ProcessorUtilityFunctions.cs",
        "InternalAdjustTuple",
        re.compile(r"new\s+DynValue\s*\[\s*values\.Count\s*\]"),
        "Tuple expansion returns escaped multi-result arrays by design until a return-buffer API lands.",
    ),
    AllowedMatch(
        "new-script-execution-context",
        "src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/VM/Processor/ProcessorInstructionLoop.cs",
        "ProcessingLoop",
        re.compile(r"new\s+ScriptExecutionContext\s*\("),
        "xpcall error-handler decoration allocates callback context on a slow path.",
    ),
    AllowedMatch(
        "new-script-execution-context",
        "src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/VM/Processor/ProcessorInstructionLoop.cs",
        "PerformMessageDecorationBeforeUnwind",
        re.compile(r"ScriptExecutionContext\s+ctx\s*=\s*new\s*\("),
        "xpcall message-handler decoration allocates callback context on a slow path.",
    ),
    AllowedMatch(
        "new-script-execution-context",
        "src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/VM/Processor/ProcessorInstructionLoop.cs",
        "CloseValue",
        re.compile(r"ScriptExecutionContext\s+context\s*=\s*new\s*\("),
        "Lua 5.4 close-metamethod CLR callback path allocates context until A5 span callbacks remove it.",
    ),
    AllowedMatch(
        "new-list-dynvalue",
        "src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/VM/Processor/ProcessorInstructionLoop.cs",
        "CreateArgsListForFunctionCall",
        re.compile(r"List\s*<\s*DynValue\s*>\s+values\s*=\s*new\s*\(\s*numargs\s*\)"),
        "Tuple expansion currently materializes an escaped argument list until A5 stack windows land.",
    ),
    AllowedMatch(
        "new-list-dynvalue",
        "src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/VM/Processor/ProcessorInstructionLoop.cs",
        "CreateArgsListForFunctionCall",
        re.compile(
            r"List\s*<\s*DynValue\s*>\s+expandedValues\s*=\s*new\s*\(\s*numargs\s*-\s*1\s*\+\s*tupleLength\s*\)"
        ),
        "Tuple expansion currently materializes an escaped argument list until A5 stack windows land.",
    ),
    AllowedMatch(
        "new-script-execution-context",
        "src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/VM/Processor/ProcessorInstructionLoop.cs",
        "InternalExecCall",
        re.compile(r"ScriptExecutionContext\s+context\s*=\s*new\s*\("),
        "CLR callback invocation allocates context until A5 span-based callback APIs remove it.",
    ),
    AllowedMatch(
        "new-script-execution-context",
        "src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/VM/Processor/ProcessorInstructionLoop.cs",
        "ExecRet",
        re.compile(r"ScriptExecutionContext\s+executionContext\s*=\s*new\s*\("),
        "Continuation invocation allocates context until A5 removes hot continuation context materialization.",
    ),
    AllowedMatch(
        "new-script-execution-context",
        "src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/VM/Processor/ProcessorDebugger.cs",
        "RefreshDebugger",
        re.compile(r"ScriptExecutionContext\s+context\s*=\s*new\s*\("),
        "Debugger refresh is an instrumented non-hot path.",
    ),
]

NUMBER_FACTORY_ALLOWLIST_CONTEXTS = {
    "ExecToNum": [r"DynValue\.NewNumber\s*\(\s*v\.Value\s*\)"],
    "ExecIncr": [r"DynValue\.NewNumber\s*\(\s*top\.LuaNumber\s*\)"],
    "ExecAdd": [
        r"DynValue\.NewNumber\s*\(\s*LuaNumber\.Add\s*\(\s*lnFast\s*,\s*rnFast\s*\)\s*\)",
        r"DynValue\.NewNumber\s*\(\s*LuaNumber\.Add\s*\(\s*ln\.Value\s*,\s*rn\.Value\s*\)\s*\)",
    ],
    "ExecSub": [
        r"DynValue\.NewNumber\s*\(\s*LuaNumber\.Subtract\s*\(\s*lnFast\s*,\s*rnFast\s*\)\s*\)",
        r"DynValue\.NewNumber\s*\(\s*LuaNumber\.Subtract\s*\(\s*ln\.Value\s*,\s*rn\.Value\s*\)\s*\)",
    ],
    "ExecMul": [
        r"DynValue\.NewNumber\s*\(\s*LuaNumber\.Multiply\s*\(\s*lnFast\s*,\s*rnFast\s*\)\s*\)",
        r"DynValue\.NewNumber\s*\(\s*LuaNumber\.Multiply\s*\(\s*ln\.Value\s*,\s*rn\.Value\s*\)\s*\)",
    ],
    "ExecMod": [
        r"DynValue\.NewNumber\s*\(\s*LuaNumber\.Modulo\s*\(\s*lnFast\s*,\s*rnFast\s*,\s*_script\.Options\.CompatibilityVersion\s*\)",
        r"DynValue\.NewNumber\s*\(\s*LuaNumber\.Modulo\s*\(\s*ln\.Value\s*,\s*rn\.Value\s*,\s*_script\.Options\.CompatibilityVersion\s*\)",
    ],
    "ExecDiv": [
        r"DynValue\.NewNumber\s*\(\s*LuaNumber\.Divide\s*\(\s*lnFast\s*,\s*rnFast\s*\)\s*\)",
        r"DynValue\.NewNumber\s*\(\s*LuaNumber\.Divide\s*\(\s*ln\.Value\s*,\s*rn\.Value\s*\)\s*\)",
    ],
    "ExecFloorDiv": [
        r"DynValue\.NewNumber\s*\(\s*LuaNumber\.FloorDivide\s*\(\s*lnFast\s*,\s*rnFast\s*\)\s*\)",
        r"DynValue\.NewNumber\s*\(\s*LuaNumber\.FloorDivide\s*\(\s*ln\.Value\s*,\s*rn\.Value\s*\)\s*\)",
    ],
    "ExecPower": [
        r"DynValue\.NewNumber\s*\(\s*LuaNumber\.Power\s*\(\s*lnFast\s*,\s*rnFast\s*\)\s*\)",
        r"DynValue\.NewNumber\s*\(\s*LuaNumber\.Power\s*\(\s*ln\.Value\s*,\s*rn\.Value\s*\)\s*\)",
    ],
    "ExecNeg": [
        r"if\s*\(\s*r\.Type\s*==\s*DataType\.Number\s*\).*DynValue\.NewNumber\s*\(\s*result\s*\)",
        r"if\s*\(\s*rn\.HasValue\s*\).*DynValue\.NewNumber\s*\(\s*result\s*\)",
    ],
}

for symbol, contexts in NUMBER_FACTORY_ALLOWLIST_CONTEXTS.items():
    for context in contexts:
        ALLOWLIST.append(
            AllowedMatch(
                "dynvalue-new-number",
                "src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/VM/Processor/ProcessorInstructionLoop.cs",
                symbol,
                re.compile(context),
                "Current A1 red allocation: numeric opcode paths must write inline LuaValue slots after the value-struct conversion.",
            )
        )

INTEGER_FACTORY_ALLOWLIST_CONTEXTS = {
    "ExecBitNot": r"DynValue\.NewInteger\s*\(\s*~operand\s*\)",
    "ExecBitwiseBinary": r"DynValue\.NewInteger\s*\(\s*operation\s*\(\s*left\s*,\s*right\s*\)\s*\)",
}

for symbol, context in INTEGER_FACTORY_ALLOWLIST_CONTEXTS.items():
    ALLOWLIST.append(
        AllowedMatch(
            "dynvalue-new-integer",
            "src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/VM/Processor/ProcessorInstructionLoop.cs",
            symbol,
            re.compile(context),
            "Current A1 red allocation: integer opcode paths must write inline LuaValue slots after the value-struct conversion.",
        )
    )

METHOD_PATTERN = re.compile(
    r"^\s*(?:(?:public|private|internal|protected)\s+)+"
    r"(?:(?:static|readonly|unsafe|sealed|override|virtual|async|extern|partial|new)\s+)*"
    r"(?P<return_type>[\w<>\[\],.?:]+(?:\s+[\w<>\[\],.?:]+)*)\s+"
    r"(?P<name>\w+)\s*\("
)


def is_ci() -> bool:
    return os.environ.get("GITHUB_ACTIONS") == "true" or os.environ.get("CI") == "true"


def strip_comments_and_strings(
    line: str,
    inside_block_comment: bool,
    raw_string_quote_count: int,
) -> tuple[str, bool, int]:
    output: list[str] = []
    index = 0
    while index < len(line):
        if raw_string_quote_count > 0:
            delimiter = '"' * raw_string_quote_count
            end_index = line.find(delimiter, index)
            if end_index < 0:
                return "".join(output), inside_block_comment, raw_string_quote_count
            output.append('""')
            index = end_index + raw_string_quote_count
            raw_string_quote_count = 0
            continue

        if inside_block_comment:
            end_index = line.find("*/", index)
            if end_index < 0:
                return "".join(output), True, raw_string_quote_count
            index = end_index + 2
            inside_block_comment = False
            continue

        char = line[index]
        next_char = line[index + 1] if index + 1 < len(line) else ""
        raw_string_match = RAW_STRING_PREFIX_PATTERN.match(line, index)
        if raw_string_match is not None:
            delimiter_start = raw_string_match.group(0).find('"')
            quote_count = len(raw_string_match.group(0)) - delimiter_start
            delimiter = '"' * quote_count
            content_start = raw_string_match.end()
            end_index = line.find(delimiter, content_start)
            output.append('""')
            if end_index < 0:
                return "".join(output), inside_block_comment, quote_count
            index = end_index + quote_count
            continue

        if char == "/" and next_char == "/":
            break
        if char == "/" and next_char == "*":
            inside_block_comment = True
            index += 2
            continue
        if char in ("'", '"'):
            index = skip_csharp_string_or_char(line, index)
            output.append('""' if char == '"' else "''")
            continue

        output.append(char)
        index += 1

    return "".join(output), inside_block_comment, raw_string_quote_count


def skip_csharp_string_or_char(line: str, index: int) -> int:
    quote = line[index]
    index += 1
    while index < len(line):
        char = line[index]
        if char == "\\" and quote != "@":
            index += 2
            continue
        if char == quote:
            return index + 1
        index += 1
    return index


def normalize_type_name(type_name: str) -> str:
    return "".join(type_name.split()).rstrip("?").replace("global::", "")


def type_matches(type_name: str, expected_name: str) -> bool:
    normalized = normalize_type_name(type_name)
    return normalized == expected_name or normalized.endswith(f".{expected_name}")


def type_is_list_dynvalue(type_name: str) -> bool:
    normalized = normalize_type_name(type_name)
    return (
        re.fullmatch(
            r"(?:[\w.]+\.)?List<(?:(?:[\w]+\.)*)?DynValue>",
            normalized,
        )
        is not None
    )


def normalize_context(lines: list[str], line_index: int) -> str:
    start = max(0, line_index - CONTEXT_BEFORE_LINES)
    end = min(len(lines), line_index + CONTEXT_AFTER_LINES + 1)
    return " ".join(" ".join(line.strip().split()) for line in lines[start:end])


def matching_rules(
    code: str,
    current_return_type: str | None,
    pending_callback_invoke_context: bool = False,
    inside_callback_invoke: bool = False,
    pending_implicit_array_initializer: bool = False,
) -> list[Rule]:
    rules_by_id = {rule.rule_id: rule for rule in RULES if rule.pattern.search(code)}

    if TARGET_TYPED_RETURN_NEW_PATTERN.search(code) and current_return_type is not None:
        if type_matches(current_return_type, "DynValue"):
            rules_by_id["new-dynvalue"] = RULES_BY_ID["new-dynvalue"]
        elif type_matches(current_return_type, "ScriptExecutionContext"):
            rules_by_id["new-script-execution-context"] = RULES_BY_ID[
                "new-script-execution-context"
            ]
        elif type_is_list_dynvalue(current_return_type):
            rules_by_id["new-list-dynvalue"] = RULES_BY_ID["new-list-dynvalue"]

    if TARGET_TYPED_RETURN_ARRAY_NEW_PATTERN.search(code) and current_return_type is not None:
        if type_matches(current_return_type, "DynValue[]"):
            rules_by_id["new-dynvalue-array"] = RULES_BY_ID["new-dynvalue-array"]

    if TARGET_TYPED_DYNVALUE_PUSH_PATTERN.search(code):
        rules_by_id["new-dynvalue"] = RULES_BY_ID["new-dynvalue"]

    if pending_callback_invoke_context and TARGET_TYPED_NEW_EXPRESSION_PATTERN.match(code):
        rules_by_id["new-script-execution-context"] = RULES_BY_ID[
            "new-script-execution-context"
        ]

    if inside_callback_invoke and CALLBACK_CONTEXT_NAMED_ARGUMENT_PATTERN.search(code):
        rules_by_id["new-script-execution-context"] = RULES_BY_ID[
            "new-script-execution-context"
        ]

    if pending_implicit_array_initializer and DYNVALUE_MEMBER_PATTERN.search(code):
        rules_by_id["new-dynvalue-array"] = RULES_BY_ID["new-dynvalue-array"]

    return list(rules_by_id.values())


def update_callback_invoke_depth(current_depth: int, code: str) -> int:
    if current_depth > 0:
        return max(0, current_depth + code.count("(") - code.count(")"))

    invoke_match = CALLBACK_INVOKE_OPEN_PATTERN.search(code)
    if invoke_match is None:
        return 0

    invoke_argument_text = code[invoke_match.end() - 1 :]
    return max(0, invoke_argument_text.count("(") - invoke_argument_text.count(")"))


def update_pending_implicit_array_initializer(current_pending: bool, code: str) -> bool:
    if current_pending:
        return ";" not in code

    if not IMPLICIT_ARRAY_NEW_PATTERN.search(code):
        return False

    return DYNVALUE_MEMBER_PATTERN.search(code) is None and ";" not in code


def run_self_tests() -> None:
    cases = [
        ("DynValue value = new();", None, {"new-dynvalue"}),
        ("return new(DataType.Nil, null);", "DynValue", {"new-dynvalue"}),
        (
            "return new(this, callback, sourceRef);",
            "ScriptExecutionContext",
            {"new-script-execution-context"},
        ),
        (
            "callback.Invoke(new(this, callback, sourceRef), args);",
            None,
            {"new-script-execution-context"},
        ),
        (
            "callback.Invoke(context: new(this, callback, sourceRef), args);",
            None,
            {"new-script-execution-context"},
        ),
        (
            "context: new(this, callback, sourceRef),",
            None,
            {"new-script-execution-context"},
            False,
            True,
        ),
        (
            "new(this, callback, sourceRef),",
            None,
            {"new-script-execution-context"},
            True,
        ),
        (
            "context: new(this, callback, sourceRef),",
            None,
            {"new-script-execution-context"},
            True,
        ),
        ("DynValue[] values = new[] { DynValue.Nil };", None, {"new-dynvalue-array"}),
        ("DynValue.Nil,", None, {"new-dynvalue-array"}, False, False, True),
        ("return new[] { DynValue.Nil };", "DynValue[]", {"new-dynvalue-array"}),
        ("private static DynValue Make() => new(DataType.Nil, null);", "DynValue", {"new-dynvalue"}),
        (
            "private static List<DynValue> Make() => new();",
            "List<DynValue>",
            {"new-list-dynvalue"},
        ),
        (
            "private static System.Collections.Generic.List<WallstopStudios.NovaSharp.Interpreter.DataTypes.DynValue> Make() => new();",
            "System.Collections.Generic.List<WallstopStudios.NovaSharp.Interpreter.DataTypes.DynValue>",
            {"new-list-dynvalue"},
        ),
        ("_valueStack.Push(new(DataType.Number, null));", None, {"new-dynvalue"}),
        (
            "new global::WallstopStudios.NovaSharp.Interpreter.DataTypes.DynValue();",
            None,
            {"new-dynvalue"},
        ),
        (
            "System.Collections.Generic.List<DynValue> values = new();",
            None,
            {"new-list-dynvalue"},
        ),
        (
            "System.Collections.Generic.List<WallstopStudios.NovaSharp.Interpreter.DataTypes.DynValue> values = new();",
            None,
            {"new-list-dynvalue"},
        ),
        (
            "new System.Collections.Generic.List<DynValue>();",
            None,
            {"new-list-dynvalue"},
        ),
        ("FixedCallArguments args = new(func);", "DynValue", set()),
    ]

    for case in cases:
        code = case[0]
        return_type = case[1]
        expected_rule_ids = case[2]
        pending_callback_invoke_context = case[3] if len(case) > 3 else False
        inside_callback_invoke = case[4] if len(case) > 4 else False
        pending_implicit_array_initializer = case[5] if len(case) > 5 else False
        actual_rule_ids = {
            rule.rule_id
            for rule in matching_rules(
                code,
                return_type,
                pending_callback_invoke_context,
                inside_callback_invoke,
                pending_implicit_array_initializer,
            )
        }
        if actual_rule_ids != expected_rule_ids:
            raise AssertionError(
                f"lint self-test failed for {code!r}: "
                f"expected {sorted(expected_rule_ids)}, got {sorted(actual_rule_ids)}"
            )

    multiline_cases = [
        (
            "multiline_implicit_dynvalue_array",
            [
                "var values = new[]",
                "{",
                "    DynValue.Nil,",
                "};",
            ],
            ["new-dynvalue-array"],
        ),
        (
            "multiline_named_callback_context",
            [
                "callback.Invoke(",
                "    args: args,",
                "    context: new(this, callback, sourceRef));",
            ],
            ["new-script-execution-context"],
        ),
        (
            "target_typed_list_return",
            [
                "private static System.Collections.Generic.List<WallstopStudios.NovaSharp.Interpreter.DataTypes.DynValue> Make() => new();",
            ],
            ["new-list-dynvalue"],
        ),
        (
            "global_target_typed_list_return",
            [
                "private static global::System.Collections.Generic.List<global::WallstopStudios.NovaSharp.Interpreter.DataTypes.DynValue> Make() => new();",
            ],
            ["new-list-dynvalue"],
        ),
        (
            "comments_and_strings_are_ignored",
            [
                "// DynValue.NewNumber(1)",
                "string text = \"new DynValue()\";",
                "/* new ScriptExecutionContext(",
                "DynValue.NewInteger(1) */",
            ],
            [],
        ),
        (
            "raw_multiline_strings_are_ignored",
            [
                "string script = \"\"\"",
                "new DynValue();",
                "DynValue.NewNumber(1);",
                "\"\"\";",
            ],
            [],
        ),
        (
            "interpolated_raw_multiline_strings_are_ignored",
            [
                "string script = $$\"\"\"",
                "new List<DynValue>();",
                "\"\"\";",
            ],
            [],
        ),
    ]

    for name, lines, expected_rule_ids in multiline_cases:
        findings, _ = analyze_lines(f"__selftest__/{name}.cs", lines, set())
        actual_rule_ids = [finding.rule.rule_id for finding in findings]
        if actual_rule_ids != expected_rule_ids:
            raise AssertionError(
                f"lint self-test failed for {name}: "
                f"expected {expected_rule_ids}, got {actual_rule_ids}"
            )


def is_allowed(
    path: str,
    symbol: str,
    rule: Rule,
    context: str,
    consumed_allowlist: set[int],
) -> tuple[int, AllowedMatch] | None:
    for index, allowed in enumerate(ALLOWLIST):
        if index in consumed_allowlist:
            continue

        if (
            allowed.rule_id == rule.rule_id
            and allowed.path == path
            and allowed.symbol == symbol
            and allowed.pattern.search(context)
        ):
            return index, allowed

    return None


def analyze_file(
    path: Path,
    consumed_allowlist: set[int],
) -> tuple[list[Finding], list[tuple[Finding, AllowedMatch]]]:
    relative_path = path.relative_to(REPO_ROOT).as_posix()
    try:
        lines = path.read_text(encoding="utf-8").splitlines()
    except OSError as ex:
        print(f"warning: could not read {relative_path}: {ex}", file=sys.stderr)
        return [], []

    return analyze_lines(relative_path, lines, consumed_allowlist)


def analyze_lines(
    relative_path: str,
    lines: list[str],
    consumed_allowlist: set[int],
) -> tuple[list[Finding], list[tuple[Finding, AllowedMatch]]]:
    findings: list[Finding] = []
    allowed_findings: list[tuple[Finding, AllowedMatch]] = []
    current_symbol = "<module>"
    current_return_type: str | None = None
    pending_callback_invoke_context = False
    callback_invoke_depth = 0
    pending_implicit_array_initializer = False
    stripped_lines: list[str] = []
    inside_block_comment = False
    raw_string_quote_count = 0

    for line in lines:
        stripped_line, inside_block_comment, raw_string_quote_count = strip_comments_and_strings(
            line,
            inside_block_comment,
            raw_string_quote_count,
        )
        stripped_lines.append(stripped_line)

    for line_index, line in enumerate(lines):
        line_number = line_index + 1
        code = stripped_lines[line_index]
        method_match = METHOD_PATTERN.match(code)
        if method_match:
            current_symbol = method_match.group("name")
            current_return_type = method_match.group("return_type")

        if not code.strip():
            continue

        context = normalize_context(stripped_lines, line_index)
        for rule in matching_rules(
            code,
            current_return_type,
            pending_callback_invoke_context,
            callback_invoke_depth > 0,
            pending_implicit_array_initializer,
        ):
            finding = Finding(relative_path, line_number, current_symbol, rule, line.strip())
            allowed_match = is_allowed(
                relative_path,
                current_symbol,
                rule,
                context,
                consumed_allowlist,
            )
            if allowed_match is not None:
                allowed_index, allowed = allowed_match
                consumed_allowlist.add(allowed_index)
                allowed_findings.append((finding, allowed))
            else:
                findings.append(finding)
            break

        pending_callback_invoke_context = CALLBACK_INVOKE_START_PATTERN.search(code) is not None
        callback_invoke_depth = update_callback_invoke_depth(callback_invoke_depth, code)
        pending_implicit_array_initializer = update_pending_implicit_array_initializer(
            pending_implicit_array_initializer,
            code,
        )

    return findings, allowed_findings


def find_target_files() -> list[Path]:
    files: set[Path] = set()
    for target in TARGET_FILE_GLOBS:
        matches = list(REPO_ROOT.glob(target))
        if matches:
            for match in matches:
                if match.is_file():
                    files.add(match)
        else:
            path = REPO_ROOT / target
            if path.is_file():
                files.add(path)

    return sorted(files)


def emit_ci_annotation(finding: Finding) -> None:
    if not is_ci():
        return

    message = finding.rule.description.replace("\n", " ")
    print(f"::error file={finding.path},line={finding.line_number}::{message}")


def print_report(
    findings: list[Finding],
    allowed_findings: list[tuple[Finding, AllowedMatch]],
    unused_allowlist: list[AllowedMatch],
    detailed: bool,
) -> None:
    print("VM hot-path allocation lint report")
    print("=" * 40)

    if findings:
        print()
        print("Violations:")
        for finding in findings:
            print(
                f"{finding.path}:{finding.line_number}: {finding.rule.rule_id}: "
                f"{finding.rule.description}"
            )
            print(f"  symbol: {finding.symbol}")
            print(f"  code: {finding.line}")
            emit_ci_annotation(finding)

    if detailed and allowed_findings:
        print()
        print("Allowlisted current allocations:")
        for finding, allowed in allowed_findings:
            print(
                f"{finding.path}:{finding.line_number}: {finding.rule.rule_id}: "
                f"{finding.symbol}"
            )
            print(f"  reason: {allowed.reason}")

    if unused_allowlist:
        print()
        print("Stale allowlist entries:")
        for allowed in unused_allowlist:
            print(f"{allowed.path}: {allowed.rule_id}: {allowed.symbol}")
            print(f"  reason: {allowed.reason}")

    print()
    if findings or unused_allowlist:
        if findings:
            print(f"Found {len(findings)} non-allowlisted VM hot-path allocation pattern(s).")
        if unused_allowlist:
            print(
                f"Found {len(unused_allowlist)} stale VM allocation allowlist entries; remove or update them."
            )
    else:
        print(
            f"[OK] No non-allowlisted VM hot-path allocation patterns found "
            f"({len(allowed_findings)} current allocation pattern(s) are explicitly allowlisted)."
        )


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Reject new allocation patterns in VM opcode and Lua-call hot paths."
    )
    parser.add_argument(
        "--detailed",
        action="store_true",
        help="Print allowlisted current allocations and their reasons.",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    run_self_tests()
    findings: list[Finding] = []
    allowed_findings: list[tuple[Finding, AllowedMatch]] = []
    consumed_allowlist: set[int] = set()

    for path in find_target_files():
        file_findings, file_allowed = analyze_file(path, consumed_allowlist)
        findings.extend(file_findings)
        allowed_findings.extend(file_allowed)

    unused_allowlist = [
        allowed for index, allowed in enumerate(ALLOWLIST) if index not in consumed_allowlist
    ]
    print_report(findings, allowed_findings, unused_allowlist, args.detailed)
    return 1 if findings or unused_allowlist else 0


if __name__ == "__main__":
    raise SystemExit(main())
