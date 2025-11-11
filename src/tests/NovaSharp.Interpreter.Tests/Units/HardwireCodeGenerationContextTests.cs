namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp.Hardwire;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NUnit.Framework;

    [TestFixture]
    public sealed class HardwireCodeGenerationContextTests
    {
        [Test]
        public void IsVisibilityAcceptedHonorsAllowInternalsFlag()
        {
            CapturingCodeGenerationLogger logger = new();
            HardwireCodeGenerationContext context = HardwireTestUtilities.CreateContext(logger);
            Table table = new(owner: null);

            table.Set("visibility", DynValue.NewString("internal"));
            Assert.That(context.IsVisibilityAccepted(table), Is.False);

            context.AllowInternals = true;
            Assert.That(context.IsVisibilityAccepted(table), Is.True);

            table.Set("visibility", DynValue.NewString("protected-internal"));
            Assert.That(context.IsVisibilityAccepted(table), Is.True);

            table.Set("visibility", DynValue.NewString("private"));
            Assert.That(context.IsVisibilityAccepted(table), Is.False);
        }

        [Test]
        public void IsVisibilityAcceptedTreatsNonStringAsAccepted()
        {
            HardwireCodeGenerationContext context = HardwireTestUtilities.CreateContext();
            Table table = new(owner: null);

            Assert.That(context.IsVisibilityAccepted(table), Is.True);

            table.Set("visibility", DynValue.NewNumber(42));
            Assert.That(context.IsVisibilityAccepted(table), Is.True);

            table.Set("visibility", DynValue.NewString("public"));
            Assert.That(context.IsVisibilityAccepted(table), Is.True);
        }

        [Test]
        public void ErrorWarningAndMinorForwardToLogger()
        {
            CapturingCodeGenerationLogger logger = new();
            HardwireCodeGenerationContext context = HardwireTestUtilities.CreateContext(logger);

            context.Error("Failure {0}", 1);
            context.Warning("Warning {0}", 2);
            context.Minor("Note {0}", 3);

            Assert.That(logger.Errors, Is.EqualTo(new[] { "Failure 1" }));
            Assert.That(logger.Warnings, Is.EqualTo(new[] { "Warning 2" }));
            Assert.That(logger.MinorMessages, Is.EqualTo(new[] { "Note 3" }));
        }
    }
}
