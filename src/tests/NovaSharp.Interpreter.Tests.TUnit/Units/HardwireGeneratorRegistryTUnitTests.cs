namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.CodeDom;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Hardwire;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Tests.Units;

    public sealed class HardwireGeneratorRegistryTUnitTests
    {
        static HardwireGeneratorRegistryTUnitTests()
        {
            _ = new RecordingGenerator();
        }

        [global::TUnit.Core.Test]
        public async Task UnknownGeneratorFallsBackToNullGenerator()
        {
            string typeName = "Hardwire.Tests.Unknown." + Guid.NewGuid().ToString("N");

            IHardwireGenerator generator = HardwireGeneratorRegistry.GetGenerator(typeName);
            await Assert.That(generator.ManagedType).IsEqualTo(typeName).ConfigureAwait(false);

            CapturingCodeGenerationLogger logger = new();
            HardwireCodeGenerationContext context = HardwireTestUtilities.CreateContext(logger);

            CodeExpression[] expressions = generator.Generate(
                null,
                context,
                new CodeTypeMemberCollection()
            );

            await Assert.That(expressions).IsEmpty().ConfigureAwait(false);
            await Assert.That(logger.Errors.Count).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(logger.Errors[0]).Contains(typeName).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RegisterOverridesExistingGenerator()
        {
            string typeName = "Hardwire.Tests.Custom." + Guid.NewGuid().ToString("N");

            RecordingGenerator first = new(typeName, new CodePrimitiveExpression(1));
            HardwireGeneratorRegistry.Register(first);

            await Assert
                .That(HardwireGeneratorRegistry.GetGenerator(typeName))
                .IsSameReferenceAs(first)
                .ConfigureAwait(false);

            RecordingGenerator second = new(typeName, new CodePrimitiveExpression(2));
            HardwireGeneratorRegistry.Register(second);

            IHardwireGenerator resolved = HardwireGeneratorRegistry.GetGenerator(typeName);
            await Assert.That(resolved).IsSameReferenceAs(second).ConfigureAwait(false);

            HardwireCodeGenerationContext context = HardwireTestUtilities.CreateContext();
            CodeExpression[] expressions = resolved.Generate(
                null,
                context,
                new CodeTypeMemberCollection()
            );

            await Assert.That(expressions.Length).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(second.InvocationCount).IsEqualTo(1).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public void RegisterThrowsWhenGeneratorNull()
        {
            Assert.Throws<ArgumentNullException>(() => HardwireGeneratorRegistry.Register(null));
        }

        [global::TUnit.Core.Test]
        public void RegisterThrowsWhenManagedTypeMissing()
        {
            RecordingGenerator generator = new(managedType: "   ");

            Assert.Throws<ArgumentException>(() => HardwireGeneratorRegistry.Register(generator));
        }

        [global::TUnit.Core.Test]
        public void GetGeneratorThrowsWhenTypeNullOrWhitespace()
        {
            Assert.Throws<ArgumentException>(() => HardwireGeneratorRegistry.GetGenerator(null));
            Assert.Throws<ArgumentException>(() => HardwireGeneratorRegistry.GetGenerator("  "));
        }

        [global::TUnit.Core.Test]
        public async Task DiscoverFromAssemblyRegistersGenerators()
        {
            const string managedType =
                "NovaSharp.Interpreter.Interop.StandardDescriptors.MemberDescriptors.DynValueMemberDescriptor";

            HardwireGeneratorRegistry.DiscoverFromAssembly(
                typeof(NovaSharp.Hardwire.Generators.DynValueMemberDescriptorGenerator).Assembly
            );

            IHardwireGenerator generator = HardwireGeneratorRegistry.GetGenerator(managedType);

            await Assert
                .That(generator)
                .IsTypeOf<NovaSharp.Hardwire.Generators.DynValueMemberDescriptorGenerator>()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RegisterPredefinedPopulatesBuiltInGenerators()
        {
            HardwireGeneratorRegistry.RegisterPredefined();

            const string methodDescriptorType =
                "NovaSharp.Interpreter.Interop.StandardDescriptors.ReflectionMemberDescriptors.MethodMemberDescriptor";

            IHardwireGenerator generator = HardwireGeneratorRegistry.GetGenerator(
                methodDescriptorType
            );

            await Assert
                .That(generator.ManagedType)
                .IsEqualTo(methodDescriptorType)
                .ConfigureAwait(false);
            await Assert
                .That(generator.GetType().Name)
                .IsEqualTo("MethodMemberDescriptorGenerator")
                .ConfigureAwait(false);
        }

        private sealed class RecordingGenerator : IHardwireGenerator
        {
            private readonly string _managedType;
            private readonly CodeExpression[] _expressions;

            public RecordingGenerator(string managedType, params CodeExpression[] expressions)
            {
                _managedType = managedType;
                _expressions = expressions ?? Array.Empty<CodeExpression>();
            }

            public RecordingGenerator()
                : this("NovaSharp.Tests.Default") { }

            public int InvocationCount { get; private set; }

            public string ManagedType => _managedType;

            public CodeExpression[] Generate(
                Table table,
                HardwireCodeGenerationContext generatorContext,
                CodeTypeMemberCollection members
            )
            {
                InvocationCount++;
                return _expressions;
            }
        }
    }
}
