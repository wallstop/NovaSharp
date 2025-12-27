-- @lua-versions: none
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:3291
-- @test: DebugModuleTUnitTests.GetUpvalueAndSetUpvalueRoundTrip
-- @compat-notes: Test targets Lua 5.1; Lua 5.2+: _ENV variable
local captured = 'initial'
                local function closure()
                    return captured
                end
                
                -- Find the upvalue index for 'captured' (skip _ENV which is usually index 1)
                local upvalueIndex = nil
                for i = 1, 10 do
                    local name, _ = debug.getupvalue(closure, i)
                    if name == 'captured' then
                        upvalueIndex = i
                        break
                    end
                    if name == nil then break end
                end
                
                if upvalueIndex == nil then
                    return 'UPVALUE_NOT_FOUND'
                end
                
                -- Get original value
                local name1, val1 = debug.getupvalue(closure, upvalueIndex)
                
                -- Set new value
                local setName = debug.setupvalue(closure, upvalueIndex, 'modified')
                
                -- Get new value
                local name2, val2 = debug.getupvalue(closure, upvalueIndex)
                
                -- Call closure to verify it uses new value
                local result = closure()
                
                return name1, val1, setName, name2, val2, result
