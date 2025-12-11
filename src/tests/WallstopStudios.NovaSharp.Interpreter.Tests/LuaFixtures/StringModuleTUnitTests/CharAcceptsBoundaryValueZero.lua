-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs
-- @test: StringModuleTUnitTests.CharAcceptsBoundaryValues(0)
-- Standard Lua behavior: string.char(0) produces null byte
return string.byte(string.char(0)) == 0
