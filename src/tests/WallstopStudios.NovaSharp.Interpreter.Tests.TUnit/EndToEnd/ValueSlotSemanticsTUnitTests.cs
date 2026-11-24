namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.EndToEnd
{
    using System.Threading.Tasks;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    /// <summary>
    /// Guards the slot/value split: locals and upvalues are mutable cells holding immutable values.
    /// Reading a variable yields a snapshot that later assignments must never rewrite, while every
    /// closure over the same variable must keep observing the one shared cell.
    /// </summary>
    public sealed class ValueSlotSemanticsTUnitTests
    {
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ReassigningLocalDoesNotRewriteStoredValue(LuaCompatibilityVersion version)
        {
            string code =
                @"
                local x = 1
                local t = { x, x }
                x = 2
                t[3] = x
                return t[1], t[2], t[3], x
                ";
            Script script = new Script(version, CoreModulePresets.Complete);
            await EndToEndDynValueAssert
                .ExpectAsync(script.DoString(code), 1, 1, 2, 2)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ReassigningUpvalueDoesNotRewriteStoredValue(
            LuaCompatibilityVersion version
        )
        {
            string code =
                @"
                local x = 1
                local set = function(v) x = v end
                local t = { x }
                set(2)
                t[2] = x
                return t[1], t[2]
                ";
            Script script = new Script(version, CoreModulePresets.Complete);
            await EndToEndDynValueAssert
                .ExpectAsync(script.DoString(code), 1, 2)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task NumericForControlVariableIsSnapshotPerIteration(
            LuaCompatibilityVersion version
        )
        {
            string code =
                @"
                local values = {}
                local fns = {}
                for i = 1, 3 do
                    values[i] = i
                    fns[i] = function() return i end
                end
                return values[1], values[2], values[3], fns[1](), fns[2](), fns[3]()
                ";
            Script script = new Script(version, CoreModulePresets.Complete);
            await EndToEndDynValueAssert
                .ExpectAsync(script.DoString(code), 1, 2, 3, 1, 2, 3)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public void GenericForLocalsStartAsNil(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            script.DoString(
                @"
                local state = { 10, 20 }
                local function step(s, previous)
                    assert(s == state, 'iterator state')
                    local index = previous + 1
                    if s[index] then return index, s[index] end
                end

                local readers = {}
                for index, value in step, state, 0 do
                    local first, second, third, fourth
                    assert(first == nil, 'first local captured iterator state')
                    assert(second == nil and third == nil and fourth == nil, 'remaining locals')
                    for _, nestedValue in ipairs({ value }) do
                        local nested
                        assert(nested == nil and nestedValue == value, 'nested iterator')
                    end
                    readers[index] = function() return first, second end
                    if index == 1 then first, second = value, index end
                end

                local first, second = readers[1]()
                assert(first == 10 and second == 1, 'first iteration capture')
                first, second = readers[2]()
                assert(first == nil and second == nil, 'second iteration capture')

                for index = 1, 1 do
                    local fresh
                    assert(fresh == nil, 'numeric loop control')
                end

                local function empty() end
                for index in step, state, 0 do
                    local explicit = nil
                    local fromEmpty = empty()
                    assert(explicit == nil and fromEmpty == nil, 'explicit initializers')
                    break
                end
                "
            );
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public void GenericForClosingValueLivesUntilExit(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            script.DoString(
                @"
                local hasClose = _VERSION == 'Lua 5.4' or _VERSION == 'Lua 5.5'
                local closed, closeError
                local closer = setmetatable({}, {__close = function(self, err)
                    closed = closed + 1
                    closeError = err
                end})
                local function step(limit, previous)
                    if previous < limit then return previous + 1 end
                end
                local function controls(limit)
                    return step, limit, 0, closer, true
                end
                local function run(mode)
                    if mode == 'iterator error' then
                        local function fail() error('iterator failed', 0) end
                        for _ in fail, nil, nil, closer do error('unreachable') end
                        return
                    end
                    for i in controls(mode == 'empty' and 0 or 2) do
                        assert(closed == 0, 'premature close')
                        if mode == 'break' then break end
                        if mode == 'return' then return i end
                        if mode == 'body error' then error('body failed', 0) end
                    end
                end

                for _, mode in ipairs({'empty', 'exhausted', 'break', 'return', 'body error', 'iterator error'}) do
                    closed, closeError = 0, nil
                    local expectedError = ({['body error'] = 'body failed', ['iterator error'] = 'iterator failed'})[mode]
                    local ok, result = pcall(run, mode)
                    assert(ok == (expectedError == nil), mode)
                    assert(closed == (hasClose and 1 or 0), 'close count: ' .. mode)
                    if mode == 'return' then assert(result == 1) end
                    assert(closeError == (hasClose and expectedError or nil), 'active error: ' .. mode)
                end

                local function checkInvalid()
                    for _ in step, 0, 0, true do error('unreachable') end
                end
                local ok, message = pcall(checkInvalid)
                assert(ok == not hasClose, 'invalid closer')
                if hasClose then
                    assert(string.find(message, ""variable '(for state)' got a non-closable value"", 1, true))
                end
                for _ in step, 0, 0, false do error('unreachable') end
                "
            );
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua54)]
        public void GenericForClosingValueFollowsLexicalExitOrder(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            script.DoString(
                @"
                local log = {}
                local function closer(name)
                    return setmetatable({}, {__close = function() log[#log + 1] = name end})
                end
                local function once(_, previous)
                    if previous == 0 then return 1 end
                end

                do
                    local surrounding <close> = closer('surrounding')
                    for _ in once, nil, 0, closer('iterator') do
                        if true then
                            local inner <close> = closer('inner')
                            do break end
                        end
                    end
                    log[#log + 1] = 'after break'
                end

                for _ in once, nil, 0, closer('goto iterator') do
                    do
                        local inner <close> = closer('goto inner')
                        goto finished
                    end
                end
                ::finished::
                assert(table.concat(log, ',') == 'inner,iterator,after break,surrounding,goto inner,goto iterator')
                "
            );
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua54)]
        public void GenericForDispatchesCloseMetamethodAtExit(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            script.DoString(
                @"
                local resource
                local called = false
                local target = setmetatable({}, {__call = function(_, value, err)
                    assert(value == resource and err == nil)
                    called = true
                end})
                local meta = {__close = target}
                resource = setmetatable({}, meta)
                local function once(_, previous)
                    if previous == 0 then return 1 end
                end
                for _ in once, nil, 0, resource do assert(not called) end
                assert(called, 'callable close')

                local function checkInvalid(replacement, errorType)
                    meta.__close = target
                    local ok, message = pcall(function()
                        for _ in once, nil, 0, resource do meta.__close = replacement end
                    end)
                    local expected = 'attempt to call a ' .. errorType .. "" value (metamethod 'close')""
                    assert(not ok and string.find(message, expected, 1, true), tostring(message))
                end
                checkInvalid(false, 'boolean')
                checkInvalid(nil, 'nil')
                checkInvalid(setmetatable({}, {__call = false}), 'boolean')

                called = false
                meta.__close = target
                local task = coroutine.create(function()
                    for _ in once, nil, 0, resource do coroutine.yield() end
                end)
                assert(coroutine.resume(task) and not called)
                assert(coroutine.close(task) and called, 'close suspended loop')
                "
            );
        }

        /// <summary>
        /// Every loop form must give each iteration a fresh cell for its body-locals, otherwise all
        /// closures made in the loop would alias one cell and report the final value. Cell freshness
        /// depends on the block-clear that nulls the slot at each iteration's scope entry, so all
        /// loop forms are covered, not just numeric <c>for</c>. Expectations were taken from
        /// reference lua5.1-lua5.5.
        /// </summary>
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task EveryLoopFormGivesEachIterationItsOwnCell(LuaCompatibilityVersion version)
        {
            (string Body, string Expected)[] cases =
            {
                ("for i = 1, 3 do fns[#fns + 1] = function() return i end end", "1,2,3"),
                (
                    "for _, v in ipairs({ 10, 20, 30 }) do fns[#fns + 1] = function() return v end end",
                    "10,20,30"
                ),
                (
                    "for i = 1, 3 do local x = i * 100 fns[#fns + 1] = function() return x end end",
                    "100,200,300"
                ),
                (
                    "local n = 0 while n < 3 do n = n + 1 local y = n * 7 fns[#fns + 1] = function() return y end end",
                    "7,14,21"
                ),
                (
                    "local m = 0 repeat m = m + 1 local z = m * 5 fns[#fns + 1] = function() return z end until m >= 3",
                    "5,10,15"
                ),
            };

            Script script = new Script(version, CoreModulePresets.Complete);
            foreach ((string body, string expected) in cases)
            {
                string code =
                    $@"
                    local fns = {{}}
                    {body}
                    local out = {{}}
                    for i = 1, #fns do out[i] = tostring(fns[i]()) end
                    return table.concat(out, "","")
                    ";
                await EndToEndDynValueAssert
                    .ExpectAsync(script.DoString(code), expected)
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// A closure that mutates its captured per-iteration local must keep writing through to that
        /// iteration's own cell across repeated invocations.
        /// </summary>
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task PerIterationCellsStayIndependentAcrossMutatingCalls(
            LuaCompatibilityVersion version
        )
        {
            string code =
                @"
                local fns = {}
                for i = 1, 3 do
                    local k = i
                    fns[#fns + 1] = function() k = k + 1 return k end
                end
                local function drain()
                    local out = {}
                    for i = 1, #fns do out[i] = tostring(fns[i]()) end
                    return table.concat(out, "","")
                end
                return drain(), drain()
                ";
            Script script = new Script(version, CoreModulePresets.Complete);
            await EndToEndDynValueAssert
                .ExpectAsync(script.DoString(code), "2,3,4", "3,4,5")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// <c>debug.upvalueid</c> exists so a program can tell whether two closures share the same
        /// variable, so its identity must track the cell, never the value the cell currently holds.
        /// Keying by value would both collide unrelated upvalues that happen to hold the same shared
        /// instance (nil, true, a cached small integer) and change one upvalue's identity whenever it
        /// is assigned. Expectations were taken from reference lua5.2-lua5.5.
        /// </summary>
        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task UpvalueIdTracksTheCellNotTheValue(LuaCompatibilityVersion version)
        {
            string code =
                @"
                local function mkNil() local v = nil return function() return v end end
                local function mkInt(n) local v = n return function() return v end end
                local function pair()
                    local v = 0
                    return function() return v end, function() v = v + 1 end
                end

                -- distinct variables that merely hold the same value must not collide
                local sharesNil = debug.upvalueid(mkNil(), 1) == debug.upvalueid(mkNil(), 1)
                local sharesInt = debug.upvalueid(mkInt(1), 1) == debug.upvalueid(mkInt(1), 1)

                -- two closures over one variable must share, and stay stable across assignment
                local read, bump = pair()
                local sharesLocal = debug.upvalueid(read, 1) == debug.upvalueid(bump, 1)
                local before = debug.upvalueid(read, 1)
                bump()
                bump()
                local stable = debug.upvalueid(read, 1) == before

                return sharesNil, sharesInt, sharesLocal, stable
                ";
            Script script = new Script(version, CoreModulePresets.Complete);
            await EndToEndDynValueAssert
                .ExpectAsync(script.DoString(code), false, false, true, true)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ClosuresShareOneCellForTheSameLocal(LuaCompatibilityVersion version)
        {
            string code =
                @"
                local n = 0
                local bump = function() n = n + 1 end
                local read = function() return n end
                bump()
                bump()
                return read(), n
                ";
            Script script = new Script(version, CoreModulePresets.Complete);
            await EndToEndDynValueAssert
                .ExpectAsync(script.DoString(code), 2, 2)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ClosureCapturedBeforeAssignmentSeesLaterValue(
            LuaCompatibilityVersion version
        )
        {
            string code =
                @"
                local x
                local read = function() return x end
                local before = read()
                x = 7
                return before == nil, read()
                ";
            Script script = new Script(version, CoreModulePresets.Complete);
            await EndToEndDynValueAssert
                .ExpectAsync(script.DoString(code), true, 7)
                .ConfigureAwait(false);
        }
    }
}
