-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:1142
-- @test: StringModuleTUnitTests.DumpWrapsClrFunctionFailuresWithScriptRuntimeException
-- @compat-notes: Test targets Lua 5.1; Uses injected variable: callback
return string.dump(callback)
