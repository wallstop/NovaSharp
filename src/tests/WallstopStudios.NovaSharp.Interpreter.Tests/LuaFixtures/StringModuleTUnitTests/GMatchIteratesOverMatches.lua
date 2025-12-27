-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:728
-- @test: StringModuleTUnitTests.GMatchIteratesOverMatches
-- @compat-notes: Test targets Lua 5.4+
local iter = string.gmatch('one two', '%w+')
                return iter(), iter()
