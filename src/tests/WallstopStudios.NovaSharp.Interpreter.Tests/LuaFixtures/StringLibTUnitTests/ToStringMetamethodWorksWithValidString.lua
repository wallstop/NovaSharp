-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/StringLibTUnitTests.cs:343
-- @test: StringLibTUnitTests.ToStringMetamethodWorksWithValidString
-- @compat-notes: Test targets Lua 5.3+
t = {}
				mt = {}
				function mt.__tostring () return 'custom_tostring' end
				setmetatable(t, mt)
                return tostring(t)
