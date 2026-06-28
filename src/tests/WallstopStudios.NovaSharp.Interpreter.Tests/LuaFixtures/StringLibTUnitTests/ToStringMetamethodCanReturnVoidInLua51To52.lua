-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\EndToEnd\StringLibTUnitTests.cs:252
-- @test: StringLibTUnitTests.ToStringMetamethodCanReturnVoidInLua51To52
-- Test targets Lua 5.1
t = {}
				mt = {}
				a = nil
				function mt.__tostring () a = 'yup' end
				setmetatable(t, mt)
                return tostring(t), a;
