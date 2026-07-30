-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptCallTUnitTests.cs:261
-- @test: ScriptCallTUnitTests.TablePackVarargsPreservesScalars
-- Compatibility notes: Test targets Lua 5.2+; Lua 5.2+: table.pack (5.2+)
return function(...) return table.pack(...) end
