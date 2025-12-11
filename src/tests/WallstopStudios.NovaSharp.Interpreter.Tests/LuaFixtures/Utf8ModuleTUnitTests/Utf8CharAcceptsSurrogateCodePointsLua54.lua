-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/Utf8ModuleTUnitTests.cs:353
-- @test: Utf8ModuleTUnitTests.Utf8CharAcceptsSurrogateCodePointsLua54
-- @compat-notes: Lua 5.4+ accepts surrogate code points (0xD800-0xDFFF) that Lua 5.3 rejects
return #utf8.char(0xD800)
