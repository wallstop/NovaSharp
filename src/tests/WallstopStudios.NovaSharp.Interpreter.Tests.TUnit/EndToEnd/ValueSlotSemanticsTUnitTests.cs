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
