namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Tree.Statements
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter.Execution.Scopes;
    using WallstopStudios.NovaSharp.Interpreter.Tree.Statements;

    public sealed class LabelStatementTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task BuildExitScopesReturnsEmptyWhenEitherBlockIsNull()
        {
            BuildTimeScopeBlock labelBlock = new(parent: null);
            BuildTimeScopeBlock gotoBlock = new(parent: null);

            await Assert
                .That(LabelStatement.BuildExitScopes(null, labelBlock).Count)
                .IsEqualTo(0)
                .ConfigureAwait(false);
            await Assert
                .That(LabelStatement.BuildExitScopes(gotoBlock, null).Count)
                .IsEqualTo(0)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task BuildExitScopesReturnsRuntimeScopesUntilLabel()
        {
            BuildTimeScopeBlock labelBlock = new(parent: null);
            BuildTimeScopeBlock child = labelBlock.AddChild();
            BuildTimeScopeBlock grandChild = child.AddChild();

            List<RuntimeScopeBlock> scopes = LabelStatement.BuildExitScopes(grandChild, labelBlock);

            await Assert.That(scopes.Count).IsEqualTo(2).ConfigureAwait(false);
            await Assert
                .That(scopes[0])
                .IsSameReferenceAs(grandChild.ScopeBlock)
                .ConfigureAwait(false);
            await Assert.That(scopes[1]).IsSameReferenceAs(child.ScopeBlock).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task BuildExitScopesReturnsEmptyWhenLabelNotReachable()
        {
            BuildTimeScopeBlock labelBlock = new(parent: null);
            BuildTimeScopeBlock gotoTree = new(parent: null);
            BuildTimeScopeBlock gotoBlock = gotoTree.AddChild();

            List<RuntimeScopeBlock> scopes = LabelStatement.BuildExitScopes(gotoBlock, labelBlock);

            await Assert.That(scopes.Count).IsEqualTo(0).ConfigureAwait(false);
        }
    }
}
