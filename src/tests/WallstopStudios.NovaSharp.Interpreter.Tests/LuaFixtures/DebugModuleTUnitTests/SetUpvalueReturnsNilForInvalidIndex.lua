-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:477
-- @test: DebugModuleTUnitTests.SetUpvalueReturnsNilForInvalidIndex
-- @compat-notes: Test targets Lua 5.1
local function f() end
                return debug.setupvalue(f, 999, 'test')
