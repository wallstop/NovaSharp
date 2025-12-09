-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/Lua55SpecTUnitTests.cs:361
-- @test: Lua55SpecTUnitTests.RightShiftOperatorWorks
-- @compat-notes: Lua 5.3+: bit shift
return 32 >> 2
