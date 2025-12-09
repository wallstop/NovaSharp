-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/JsonModuleTUnitTests.cs:64
-- @test: JsonModuleTUnitTests.ParseThrowsScriptRuntimeExceptionOnInvalidJson
-- @compat-notes: NovaSharp: NovaSharp json module
return require('json')
