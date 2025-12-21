-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/Utf8ModuleTUnitTests.cs:398
-- @test: Utf8ModuleTUnitTests.Utf8OffsetSupportsNegativePositions
-- @compat-notes: Test targets Lua 5.3+; Lua 5.3+: utf8 library
local fromEnd = utf8.offset('abcd', 1, -1)
                return fromEnd
