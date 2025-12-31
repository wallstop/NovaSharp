-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\EndToEnd\StringLibTUnitTests.cs:300
-- @test: StringLibTUnitTests.ToStringMetamethodRejectsNumberReturnInLua53Plus
-- @compat-notes: Test targets Lua 5.1
t = {}
				mt = {}
				function mt.__tostring () return 42 end
				setmetatable(t, mt)
                return tostring(t)
