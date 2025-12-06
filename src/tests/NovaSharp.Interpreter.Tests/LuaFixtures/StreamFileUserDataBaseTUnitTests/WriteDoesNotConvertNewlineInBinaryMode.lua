-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:1365
-- @test: StreamFileUserDataBaseTUnitTests.WriteDoesNotConvertNewlineInBinaryMode
file:write('line1\
line2')
