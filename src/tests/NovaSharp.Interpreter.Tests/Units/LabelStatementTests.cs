namespace NovaSharp.Interpreter.Tests.Units
{
    using System.Collections.Generic;
    using NovaSharp.Interpreter.Execution.Scopes;
    using NovaSharp.Interpreter.Tree.Statements;
    using NUnit.Framework;

    [TestFixture]
    public sealed class LabelStatementTests
    {
        [Test]
        public void BuildExitScopesReturnsEmptyWhenEitherBlockIsNull()
        {
            BuildTimeScopeBlock labelBlock = new(parent: null);
            BuildTimeScopeBlock gotoBlock = new(parent: null);

            Assert.That(InvokeBuildExitScopes(null, labelBlock), Is.Empty);
            Assert.That(InvokeBuildExitScopes(gotoBlock, null), Is.Empty);
        }

        [Test]
        public void BuildExitScopesReturnsRuntimeScopesUntilLabel()
        {
            BuildTimeScopeBlock labelBlock = new(parent: null);
            BuildTimeScopeBlock child = labelBlock.AddChild();
            BuildTimeScopeBlock grandChild = child.AddChild();

            List<RuntimeScopeBlock> scopes = LabelStatement.BuildExitScopes(grandChild, labelBlock);

            Assert.Multiple(() =>
            {
                Assert.That(scopes, Has.Count.EqualTo(2));
                Assert.That(scopes[0], Is.SameAs(grandChild.ScopeBlock));
                Assert.That(scopes[1], Is.SameAs(child.ScopeBlock));
            });
        }

        [Test]
        public void BuildExitScopesReturnsEmptyWhenLabelIsNotReachable()
        {
            BuildTimeScopeBlock labelBlock = new(parent: null);
            BuildTimeScopeBlock gotoTree = new(parent: null);
            BuildTimeScopeBlock gotoBlock = gotoTree.AddChild();

            List<RuntimeScopeBlock> scopes = LabelStatement.BuildExitScopes(gotoBlock, labelBlock);

            Assert.That(scopes, Is.Empty);
        }

        private static List<RuntimeScopeBlock> InvokeBuildExitScopes(
            BuildTimeScopeBlock gotoBlock,
            BuildTimeScopeBlock labelBlock
        ) => LabelStatement.BuildExitScopes(gotoBlock, labelBlock);
    }
}
