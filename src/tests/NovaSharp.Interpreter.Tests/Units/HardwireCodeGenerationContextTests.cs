namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.CodeDom;
    using System.Linq;
    using NovaSharp.Hardwire;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Tests.TestUtilities;
    using NUnit.Framework;

    [TestFixture]
    public sealed class HardwireCodeGenerationContextTests
    {
        private static readonly string[] ExpectedErrors = { "Failure 1" };
        private static readonly string[] ExpectedWarnings = { "Warning 2" };
        private static readonly string[] ExpectedMinorMessages = { "Note 3" };

        static HardwireCodeGenerationContextTests()
        {
            _ = new RecordingHardwireGenerator();
        }

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

            Assert.That(logger.Errors, Is.EqualTo(ExpectedErrors));
            Assert.That(logger.Warnings, Is.EqualTo(ExpectedWarnings));
            Assert.That(logger.MinorMessages, Is.EqualTo(ExpectedMinorMessages));
        }

        [Test]
        public void TimestampCommentComesFromProvidedTimeProvider()
        {
            DateTimeOffset timestamp = new(new DateTime(2030, 1, 2, 3, 4, 5, DateTimeKind.Utc));
            FakeTimeProvider provider = new(timestamp);
            HardwireCodeGenerationContext context = HardwireTestUtilities.CreateContext(
                timeProvider: provider
            );

            CodeNamespace ns = context.CompileUnit.Namespaces.Cast<CodeNamespace>().Single();
            string expected = $"Code generated on {timestamp.UtcDateTime:O}";
            Assert.That(
                ns.Comments.Cast<CodeCommentStatement>().Select(c => c.Comment.Text),
                Does.Contain(expected)
            );
        }

        [Test]
        public void DispatchTableThrowsWhenClassEntryMissing()
        {
            HardwireCodeGenerationContext context = HardwireTestUtilities.CreateContext();
            Table descriptor = new(owner: null);

            Assert.That(
                () => context.DispatchTable("key", descriptor, new CodeTypeMemberCollection()),
                Throws.ArgumentException
            );
        }

        [Test]
        public void GenerateCodeInvokesRegisteredGenerators()
        {
            string managedType = "Hardwire.Tests.Context." + Guid.NewGuid().ToString("N");
            RecordingHardwireGenerator generator = new(managedType);
            HardwireGeneratorRegistry.Register(generator);

            HardwireCodeGenerationContext context = HardwireTestUtilities.CreateContext();
            Table root = new(owner: null);
            Table descriptor = new(owner: null);
            descriptor.Set("class", DynValue.NewString(managedType));
            root.Set("Example", DynValue.NewTable(descriptor));

            context.GenerateCode(root);

            Assert.That(generator.InvocationCount, Is.EqualTo(1));
            Assert.That(generator.LastKey, Is.EqualTo("Example"));

            CodeNamespace ns = context.CompileUnit.Namespaces.Cast<CodeNamespace>().Single();
            CodeTypeDeclaration kickstarter = ns.Types.Cast<CodeTypeDeclaration>().Single();
            CodeMemberMethod initialize = kickstarter
                .Members.OfType<CodeMemberMethod>()
                .Single(m => m.Name == "Initialize");

            Assert.That(initialize.Statements, Has.Count.EqualTo(1));
            CodeExpressionStatement expressionStatement = initialize
                .Statements.OfType<CodeExpressionStatement>()
                .Single();

            CodeMethodInvokeExpression invocation = (CodeMethodInvokeExpression)
                expressionStatement.Expression;

            Assert.That(invocation.Method.MethodName, Is.EqualTo("RegisterType"));
            Assert.That(invocation.Parameters, Has.Count.EqualTo(1));
        }

        [Test]
        public void DispatchTablePairsThrowsWhenTableNull()
        {
            HardwireCodeGenerationContext context = HardwireTestUtilities.CreateContext();

            Assert.That(
                () => context.DispatchTablePairs(null, new CodeTypeMemberCollection()),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("table")
            );
        }

        [Test]
        public void DispatchTablePairsThrowsWhenMembersNull()
        {
            HardwireCodeGenerationContext context = HardwireTestUtilities.CreateContext();
            Table table = new(new Script());

            Assert.That(
                () => context.DispatchTablePairs(table, null),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("members")
            );
        }

        [Test]
        public void DispatchTablePairsThrowsWhenActionNullForExpressionOverload()
        {
            HardwireCodeGenerationContext context = HardwireTestUtilities.CreateContext();
            Table table = new(new Script());

            Assert.That(
                () =>
                    context.DispatchTablePairs(
                        table,
                        new CodeTypeMemberCollection(),
                        (Action<CodeExpression>)null
                    ),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("action")
            );
        }

        [Test]
        public void DispatchTablePairsSkipsEntriesMarkedForSkip()
        {
            string managedType = "Hardwire.Tests.Skip." + Guid.NewGuid().ToString("N");
            RecordingHardwireGenerator generator = new(managedType);
            HardwireGeneratorRegistry.Register(generator);

            Script script = new();
            Table descriptor = new(script);
            descriptor.Set("class", DynValue.NewString(managedType));
            descriptor.Set("skip", DynValue.True);

            Table root = new(script);
            root.Set("SkipMe", DynValue.NewTable(descriptor));

            HardwireCodeGenerationContext context = HardwireTestUtilities.CreateContext();
            context.DispatchTablePairs(root, new CodeTypeMemberCollection());

            Assert.That(generator.InvocationCount, Is.EqualTo(0));
        }

        [Test]
        public void DispatchTablePairsLogsWarningWhenVisibilityRejected()
        {
            CapturingCodeGenerationLogger logger = new();
            HardwireCodeGenerationContext context = HardwireTestUtilities.CreateContext(logger);
            string managedType = "Hardwire.Tests.Visibility." + Guid.NewGuid().ToString("N");
            RecordingHardwireGenerator generator = new(managedType);
            HardwireGeneratorRegistry.Register(generator);

            Script script = new();
            Table descriptor = new(script);
            descriptor.Set("class", DynValue.NewString(managedType));
            descriptor.Set("visibility", DynValue.NewString("private"));

            Table root = new(script);
            root.Set("Hidden", DynValue.NewTable(descriptor));

            context.DispatchTablePairs(root, new CodeTypeMemberCollection());

            Assert.Multiple(() =>
            {
                Assert.That(generator.InvocationCount, Is.EqualTo(0));
                Assert.That(logger.Warnings, Is.Not.Empty);
                Assert.That(logger.Warnings[0], Does.Contain("Hidden"));
            });
        }

        [Test]
        public void DispatchTablePairsLogsErrorWhenEntryNotTable()
        {
            CapturingCodeGenerationLogger logger = new();
            HardwireCodeGenerationContext context = HardwireTestUtilities.CreateContext(logger);

            Table root = new(new Script());
            root.Set("Broken", DynValue.NewString("failure detected"));

            context.DispatchTablePairs(root, new CodeTypeMemberCollection());

            Assert.That(logger.Errors, Has.Count.EqualTo(1));
            Assert.That(logger.Errors[0], Does.Contain("Broken"));
        }

        private sealed class RecordingHardwireGenerator : IHardwireGenerator
        {
            private readonly string _managedType;

            internal RecordingHardwireGenerator(string managedType)
            {
                _managedType = managedType;
            }

            public RecordingHardwireGenerator()
                : this("NovaSharp.Tests.Context.Default") { }

            public int InvocationCount { get; private set; }

            public string LastKey { get; private set; }

            public string ManagedType => _managedType;

            public CodeExpression[] Generate(
                Table table,
                HardwireCodeGenerationContext generatorContext,
                CodeTypeMemberCollection members
            )
            {
                InvocationCount++;
                LastKey = table?.Get("$key")?.String;
                return new CodeExpression[] { new CodeTypeOfExpression(typeof(object)) };
            }
        }
    }
}
