namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.EndToEnd
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;

    public sealed class GotoTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task GotoSimpleForwardJump()
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

            DynValue result = Script.RunString(code);
            await EndToEndDynValueAssert.ExpectAsync(result, 3).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GotoSimpleBackwardJump()
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

            DynValue result = Script.RunString(code);
            await EndToEndDynValueAssert.ExpectAsync(result, 3).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GotoUndefinedLabelThrows()
        {
            string code = "goto there";
            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
            {
                Script.RunString(code);
            });

            await Assert
                .That(exception.DecoratedMessage)
                .Contains("no visible label")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GotoDoubleDefinedLabelThrows()
        {
            string code =
                @"
                ::label::
                ::label::
                ";
            _ = Assert.Throws<SyntaxErrorException>(() => Script.RunString(code));
            await Task.CompletedTask.ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GotoSupportsRedeclaringInsideBlock()
        {
            string code =
                @"
                ::label::
                do
                    ::label::
                end
                ";

            Script.RunString(code);
            await Task.CompletedTask.ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GotoRedeclaredLabelAllowsJump()
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

            DynValue result = Script.RunString(code);
            await EndToEndDynValueAssert.ExpectAsync(result, 3).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GotoUndefinedInnerLabelThrows()
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

            _ = Assert.Throws<SyntaxErrorException>(() => Script.RunString(code));
            await Task.CompletedTask.ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GotoCannotJumpOverLocalDeclarations()
        {
            string code =
                @"
                goto f
                local x
                ::f::
                ";

            _ = Assert.Throws<SyntaxErrorException>(() => Script.RunString(code));
            await Task.CompletedTask.ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GotoJumpOutOfBlocksReturnsValue()
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

            DynValue result = Script.RunString(code);
            await EndToEndDynValueAssert.ExpectAsync(result, 3).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GotoJumpOutOfScopesPreservesVariables()
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

            DynValue result = Script.RunString(code);
            await EndToEndDynValueAssert.ExpectAsync(result, 67).ConfigureAwait(false);
        }
    }
}
