namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.CodeDom;
    using System.Collections.Generic;
    using System.Linq;
    using NovaSharp.Hardwire;
    using NovaSharp.Hardwire.Generators;
    using NovaSharp.Hardwire.Languages;
    using NovaSharp.Interpreter.DataTypes;
    using NUnit.Framework;

    [TestFixture]
    public sealed class HardwireGeneratorTests
    {
        [Test]
        public void BuildCodeModelRegistersCodeExpressions()
        {
            string managedType = "Hardwire.Tests.Generator." + Guid.NewGuid().ToString("N");
            RegisteringGenerator.Reset();
            HardwireGeneratorRegistry.Register(new RegisteringGenerator(managedType));

            string source = GenerateSourceFor(managedType);

            Assert.That(RegisteringGenerator.InvocationCount, Is.EqualTo(1));
            Assert.That(source, Does.Contain("UserData.RegisterType(typeof(object));"));
        }

        [Test]
        public void AllowInternalsFlagPropagatesToGeneratorContext()
        {
            string managedType = "Hardwire.Tests.AllowInternals." + Guid.NewGuid().ToString("N");
            AllowInternalsProbeGenerator.Reset();
            HardwireGeneratorRegistry.Register(new AllowInternalsProbeGenerator(managedType));

            HardwireGenerator generator = CreateGenerator();
            generator.AllowInternals = true;

            BuildCodeModel(generator, managedType);

            Assert.That(AllowInternalsProbeGenerator.AllowInternalsSeen, Is.True);
        }

        [Test]
        public void MethodMemberDescriptorGeneratorEmitsDefaultDispatchAndRefHandling()
        {
            MethodMemberDescriptorGenerator methodGenerator = new();
            HardwireGeneratorRegistry.Register(methodGenerator);

            HardwireGenerator generator = CreateGenerator();
            Table descriptor = CreateMethodDescriptor(
                methodGenerator.ManagedType,
                declType: "NovaSharp.Tests.RefHolder",
                returnType: typeof(object).FullName,
                isStatic: false,
                isSpecial: false,
                parameters: new[]
                {
                    CreateParameter("instance", typeof(int).FullName, isRef: true),
                    CreateParameter("count", typeof(int).FullName, hasDefault: true),
                }
            );

            Table root = new(owner: null);
            root.Set("RefHolder", DynValue.NewTable(descriptor));

            generator.BuildCodeModel(root);
            string source = generator.GenerateSourceCode();

            Assert.That(source, Does.Contain("refp_0"));
            Assert.That(source, Does.Contain("argscount <= 1"));
            Assert.That(source, Does.Contain("DynValue.NewTuple"));
        }

        [Test]
        public void MethodMemberDescriptorGeneratorHandlesPropertySetterSpecialName()
        {
            MethodMemberDescriptorGenerator methodGenerator = new();
            HardwireGeneratorRegistry.Register(methodGenerator);

            HardwireGenerator generator = CreateGenerator();
            Table descriptor = CreateMethodDescriptor(
                methodGenerator.ManagedType,
                declType: "NovaSharp.Tests.PropertyHolder",
                returnType: typeof(void).FullName,
                isStatic: false,
                isSpecial: true,
                methodName: "set_Value",
                parameters: new[] { CreateParameter("value", typeof(int).FullName) }
            );

            Table root = new(owner: null);
            root.Set("PropertyHolder", DynValue.NewTable(descriptor));

            generator.BuildCodeModel(root);
            string source = generator.GenerateSourceCode();

            Assert.That(source, Does.Contain("PropertyHolder tmp ="));
            Assert.That(source, Does.Contain("tmp.Value = ((int)(pars[0]));"));
            Assert.That(source, Does.Contain("return null;"));
        }

        [Test]
        public void MethodMemberDescriptorGeneratorLogsErrorForStaticIndexer()
        {
            MethodMemberDescriptorGenerator methodGenerator = new();
            HardwireGeneratorRegistry.Register(methodGenerator);

            CapturingCodeGenerationLogger logger = new();
            HardwireGenerator generator = new HardwireGenerator(
                "NovaSharp.Tests.Generated.StaticIndexer",
                "EntryPoint",
                logger,
                HardwireCodeGenerationLanguage.CSharp
            );

            Table descriptor = CreateMethodDescriptor(
                methodGenerator.ManagedType,
                declType: "NovaSharp.Tests.StaticIndexerHolder",
                returnType: typeof(int).FullName,
                isStatic: true,
                isSpecial: true,
                methodName: "get_Item",
                parameters: new[] { CreateParameter("index", typeof(int).FullName) }
            );

            Table root = new(owner: null);
            root.Set("StaticIndexerHolder", DynValue.NewTable(descriptor));

            generator.BuildCodeModel(root);
            string source = generator.GenerateSourceCode();

            Assert.That(source, Does.Contain("ERROR"));
            Assert.That(
                logger.Errors.Any(error =>
                    error.Contains("Static indexers are not supported by hardwired descriptors.")
                ),
                Is.True,
                "Expected static indexer error to be logged."
            );
        }

        private static string GenerateSourceFor(string managedType)
        {
            HardwireGenerator generator = CreateGenerator();
            BuildCodeModel(generator, managedType);
            return generator.GenerateSourceCode();
        }

        private static HardwireGenerator CreateGenerator()
        {
            return new HardwireGenerator(
                "NovaSharp.Tests.Generated",
                "EntryPoint",
                new CapturingCodeGenerationLogger(),
                HardwireCodeGenerationLanguage.CSharp
            );
        }

        private static void BuildCodeModel(HardwireGenerator generator, string managedType)
        {
            Table descriptor = new(owner: null);
            descriptor.Set("class", DynValue.NewString(managedType));

            Table root = new(owner: null);
            root.Set("SampleType", DynValue.NewTable(descriptor));

            generator.BuildCodeModel(root);
        }

        private static Table CreateMethodDescriptor(
            string managedType,
            string declType,
            string returnType,
            bool isStatic,
            bool isSpecial,
            string methodName = "Invoke",
            Table[] parameters = null
        )
        {
            Table methodTable = new(owner: null);
            methodTable.Set("class", DynValue.NewString(managedType));
            methodTable.Set("name", DynValue.NewString(methodName));
            methodTable.Set("decltype", DynValue.NewString(declType));
            methodTable.Set("ret", DynValue.NewString(returnType));
            methodTable.Set("static", DynValue.NewBoolean(isStatic));
            methodTable.Set("extension", DynValue.NewBoolean(false));
            methodTable.Set("ctor", DynValue.NewBoolean(false));
            methodTable.Set("special", DynValue.NewBoolean(isSpecial));
            methodTable.Set("arraytype", DynValue.NewNil());

            Table paramsTable = new(owner: null);
            if (parameters != null)
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    paramsTable.Set(i + 1, DynValue.NewTable(parameters[i]));
                }
            }
            methodTable.Set("params", DynValue.NewTable(paramsTable));

            return methodTable;
        }

        private static Table CreateParameter(
            string name,
            string typeName,
            bool hasDefault = false,
            bool isRef = false,
            bool isOut = false
        )
        {
            Table table = new(owner: null);
            table.Set("name", DynValue.NewString(name));
            table.Set("origtype", DynValue.NewString(typeName));
            table.Set("default", DynValue.NewBoolean(hasDefault));
            table.Set("out", DynValue.NewBoolean(isOut));
            table.Set("ref", DynValue.NewBoolean(isRef));
            table.Set("varargs", DynValue.NewBoolean(false));
            table.Set("type", DynValue.NewString(typeName));
            table.Set("restricted", DynValue.NewBoolean(false));
            return table;
        }

        private sealed class RegisteringGenerator : IHardwireGenerator
        {
            private readonly string _managedType;

            internal RegisteringGenerator(string managedType)
            {
                _managedType = managedType;
                InvocationCount = 0;
            }

            internal static int InvocationCount { get; private set; }

            public string ManagedType => _managedType;

            public CodeExpression[] Generate(
                Table table,
                HardwireCodeGenerationContext generatorContext,
                CodeTypeMemberCollection members
            )
            {
                InvocationCount++;
                return new CodeExpression[] { new CodeTypeOfExpression(typeof(object)) };
            }

            internal static void Reset()
            {
                InvocationCount = 0;
            }
        }

        private sealed class AllowInternalsProbeGenerator : IHardwireGenerator
        {
            private readonly string _managedType;

            internal AllowInternalsProbeGenerator(string managedType)
            {
                _managedType = managedType;
            }

            internal static bool AllowInternalsSeen { get; private set; }

            public string ManagedType => _managedType;

            public CodeExpression[] Generate(
                Table table,
                HardwireCodeGenerationContext generatorContext,
                CodeTypeMemberCollection members
            )
            {
                AllowInternalsSeen = generatorContext.AllowInternals;
                return Array.Empty<CodeExpression>();
            }

            internal static void Reset()
            {
                AllowInternalsSeen = false;
            }
        }
    }
}
