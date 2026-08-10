---
name: adding-opcodes
description: "Add or change NovaSharp VM bytecode instructions across the opcode enum, compiler, execution loop, and tests. Use when implementing opcodes, bytecode, VM instructions, or execution-loop behavior."
metadata:
  category: lua
  priority: reference
  related: codebase-navigation, tunit-test-writing, lua-fixture-creation
---
# Skill: Adding New VM Opcodes

**Related Skills**: [codebase-navigation](../codebase-navigation/SKILL.md) (debugging execution), [tunit-test-writing](../tunit-test-writing/SKILL.md) (testing opcodes), [lua-fixture-creation](../lua-fixture-creation/SKILL.md) (creating .lua fixtures)

______________________________________________________________________

## Overview

NovaSharp uses a bytecode VM with per-function bytecode and stack-based execution. Adding a new opcode requires changes in multiple locations.

______________________________________________________________________

## Additional guidance

Read [the detailed reference](references/REFERENCE.md) for Steps to Add an Opcode, Key Files, Architecture Context, Debugging Tips.
