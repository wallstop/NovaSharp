-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:29
-- @test: StringModuleTUnitTests.CharThrowsWhenArgumentCannotBeCoerced
return string.char("not-a-number")
