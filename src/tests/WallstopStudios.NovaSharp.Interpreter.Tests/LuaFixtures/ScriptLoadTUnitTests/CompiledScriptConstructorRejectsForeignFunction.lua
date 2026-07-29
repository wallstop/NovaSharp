-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptLoadTUnitTests.cs:1823
-- @test: ScriptLoadTUnitTests.CompiledScriptConstructorRejectsForeignFunction
return function() return 1 end
