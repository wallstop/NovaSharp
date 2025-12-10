-- @lua-versions: 5.3
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/Utf8ModuleTUnitTests.cs:339
-- @test: Utf8ModuleTUnitTests.Utf8CharRejectsSurrogateCodePointsLua53
-- @compat-notes: Lua 5.3 rejects surrogate code points (0xD800-0xDFFF); Lua 5.4+ accepts them
return utf8.char(0xD800)
