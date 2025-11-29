#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter.Execution.Scopes;
    using NovaSharp.Interpreter.Tree.Statements;

    public sealed class LabelStatementTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task BuildExitScopesReturnsEmptyWhenEitherBlockIsNull()
        {
            BuildTimeScopeBlock labelBlock = new(parent: null);
            BuildTimeScopeBlock gotoBlock = new(parent: null);

            await Assert.That(LabelStatement.BuildExitScopes(null, labelBlock).Count).IsEqualTo(0);
            await Assert.That(LabelStatement.BuildExitScopes(gotoBlock, null).Count).IsEqualTo(0);
        }

        [global::TUnit.Core.Test]
        public async Task BuildExitScopesReturnsRuntimeScopesUntilLabel()
        {
            BuildTimeScopeBlock labelBlock = new(parent: null);
            BuildTimeScopeBlock child = labelBlock.AddChild();
            BuildTimeScopeBlock grandChild = child.AddChild();

            List<RuntimeScopeBlock> scopes = LabelStatement.BuildExitScopes(grandChild, labelBlock);

            await Assert.That(scopes.Count).IsEqualTo(2);
            await Assert.That(scopes[0]).IsSameReferenceAs(grandChild.ScopeBlock);
            await Assert.That(scopes[1]).IsSameReferenceAs(child.ScopeBlock);
        }

        [global::TUnit.Core.Test]
        public async Task BuildExitScopesReturnsEmptyWhenLabelNotReachable()
        {
            BuildTimeScopeBlock labelBlock = new(parent: null);
            BuildTimeScopeBlock gotoTree = new(parent: null);
            BuildTimeScopeBlock gotoBlock = gotoTree.AddChild();

            List<RuntimeScopeBlock> scopes = LabelStatement.BuildExitScopes(gotoBlock, labelBlock);

            await Assert.That(scopes.Count).IsEqualTo(0);
        }
    }
}
#pragma warning restore CA2007
