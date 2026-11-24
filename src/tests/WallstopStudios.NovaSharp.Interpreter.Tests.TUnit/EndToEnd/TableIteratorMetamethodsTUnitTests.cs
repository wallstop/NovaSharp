namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.EndToEnd
{
    using global::NovaSharp;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Interop.Attributes;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    public sealed class TableIteratorMetamethodsTUnitTests
    {
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public void PairsAdaptsMetamethodResultsToLuaVersion(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            script.DoString(
                @"
                local supportsHook = _VERSION ~= 'Lua 5.1'
                local supportsClose = _VERSION == 'Lua 5.5'
                local expectedCount = supportsClose and 4 or 3
                local closes, hookCalls = 0, 0
                local closer = setmetatable({}, {__close = function() closes = closes + 1 end})
                local function done() end
                local meta = {__pairs = function(...)
                    assert(select('#', ...) == 1, 'hook should only receive the iterated table')
                    hookCalls = hookCalls + 1
                    return done, nil, nil, closer, 'discard'
                end}
                local target = setmetatable({}, meta)
                assert(select('#', pairs({})) == expectedCount, 'default result count')
                assert(select('#', pairs(target, 'discard')) == expectedCount, 'hook result count')
                for key in pairs(target) do error('unexpected key') end
                assert(hookCalls == (supportsHook and 2 or 0), 'hook availability')
                assert(closes == (supportsClose and 1 or 0), 'fourth-result lifetime')

                meta.__pairs = function() return done end
                local generator, state, control, close = pairs(target)
                if supportsHook then
                    assert(generator == done and state == nil and control == nil and close == nil)
                end
                assert(select('#', pairs(target)) == expectedCount, 'padded hook result count')

                meta.__pairs = done
                if supportsHook then
                    local first = pairs(target)
                    assert(first == nil and select('#', first) == 1, 'explicit nil from a zero-result hook')

                    meta.__pairs = function() return done, done(), 0 end
                    local _, middle, last = pairs(target)
                    assert(middle == nil and select('#', middle) == 1 and last == 0, 'explicit nil before control')
                end

                meta.__pairs = type
                if supportsHook then
                    assert(pairs(target) == 'table')
                end
                assert(select('#', pairs(target)) == expectedCount, 'padded built-in hook result count')

                local function checkProtected(ok, ...)
                    assert(ok and select('#', ...) == expectedCount, 'protected iterator result count')
                end
                for _, hook in ipairs({function() end, setmetatable({}, {__call = function() end}), type}) do
                    meta.__pairs = hook
                    checkProtected(pcall(pairs, target))
                    if _VERSION == 'Lua 5.4' or supportsClose then
                        checkProtected(xpcall(pairs, function(err) return err end, target))
                    end
                end
                if supportsHook then
                    meta.__pairs = false
                    local ok, problem = pcall(pairs, target)
                    assert(not ok and string.find(problem, 'attempt to call a boolean value', 1, true))
                end
                "
            );
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public void PairsMetamethodCanYieldStartingInLua54(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            script.DoString(
                @"
                local meta = {}
                local target = setmetatable({}, meta)
                local function hook()
                    coroutine.yield('paused')
                    return 1, 2, 3, 4, 5
                end
                local function check(fn, payload, invoke)
                    meta.__pairs = fn
                    local task = coroutine.create(function() return select('#', invoke(pairs, target)) end)
                    local ok, value = coroutine.resume(task)
                    if _VERSION == 'Lua 5.2' or _VERSION == 'Lua 5.3' then
                        assert(not ok and string.find(value, 'yield', 1, true))
                    else
                        assert(ok and value == payload)
                        ok, value = coroutine.resume(task, 1, 2, 3, 4, 5)
                        local count = _VERSION == 'Lua 5.5' and 4 or 3
                        assert(ok and value == (invoke == pcall and count + 1 or count))
                    end
                end
                local function direct(fn, target) return fn(target) end
                check(hook, 'paused', direct)
                check(setmetatable({}, {__call = hook}), 'paused', direct)
                check(coroutine.yield, target, direct)
                if _VERSION == 'Lua 5.4' or _VERSION == 'Lua 5.5' then
                    check(coroutine.yield, target, pcall)
                end
                "
            );
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public void IpairsUsesOnlyTheProfileSupportedMetamethod(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            script.DoString(
                @"
                local called = false
                local function done() end
                local meta = {__ipairs = function()
                    called = true
                    return done, nil, nil, true
                end}
                local target = setmetatable({ 'default' }, meta)
                local seen = 0
                for index, value in ipairs(target) do
                    assert(index == 1 and value == 'default')
                    seen = seen + 1
                end
                -- Stock Lua 5.3 enables LUA_COMPAT_IPAIRS.
                assert(called == (_VERSION == 'Lua 5.2' or _VERSION == 'Lua 5.3'), '__ipairs availability')
                assert(seen == (called and 0 or 1))
                assert(select('#', ipairs(target)) == 3, 'ipairs result count')

                meta.__ipairs = function() return done end
                assert(select('#', ipairs(target)) == 3, 'padded ipairs result count')
                "
            );
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        [UserDataIsolation]
        public void UserDataIpairsCannotPassAnIteratorClosingValue(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            using UserDataRegistrationScope registry =
                UserDataRegistrationScope.Track<UserDataIterator>(ensureUnregistered: true);
            registry.RegisterType<UserDataIterator>();
            LuaValue yieldingIterator = script.Globals.Get("coroutine").Table.Get("yield");
            script.Globals.Set(
                "iteratorUserData",
                UserData.Create(new UserDataIterator(yieldingIterator)).Value
            );
            script.DoString(
                @"
                local function check(fn, expected)
                    local task = coroutine.create(fn)
                    local ok, marker = coroutine.resume(task)
                    assert(ok and marker == 'paused')
                    ok, marker = coroutine.resume(task, function() end, nil, nil, true)
                    assert(ok and marker == expected)
                end
                check(function() return select('#', ipairs(iteratorUserData)) end, 3)
                check(function()
                    for index in ipairs(iteratorUserData) do error('unexpected index') end
                    return 'done'
                end, 'done')
                "
            );
        }

        internal sealed class UserDataIterator
        {
            private readonly LuaValue _yieldingIterator;

            internal UserDataIterator(LuaValue yieldingIterator)
            {
                _yieldingIterator = yieldingIterator;
            }

            [NovaSharpUserDataMetamethod("__ipairs")]
            public LuaValue YieldIterator()
            {
                return LuaValue.NewTailCallReq(_yieldingIterator, LuaValue.NewString("paused"));
            }
        }
    }
}
