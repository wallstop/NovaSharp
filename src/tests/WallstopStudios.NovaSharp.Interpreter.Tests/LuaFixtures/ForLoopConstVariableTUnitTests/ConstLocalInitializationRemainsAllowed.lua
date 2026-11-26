-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ForLoopConstVariableTUnitTests.cs:109
-- @test: ForLoopConstVariableTUnitTests.ConstLocalInitializationRemainsAllowed
-- Compatibility notes: Test targets Lua 5.4+; Lua 5.4+: const attribute
local value <const> = 7 return value
