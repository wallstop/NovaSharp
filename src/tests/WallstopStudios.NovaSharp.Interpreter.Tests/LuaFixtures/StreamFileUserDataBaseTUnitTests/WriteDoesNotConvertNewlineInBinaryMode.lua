-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:1365
-- @test: StreamFileUserDataBaseTUnitTests.WriteDoesNotConvertNewlineInBinaryMode
-- @compat-notes: Uses injected variable: file
file:write('line1\
line2')
