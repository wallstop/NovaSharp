-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\EndToEnd\StringLibTUnitTests.cs:390
-- @test: StringLibTUnitTests.ToStringMetamethodAllowsNilReturnInLua51To52
-- Test targets Lua 5.1
t = {}
				mt = {}
				function mt.__tostring () return nil end
				setmetatable(t, mt)
                return tostring(t)
