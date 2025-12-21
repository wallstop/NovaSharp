-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/BasicModuleTUnitTests.cs:1059
-- @test: BasicModuleTUnitTests.PrintSeparatesArgumentsWithTabs
-- @compat-notes: Test targets Lua 5.1
print(1, 2, 3)
