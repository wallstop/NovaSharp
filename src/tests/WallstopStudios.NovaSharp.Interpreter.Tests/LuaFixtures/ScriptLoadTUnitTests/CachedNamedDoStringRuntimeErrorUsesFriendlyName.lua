-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptLoadTUnitTests.cs:2339
-- @test: ScriptLoadTUnitTests.CachedNamedDoStringRuntimeErrorUsesFriendlyName
local f = nil; return f()
