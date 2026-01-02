-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\EndToEnd\StringLibTUnitTests.cs:275
-- @test: StringLibTUnitTests.ToStringMetamethodMustReturnStringInLua53Plus
-- @compat-notes: Test targets Lua 5.3+
t = {}
				mt = {}
				function mt.__tostring () end
				setmetatable(t, mt)
                return tostring(t)
