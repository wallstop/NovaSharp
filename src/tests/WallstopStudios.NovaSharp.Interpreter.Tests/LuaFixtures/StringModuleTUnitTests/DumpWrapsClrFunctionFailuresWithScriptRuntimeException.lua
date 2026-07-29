-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:853
-- @test: StringModuleTUnitTests.DumpWrapsClrFunctionFailuresWithScriptRuntimeException
-- Compatibility notes: Uses injected variable: callback
return string.dump(callback)
