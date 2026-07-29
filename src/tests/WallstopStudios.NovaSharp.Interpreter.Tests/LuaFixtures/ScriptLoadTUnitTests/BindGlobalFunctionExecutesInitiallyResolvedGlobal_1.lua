-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptLoadTUnitTests.cs:1437
-- @test: ScriptLoadTUnitTests.BindGlobalFunctionExecutesInitiallyResolvedGlobal
function update(value) return value + 100 end
