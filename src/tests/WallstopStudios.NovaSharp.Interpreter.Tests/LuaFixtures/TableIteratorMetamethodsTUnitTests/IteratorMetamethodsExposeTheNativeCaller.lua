-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/TableIteratorMetamethodsTUnitTests.cs:122
-- @test: TableIteratorMetamethodsTUnitTests.IteratorMetamethodsExposeTheNativeCaller
-- Compatibility notes: Test targets Lua 5.2+
local nativePairs, nativeIpairs = pairs, ipairs
                pairs, ipairs = nil, nil
                local oldProfile = _VERSION == 'Lua 5.2' or _VERSION == 'Lua 5.3'
                local temporary = oldProfile and '(*temporary)' or '(C temporary)'
                local function check(iterator, hook, protected)
                    local ran = false
                    local target = setmetatable({}, {[hook] = function(subject)
                        ran = true
                        local info = debug.getinfo(2, 'Slutf')
                        assert(info.what == 'C' and info.source == '=[C]' and info.short_src == '[C]')
                        assert(info.linedefined == -1 and info.lastlinedefined == -1 and info.currentline == -1)
                        assert(info.nparams == 0 and info.nups == 0 and info.isvararg and not info.istailcall)
                        assert(info.func == iterator and debug.getinfo(info.func, 'S').what == 'C')
                        assert(debug.getinfo(3, 'S').what == (protected and 'C' or 'Lua'))
                        assert(string.find(debug.traceback(), '[C]: in', 1, true))
                        local name, value = debug.getlocal(2, 1)
                        assert(name == temporary and value == subject and debug.getlocal(2, 2) == nil)
                        assert(debug.setlocal(2, 2, false) == nil)
                        assert(debug.setlocal(2, 1, false) == temporary)
                        name, value = debug.getlocal(2, 1)
                        assert(name == temporary and value == false)
                        return next, subject, nil
                    end})
                    if protected then
                        assert(pcall(iterator, target))
                    else
                        iterator(target)
                    end
                    assert(ran)
                end
                check(nativePairs, '__pairs', false)
                check(nativePairs, '__pairs', true)
                if oldProfile then
                    check(nativeIpairs, '__ipairs', false)
                    check(nativeIpairs, '__ipairs', true)
                end
