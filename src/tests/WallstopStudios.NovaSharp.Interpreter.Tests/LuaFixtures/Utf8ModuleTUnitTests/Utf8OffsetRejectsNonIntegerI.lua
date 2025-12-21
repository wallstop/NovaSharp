-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @error-pattern: number has no integer representation
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/Utf8ModuleTUnitTests.cs
-- @test: Utf8ModuleTUnitTests.Utf8OffsetRejectsNonIntegerI
-- @compat-notes: Lua 5.3+ requires integer representation for utf8.offset arguments
return utf8.offset("abc", 1, 1.5)
