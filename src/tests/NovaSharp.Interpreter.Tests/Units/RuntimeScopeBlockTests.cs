namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution.Scopes;
    using NUnit.Framework;

    [TestFixture]
    public sealed class RuntimeScopeBlockTests
    {
        [Test]
        public void DefaultsUseEmptyToBeClosedArray()
        {
            RuntimeScopeBlock block = new();
            Assert.That(block.ToBeClosed, Is.Not.Null);
            Assert.That(block.ToBeClosed, Is.Empty);
        }

        [Test]
        public void PropertiesAreMutableInsideTests()
        {
            RuntimeScopeBlock block = new()
            {
                From = 10,
                To = 20,
                ToInclusive = 30,
                ToBeClosed = new[] { SymbolRef.Local("value", 0) },
            };

            Assert.Multiple(() =>
            {
                Assert.That(block.From, Is.EqualTo(10));
                Assert.That(block.To, Is.EqualTo(20));
                Assert.That(block.ToInclusive, Is.EqualTo(30));
                Assert.That(block.ToBeClosed, Has.Length.EqualTo(1));
                Assert.That(block.ToBeClosed[0].Name, Is.EqualTo("value"));
            });
        }

        [Test]
        public void ToStringPrintsRangeInformation()
        {
            RuntimeScopeBlock block = new()
            {
                From = 1,
                To = 2,
                ToInclusive = 3,
            };

            Assert.That(block.ToString(), Is.EqualTo("ScopeBlock : 1 -> 2 --> 3"));
        }
    }
}
