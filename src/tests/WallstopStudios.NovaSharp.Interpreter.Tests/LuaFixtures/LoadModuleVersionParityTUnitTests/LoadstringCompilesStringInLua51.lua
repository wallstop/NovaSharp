-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleVersionParityTUnitTests.cs:57
-- @test: LoadModuleVersionParityTUnitTests.LoadstringCompilesStringInLua51
-- @compat-notes: Test targets Lua 5.1
local f, err = loadstring('return 42')
                assert(f ~= nil, 'loadstring should return a function')
                return f()
