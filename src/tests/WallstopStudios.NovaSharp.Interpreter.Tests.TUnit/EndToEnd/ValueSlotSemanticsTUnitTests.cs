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
