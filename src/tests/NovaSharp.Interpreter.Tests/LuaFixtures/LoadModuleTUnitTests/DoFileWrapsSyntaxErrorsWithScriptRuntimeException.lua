-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleTUnitTests.cs:308
-- @test: LoadModuleTUnitTests.DoFileWrapsSyntaxErrorsWithScriptRuntimeException
dofile('broken.lua')
