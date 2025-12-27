namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Descriptors
{
    using System;
    using System.CodeDom;
    using System.Linq;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Hardwire;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Tests.TestUtilities;
    using WallstopStudios.NovaSharp.Interpreter.Tests.Units;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    public sealed class HardwireCodeGenerationContextTUnitTests
    {
        private static readonly string[] ExpectedErrors = { "Failure 1" };
        private static readonly string[] ExpectedWarnings = { "Warning 2" };
        private static readonly string[] ExpectedMinorMessages = { "Note 3" };

        static HardwireCodeGenerationContextTUnitTests()
        {
            _ = new RecordingHardwireGenerator();
        }

        [global::TUnit.Core.Test]
        public async Task IsVisibilityAcceptedHonorsAllowInternalsFlag()
        {
            CapturingCodeGenerationLogger logger = new();
            HardwireCodeGenerationContext context = HardwireTestUtilities.CreateContext(logger);
            Table table = new(owner: null);

            table.Set("visibility", DynValue.NewString("internal"));
            await Assert.That(context.IsVisibilityAccepted(table)).IsFalse().ConfigureAwait(false);

            context.AllowInternals = true;
            await Assert.That(context.IsVisibilityAccepted(table)).IsTrue().ConfigureAwait(false);

            table.Set("visibility", DynValue.NewString("protected-internal"));
            await Assert.That(context.IsVisibilityAccepted(table)).IsTrue().ConfigureAwait(false);

            table.Set("visibility", DynValue.NewString("private"));
            await Assert.That(context.IsVisibilityAccepted(table)).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task IsVisibilityAcceptedTreatsNonStringAsAccepted()
        {
            HardwireCodeGenerationContext context = HardwireTestUtilities.CreateContext();
            Table table = new(owner: null);

            await Assert.That(context.IsVisibilityAccepted(table)).IsTrue().ConfigureAwait(false);

            table.Set("visibility", DynValue.NewNumber(42));
            await Assert.That(context.IsVisibilityAccepted(table)).IsTrue().ConfigureAwait(false);

            table.Set("visibility", DynValue.NewString("public"));
            await Assert.That(context.IsVisibilityAccepted(table)).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ErrorWarningAndMinorForwardToLogger()
        {
            CapturingCodeGenerationLogger logger = new();
            HardwireCodeGenerationContext context = HardwireTestUtilities.CreateContext(logger);

            context.Error("Failure {0}", 1);
            context.Warning("Warning {0}", 2);
            context.Minor("Note {0}", 3);

            await Assert
                .That(logger.Errors.SequenceEqual(ExpectedErrors))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(logger.Warnings.SequenceEqual(ExpectedWarnings))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(logger.MinorMessages.SequenceEqual(ExpectedMinorMessages))
                .IsTrue()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TimestampCommentComesFromProvidedTimeProvider()
        {
            DateTimeOffset timestamp = new(new DateTime(2030, 1, 2, 3, 4, 5, DateTimeKind.Utc));
            FakeTimeProvider provider = new(timestamp);
            HardwireCodeGenerationContext context = HardwireTestUtilities.CreateContext(
                timeProvider: provider
            );

            CodeNamespace ns = context.CompileUnit.Namespaces.Cast<CodeNamespace>().Single();
            string expected = $"Code generated on {timestamp.UtcDateTime:O}";
            bool contains = ns
                .Comments.Cast<CodeCommentStatement>()
                .Select(c => c.Comment.Text)
                .Contains(expected);
            await Assert.That(contains).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DispatchTableThrowsWhenClassEntryMissing()
        {
            HardwireCodeGenerationContext context = HardwireTestUtilities.CreateContext();
            Table descriptor = new(owner: null);

            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                context.DispatchTable("key", descriptor, new CodeTypeMemberCollection())
            );
            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GenerateCodeInvokesRegisteredGenerators()
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

            await Assert.That(generator.InvocationCount).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(generator.LastKey).IsEqualTo("Example").ConfigureAwait(false);

            CodeNamespace ns = context.CompileUnit.Namespaces.Cast<CodeNamespace>().Single();
            CodeTypeDeclaration kickstarter = ns.Types.Cast<CodeTypeDeclaration>().Single();
            CodeMemberMethod initialize = kickstarter
                .Members.OfType<CodeMemberMethod>()
                .Single(m => m.Name == "Initialize");

            await Assert.That(initialize.Statements.Count).IsEqualTo(1).ConfigureAwait(false);
            CodeExpressionStatement expressionStatement = initialize
                .Statements.OfType<CodeExpressionStatement>()
                .Single();

            CodeMethodInvokeExpression invocation = (CodeMethodInvokeExpression)
                expressionStatement.Expression;

            await Assert
                .That(invocation.Method.MethodName)
                .IsEqualTo("RegisterType")
                .ConfigureAwait(false);
            await Assert.That(invocation.Parameters.Count).IsEqualTo(1).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DispatchTablePairsThrowsWhenTableNull()
        {
            HardwireCodeGenerationContext context = HardwireTestUtilities.CreateContext();

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                context.DispatchTablePairs(null, new CodeTypeMemberCollection())
            );
            await Assert.That(exception.ParamName).IsEqualTo("table").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task DispatchTablePairsThrowsWhenMembersNull(LuaCompatibilityVersion version)
        {
            HardwireCodeGenerationContext context = HardwireTestUtilities.CreateContext();
            Table table = new(new Script(version));

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                context.DispatchTablePairs(table, null)
            );
            await Assert.That(exception.ParamName).IsEqualTo("members").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task DispatchTablePairsThrowsWhenActionNullForExpressionOverload(
            LuaCompatibilityVersion version
        )
        {
            HardwireCodeGenerationContext context = HardwireTestUtilities.CreateContext();
            Table table = new(new Script(version));

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                context.DispatchTablePairs(
                    table,
                    new CodeTypeMemberCollection(),
                    (Action<CodeExpression>)null
                )
            );
            await Assert.That(exception.ParamName).IsEqualTo("action").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DispatchTablePairsSkipsEntriesMarkedForSkip()
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

            await Assert.That(generator.InvocationCount).IsEqualTo(0).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DispatchTablePairsLogsWarningWhenVisibilityRejected()
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

            await Assert.That(generator.InvocationCount).IsEqualTo(0).ConfigureAwait(false);
            await Assert.That(logger.Warnings.Count).IsGreaterThan(0).ConfigureAwait(false);
            await Assert.That(logger.Warnings[0]).Contains("Hidden").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task DispatchTablePairsLogsErrorWhenEntryNotTable(
            LuaCompatibilityVersion version
        )
        {
            CapturingCodeGenerationLogger logger = new();
            HardwireCodeGenerationContext context = HardwireTestUtilities.CreateContext(logger);

            Table root = new(new Script(version));
            root.Set("Broken", DynValue.NewString("failure detected"));

            context.DispatchTablePairs(root, new CodeTypeMemberCollection());

            await Assert.That(logger.Errors.Count).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(logger.Errors[0]).Contains("Broken").ConfigureAwait(false);
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
