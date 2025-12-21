-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/VarargsTupleTUnitTests.cs:85
-- @test: VarargsTupleTUnitTests.VarArgsTupleDontCrash
-- @compat-notes: Test targets Lua 5.1
function Obj(...)
                    local args = { ... }
                end
                Obj(1)
