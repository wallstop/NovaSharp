namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.EndToEnd
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    /// <summary>
    /// Tests for the goto statement, which was introduced in Lua 5.2.
    /// All tests target Lua 5.2+ only.
    /// </summary>
    public sealed class GotoTUnitTests
    {
        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task GotoSimpleForwardJump(LuaCompatibilityVersion version)
        {
            string code =
                @"
                function test()
                    x = 3
                    goto skip
                    x = x + 2
                    ::skip::
                    return x
                end
                return test()
                ";

            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(code);
            await EndToEndDynValueAssert.ExpectAsync(result, 3).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task GotoSimpleBackwardJump(LuaCompatibilityVersion version)
        {
            string code =
                @"
                function test()
                    x = 5
                    ::jump::
                    if (x == 3) then return x end
                    x = 3
                    goto jump
                end
                return test()
                ";

            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(code);
            await EndToEndDynValueAssert.ExpectAsync(result, 3).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task GotoUndefinedLabelThrows(LuaCompatibilityVersion version)
        {
            string code = "goto there";
            Script script = new Script(version, CoreModulePresets.Complete);
            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
            {
                script.DoString(code);
            });

            await Assert
                .That(exception.DecoratedMessage)
                .Contains("no visible label")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task GotoDoubleDefinedLabelThrows(LuaCompatibilityVersion version)
        {
            string code =
                @"
                ::label::
                ::label::
                ";
            Script script = new Script(version, CoreModulePresets.Complete);
            _ = Assert.Throws<SyntaxErrorException>(() => script.DoString(code));
            await Task.CompletedTask.ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task GotoSupportsRedeclaringInsideBlock(LuaCompatibilityVersion version)
        {
            string code =
                @"
                ::label::
                do
                    ::label::
                end
                ";

            Script script = new Script(version, CoreModulePresets.Complete);
            script.DoString(code);
            await Task.CompletedTask.ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task GotoRedeclaredLabelAllowsJump(LuaCompatibilityVersion version)
        {
            string code =
                @"
                ::label::
                do
                    goto label
                    do return 5 end
                    ::label::
                    return 3
                end
                ";

            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(code);
            await EndToEndDynValueAssert.ExpectAsync(result, 3).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task GotoUndefinedInnerLabelThrows(LuaCompatibilityVersion version)
        {
            string code =
                @"
                goto label
                do
                    do return 5 end
                    ::label::
                    return 3
                end
                ";

            Script script = new Script(version, CoreModulePresets.Complete);
            _ = Assert.Throws<SyntaxErrorException>(() => script.DoString(code));
            await Task.CompletedTask.ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task GotoCanJumpOverLocalWhenLabelAtEndOfBlock(LuaCompatibilityVersion version)
        {
            // Per Lua 5.4 §3.5: "The scope of a local variable begins at the first statement
            // after its declaration and lasts until the last non-void statement of the
            // innermost block that includes the declaration. Void statements are labels
            // and empty statements."
            //
            // This means when a label is at the end of a block (followed only by void
            // statements), locals declared before the label are NOT in scope at the label,
            // so the goto does NOT "jump into scope".
            string code =
                @"
                goto f
                local x = 1
                ::f::
                ";

            Script script = new Script(version, CoreModulePresets.Complete);
            script.DoString(code); // Should not throw
            await Task.CompletedTask.ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task GotoCannotJumpOverLocalDeclarations(LuaCompatibilityVersion version)
        {
            // Note: Per Lua 5.4 §3.5, if the label is at the end of a block (followed only by
            // void statements like other labels or semicolons), then locals declared before
            // the label are NOT in scope at the label. This code adds a statement after the
            // label to ensure the label is NOT at the end of the block.
            string code =
                @"
                goto f
                local x
                ::f::
                print(x)
                ";

            Script script = new Script(version, CoreModulePresets.Complete);
            _ = Assert.Throws<SyntaxErrorException>(() => script.DoString(code));
            await Task.CompletedTask.ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task GotoJumpOutOfBlocksReturnsValue(LuaCompatibilityVersion version)
        {
            string code =
                @"
                local u = 4
                do
                    local x = 5
                    do
                        local y = 6
                        do
                            local z = 7
                        end
                        goto out
                    end
                end
                do return 5 end
                ::out::
                return 3
                ";

            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(code);
            await EndToEndDynValueAssert.ExpectAsync(result, 3).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task GotoJumpOutOfScopesPreservesVariables(LuaCompatibilityVersion version)
        {
            string code =
                @"
                local u = 4
                do
                    local x = 5
                    do
                        local y = 6
                        do
                            goto out
                            local z = 7
                        end
                    end
                end
                ::out::
                do
                    local a
                    local b = 55
                    if (a == nil) then
                        b = b + 12
                    end
                    return b
                end
                ";

            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(code);
            await EndToEndDynValueAssert.ExpectAsync(result, 67).ConfigureAwait(false);
        }
    }
}
