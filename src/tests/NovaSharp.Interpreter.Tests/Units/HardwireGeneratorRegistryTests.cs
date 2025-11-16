namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.CodeDom;
    using NovaSharp.Hardwire;
    using NovaSharp.Interpreter.DataTypes;
    using NUnit.Framework;

    [TestFixture]
    public sealed class HardwireGeneratorRegistryTests
    {
        [Test]
        public void UnknownGeneratorFallsBackToNullGenerator()
        {
            string typeName = "Hardwire.Tests.Unknown." + Guid.NewGuid().ToString("N");

            IHardwireGenerator generator = HardwireGeneratorRegistry.GetGenerator(typeName);
            Assert.That(generator.ManagedType, Is.EqualTo(typeName));

            CapturingCodeGenerationLogger logger = new();
            HardwireCodeGenerationContext context = HardwireTestUtilities.CreateContext(logger);

            CodeExpression[] expressions = generator.Generate(
                null,
                context,
                new CodeTypeMemberCollection()
            );

            Assert.That(expressions, Is.Empty);
            Assert.That(logger.Errors, Has.Count.EqualTo(1));
            Assert.That(logger.Errors[0], Does.Contain(typeName));
        }

        [Test]
        public void RegisterOverridesExistingGenerator()
        {
            string typeName = "Hardwire.Tests.Custom." + Guid.NewGuid().ToString("N");

            RecordingGenerator first = new(typeName, new CodePrimitiveExpression(1));
            HardwireGeneratorRegistry.Register(first);

            Assert.That(HardwireGeneratorRegistry.GetGenerator(typeName), Is.SameAs(first));

            RecordingGenerator second = new(typeName, new CodePrimitiveExpression(2));
            HardwireGeneratorRegistry.Register(second);

            IHardwireGenerator resolved = HardwireGeneratorRegistry.GetGenerator(typeName);
            Assert.That(resolved, Is.SameAs(second));

            HardwireCodeGenerationContext context = HardwireTestUtilities.CreateContext();
            CodeExpression[] expressions = resolved.Generate(
                null,
                context,
                new CodeTypeMemberCollection()
            );

            Assert.That(expressions, Has.Length.EqualTo(1));
            Assert.That(second.InvocationCount, Is.EqualTo(1));
        }

        [Test]
        public void DiscoverFromAssemblyRegistersGenerators()
        {
            const string managedType =
                "NovaSharp.Interpreter.Interop.StandardDescriptors.MemberDescriptors.DynValueMemberDescriptor";

            HardwireGeneratorRegistry.DiscoverFromAssembly(
                typeof(NovaSharp.Hardwire.Generators.DynValueMemberDescriptorGenerator).Assembly
            );

            IHardwireGenerator generator = HardwireGeneratorRegistry.GetGenerator(managedType);

            Assert.That(
                generator,
                Is.TypeOf<NovaSharp.Hardwire.Generators.DynValueMemberDescriptorGenerator>()
            );
        }

        [Test]
        public void RegisterPredefinedPopulatesBuiltInGenerators()
        {
            HardwireGeneratorRegistry.RegisterPredefined();

            const string methodDescriptorType =
                "NovaSharp.Interpreter.Interop.StandardDescriptors.ReflectionMemberDescriptors.MethodMemberDescriptor";

            IHardwireGenerator generator = HardwireGeneratorRegistry.GetGenerator(
                methodDescriptorType
            );

            Assert.That(generator.ManagedType, Is.EqualTo(methodDescriptorType));
            Assert.That(generator.GetType().Name, Is.EqualTo("MethodMemberDescriptorGenerator"));
        }

        public sealed class RecordingGenerator : IHardwireGenerator
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
