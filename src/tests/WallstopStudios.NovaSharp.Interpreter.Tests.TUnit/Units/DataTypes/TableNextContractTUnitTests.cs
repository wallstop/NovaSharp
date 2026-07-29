namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.DataTypes
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    /// <summary>
    /// Locks the <c>next</c>/<c>pairs</c> traversal contract that the table storage must honour
    /// independently of how the array and hash parts are laid out internally.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Assertions never depend on hash-part ordering, which the Lua manual leaves unspecified and
    /// which genuinely differs between NovaSharp and reference Lua. Where a test needs to compare
    /// key sets it sorts or counts them, so the extracted fixtures stay valid under the reference
    /// interpreters.
    /// </para>
    /// <para>
    /// Each snippet prints its outcome as well as returning it, so the extracted <c>.lua</c> fixture
    /// produces output the cross-interpreter comparison harness can actually diff.
    /// </para>
    /// </remarks>
    public sealed class TableNextContractTUnitTests
    {
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task PairsVisitsEveryKeyExactlyOnce(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            DynValue result = script.DoString(
                @"
                local function check()
                    local t = {}
                    for i = 1, 40 do t[i] = i end
                    t.alpha = 'a'
                    t.beta = 'b'
                    t[500] = 'sparse'
                    t[-3] = 'negative'
                    t[0] = 'zero'
                    t[2.5] = 'fractional'
                    t[true] = 'boolean'

                    local seen = {}
                    local count = 0
                    for k in pairs(t) do
                        if seen[k] then return 'duplicate: ' .. tostring(k) end
                        seen[k] = true
                        count = count + 1
                    end

                    local expected = 40 + 2 + 1 + 1 + 1 + 1 + 1
                    if count ~= expected then
                        return 'count ' .. count .. ' expected ' .. expected
                    end

                    for i = 1, 40 do if not seen[i] then return 'missing ' .. i end end
                    for _, k in ipairs({ 'alpha', 'beta', 500, -3, 0, 2.5, true }) do
                        if not seen[k] then return 'missing ' .. tostring(k) end
                    end

                    return 'ok'
                end

                local outcome = check()
                print(outcome)
                return outcome
            "
            );

            await Assert.That(result.String).IsEqualTo("ok").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task AssigningExistingFieldsDuringTraversalIsLegal(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            DynValue result = script.DoString(
                @"
                local function check()
                    local t = {}
                    for i = 1, 20 do t[i] = i end
                    t.name = 'value'
                    t.other = 'value'

                    local visited = 0
                    for k in pairs(t) do
                        t[k] = 'rewritten'
                        visited = visited + 1
                    end

                    local cleared = 0
                    for k in pairs(t) do
                        t[k] = nil
                        cleared = cleared + 1
                    end

                    local remaining = 0
                    for _ in pairs(t) do remaining = remaining + 1 end

                    return visited .. ',' .. cleared .. ',' .. remaining
                end

                local outcome = check()
                print(outcome)
                return outcome
            "
            );

            await Assert.That(result.String).IsEqualTo("22,22,0").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task NextResumesFromEveryReturnedKey(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            DynValue result = script.DoString(
                @"
                local function check()
                    local t = { 10, 20, 30 }
                    t.tail = 'end'

                    local count = 0
                    local key, value = next(t)
                    while key ~= nil do
                        count = count + 1
                        if value == nil then return 'nil value at ' .. tostring(key) end
                        key, value = next(t, key)
                    end

                    if next({}) ~= nil then return 'empty table must yield nil' end

                    return tostring(count)
                end

                local outcome = check()
                print(outcome)
                return outcome
            "
            );

            await Assert.That(result.String).IsEqualTo("4").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task NextStillAdvancesAfterTheCurrentKeyIsCleared(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            DynValue result = script.DoString(
                @"
                local function check()
                    -- Clearing the key you are standing on is the one mutation the manual permits
                    -- mid-traversal, so next() must still resolve that key as a cursor afterwards.
                    local t = { 10, 20, 30, 40 }
                    t.tail = 'end'

                    local visited = 0
                    local key = next(t)
                    while key ~= nil do
                        visited = visited + 1
                        local current = key
                        key = next(t, current)
                        t[current] = nil
                    end

                    local remaining = 0
                    for _ in pairs(t) do remaining = remaining + 1 end

                    return visited .. ',' .. remaining
                end

                local outcome = check()
                print(outcome)
                return outcome
            "
            );

            await Assert.That(result.String).IsEqualTo("5,0").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task BorderIsAValidBoundaryForHoleyTables(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            DynValue result = script.DoString(
                @"
                local function check()
                    -- A border n satisfies t[n] ~= nil and t[n+1] == nil, for any table shape.
                    local function isBorder(t)
                        local n = #t
                        if n == 0 then return t[1] == nil end
                        return t[n] ~= nil and t[n + 1] == nil
                    end

                    local cases = {
                        {},
                        { 1 },
                        { 1, 2, 3 },
                        { 1, 2, nil, 4 },
                        { nil, 2 },
                        { 1, nil, nil, nil, 5 },
                    }

                    for i = 1, 6 do
                        if not isBorder(cases[i]) then return 'constructor case ' .. i end
                    end

                    local grown = {}
                    for i = 1, 33 do
                        grown[i] = i
                        if #grown ~= i then return 'append border at ' .. i end
                    end

                    -- Shrinking only guarantees *a* border, not a particular one, so assert the
                    -- invariant rather than an exact length.
                    for i = 33, 1, -1 do
                        grown[i] = nil
                        if not isBorder(grown) then return 'shrink border at ' .. i end
                    end

                    return 'ok'
                end

                local outcome = check()
                print(outcome)
                return outcome
            "
            );

            await Assert.That(result.String).IsEqualTo("ok").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task SparseAndDenseIntegerKeysCoexist(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            DynValue result = script.DoString(
                @"
                local function check()
                    -- Forces a dense integer prefix into contiguous storage and keys far past it
                    -- into the hashed side, then checks every key still reads back and is
                    -- traversed exactly once.
                    local t = {}
                    for i = 1, 64 do t[i] = i end
                    for _, k in ipairs({ 1000, 5000, 100000 }) do t[k] = k end

                    for i = 1, 64 do
                        if t[i] ~= i then return 'dense miss ' .. i end
                    end
                    for _, k in ipairs({ 1000, 5000, 100000 }) do
                        if t[k] ~= k then return 'sparse miss ' .. k end
                    end

                    local total = 0
                    for k, v in pairs(t) do
                        if k ~= v then return 'key/value drift ' .. tostring(k) end
                        total = total + 1
                    end

                    if #t ~= 64 then return 'border ' .. #t end
                    return tostring(total)
                end

                local outcome = check()
                print(outcome)
                return outcome
            "
            );

            await Assert.That(result.String).IsEqualTo("67").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ReinsertingRemovedKeysKeepsTraversalConsistent(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            DynValue result = script.DoString(
                @"
                local function check()
                    -- Churn removes and re-adds keys many times; the traversal must always report
                    -- the live set exactly, no matter how the storage recycles or compacts entries.
                    local t = {}
                    for round = 1, 8 do
                        for i = 1, 50 do t['k' .. i] = round end
                        for i = 1, 50, 2 do t['k' .. i] = nil end

                        local live = 0
                        for k, v in pairs(t) do
                            if v ~= round then return 'stale value at ' .. k end
                            live = live + 1
                        end
                        if live ~= 25 then return 'round ' .. round .. ' live ' .. live end

                        for i = 1, 50, 2 do t['k' .. i] = round end
                    end

                    local final = 0
                    for _ in pairs(t) do final = final + 1 end
                    return tostring(final)
                end

                local outcome = check()
                print(outcome)
                return outcome
            "
            );

            await Assert.That(result.String).IsEqualTo("50").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task NextRejectsKeysThatAreNotInTheTable(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            DynValue result = script.DoString(
                @"
                local function check()
                    local t = { a = 1 }
                    local ok = pcall(next, t, 'missing')
                    return tostring(ok)
                end

                local outcome = check()
                print(outcome)
                return outcome
            "
            );

            await Assert.That(result.String).IsEqualTo("false").ConfigureAwait(false);
        }
    }
}
