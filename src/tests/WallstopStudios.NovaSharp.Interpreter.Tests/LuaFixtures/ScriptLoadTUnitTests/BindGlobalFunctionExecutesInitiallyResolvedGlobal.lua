-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptLoadTUnitTests.cs:1434
-- @test: ScriptLoadTUnitTests.BindGlobalFunctionExecutesInitiallyResolvedGlobal
function update(value) return value + 1 end
