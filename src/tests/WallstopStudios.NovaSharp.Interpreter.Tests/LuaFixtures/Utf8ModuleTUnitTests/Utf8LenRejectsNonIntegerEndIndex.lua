-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @error-pattern: number has no integer representation
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/Utf8ModuleTUnitTests.cs
-- @test: Utf8ModuleTUnitTests.Utf8LenRejectsNonIntegerEndIndex
-- @compat-notes: Lua 5.3+ requires integer representation for utf8.len arguments
return utf8.len("abc", 1, 2.5)
