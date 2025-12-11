-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs
-- @test: StringModuleTUnitTests.CharAcceptsBoundaryValues(255)
-- Standard Lua behavior: string.char(255) produces max byte value
return string.byte(string.char(255)) == 255
