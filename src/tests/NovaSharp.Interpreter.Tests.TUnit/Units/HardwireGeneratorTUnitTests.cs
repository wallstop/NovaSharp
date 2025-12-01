#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.CodeDom;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Hardwire;
    using NovaSharp.Hardwire.Generators;
    using NovaSharp.Hardwire.Languages;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Tests.Units;

    public sealed class HardwireGeneratorTUnitTests
    {
        [global::TUnit.Core.Test]
        public Task BuildCodeModelRegistersCodeExpressions()
        {
            return RunWithRegistryLock(async () =>
            {
                string managedType = "Hardwire.Tests.Generator." + Guid.NewGuid().ToString("N");
                RegisteringGenerator generator = new(managedType);
                HardwireGeneratorRegistry.Register(generator);

                IHardwireGenerator resolved = HardwireGeneratorRegistry.GetGenerator(managedType);
                await Assert.That(resolved).IsSameReferenceAs(generator);

                string source = GenerateSourceFor(managedType);

                await Assert.That(generator.InvocationCount).IsEqualTo(1);
                await Assert.That(source).Contains("UserData.RegisterType(typeof(object));");
            });
        }

        [global::TUnit.Core.Test]
        public Task AllowInternalsFlagPropagatesToGeneratorContext()
        {
            return RunWithRegistryLock(async () =>
            {
                string managedType =
                    "Hardwire.Tests.AllowInternals." + Guid.NewGuid().ToString("N");
                AllowInternalsProbeGenerator generatorInstance = new(managedType);
                HardwireGeneratorRegistry.Register(generatorInstance);

                HardwireGenerator generator = CreateGenerator();
                generator.AllowInternals = true;

                BuildCodeModel(generator, managedType);

                await Assert.That(generatorInstance.AllowInternalsSeen).IsTrue();
            });
        }

        [global::TUnit.Core.Test]
        public Task MethodMemberDescriptorGeneratorEmitsDefaultDispatchAndRefHandling()
        {
            return RunWithRegistryLock(async () =>
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

                await Assert.That(source).Contains("refp_0");
                await Assert.That(source).Contains("argscount <= 1");
                await Assert.That(source).Contains("DynValue.NewTuple");
            });
        }

        [global::TUnit.Core.Test]
        public Task MethodMemberDescriptorGeneratorHandlesPropertySetterSpecialName()
        {
            return RunWithRegistryLock(async () =>
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

                await Assert.That(source).Contains("tmp.Value = ((int)(pars[0]));");
                await Assert.That(source).Contains("return null;");
            });
        }

        [global::TUnit.Core.Test]
        public Task MethodMemberDescriptorGeneratorLogsErrorForStaticIndexer()
        {
            return RunWithRegistryLock(async () =>
            {
                MethodMemberDescriptorGenerator methodGenerator = new();
                HardwireGeneratorRegistry.Register(methodGenerator);

                CapturingCodeGenerationLogger logger = new();
                HardwireGenerator generator = new(
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

                await Assert
                    .That(source)
                    .Contains("Static indexers are not supported by hardwired descriptors");
                await Assert
                    .That(
                        logger.Warnings.Any(error =>
                            ContainsOrdinal(
                                error,
                                "Static indexers are not supported by hardwired descriptors."
                            )
                        )
                    )
                    .IsTrue();
            });
        }

        [global::TUnit.Core.Test]
        public Task BuildCodeModelThrowsWhenTableNull()
        {
            return RunWithRegistryLock(() =>
            {
                HardwireGenerator generator = CreateGenerator();
                Assert.Throws<ArgumentNullException>(() => generator.BuildCodeModel(null));
            });
        }

        [global::TUnit.Core.Test]
        public Task ConstructorThrowsWhenNamespaceMissing()
        {
            return RunWithRegistryLock(() =>
            {
                CapturingCodeGenerationLogger logger = new();

                Assert.Throws<ArgumentException>(() =>
                {
                    HardwireGenerator generator = new(null, "EntryPoint", logger);
                    _ = generator.AllowInternals;
                });
                Assert.Throws<ArgumentException>(() =>
                {
                    HardwireGenerator generator = new("   ", "EntryPoint", logger);
                    _ = generator.AllowInternals;
                });
            });
        }

        [global::TUnit.Core.Test]
        public Task ConstructorThrowsWhenEntryClassMissing()
        {
            return RunWithRegistryLock(() =>
            {
                CapturingCodeGenerationLogger logger = new();

                Assert.Throws<ArgumentException>(() =>
                {
                    HardwireGenerator generator = new("Namespace", null, logger);
                    _ = generator.AllowInternals;
                });
                Assert.Throws<ArgumentException>(() =>
                {
                    HardwireGenerator generator = new("Namespace", "   ", logger);
                    _ = generator.AllowInternals;
                });
            });
        }

        [global::TUnit.Core.Test]
        public Task ConstructorThrowsWhenLoggerNull()
        {
            return RunWithRegistryLock(() =>
            {
                Assert.Throws<ArgumentNullException>(() =>
                {
                    HardwireGenerator generator = new("Namespace", "EntryPoint", null);
                    _ = generator.AllowInternals;
                });
            });
        }

        [global::TUnit.Core.Test]
        public Task GenerateSourceThrowsWhenLanguageMissingProvider()
        {
            return RunWithRegistryLock(async () =>
            {
                HardwireCodeGenerationLanguage stubLanguage = new StubLanguageWithoutProvider();
                HardwireGenerator generator = CreateGenerator(language: stubLanguage);
                BuildCodeModel(generator, StubManagedTypeValue);

                InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                    generator.GenerateSourceCode()
                )!;
                await Assert.That(exception.Message).Contains("CodeDom provider");
            });
        }

        private static readonly SemaphoreSlim RegistryGate = new(1, 1);

        private static Task RunWithRegistryLock(Action action)
        {
            return RunWithRegistryLock(() =>
            {
                action();
                return Task.CompletedTask;
            });
        }

        private static async Task RunWithRegistryLock(Func<Task> action)
        {
            await RegistryGate.WaitAsync().ConfigureAwait(false);
            try
            {
                ResetGenerators();
                await action().ConfigureAwait(false);
            }
            finally
            {
                RegistryGate.Release();
            }
        }

        private static void ResetGenerators()
        {
            HardwireGeneratorRegistry.Reset();
            HardwireGeneratorRegistry.RegisterPredefined();
        }

        private static string GenerateSourceFor(string managedType)
        {
            HardwireGenerator generator = CreateGenerator();
            BuildCodeModel(generator, managedType);
            return generator.GenerateSourceCode();
        }

        private static HardwireGenerator CreateGenerator(
            ICodeGenerationLogger logger = null,
            HardwireCodeGenerationLanguage language = null
        )
        {
            return new HardwireGenerator(
                "NovaSharp.Tests.Generated",
                "EntryPoint",
                logger ?? new CapturingCodeGenerationLogger(),
                language ?? HardwireCodeGenerationLanguage.CSharp
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

        private sealed class StubLanguageWithoutProvider : HardwireCodeGenerationLanguage
        {
            public override string Name => "Stub";

            public override System.CodeDom.Compiler.CodeDomProvider CodeDomProvider => null;

            public override string[] GetInitialComment() => Array.Empty<string>();

            public override CodeExpression UnaryPlus(CodeExpression expression) => expression;

            public override CodeExpression UnaryNegation(CodeExpression expression) => expression;

            public override CodeExpression UnaryLogicalNot(CodeExpression expression) => expression;

            public override CodeExpression UnaryOneComplement(CodeExpression expression) =>
                expression;

            public override CodeExpression UnaryIncrement(CodeExpression expression) => expression;

            public override CodeExpression UnaryDecrement(CodeExpression expression) => expression;

            public override CodeExpression BinaryXor(CodeExpression left, CodeExpression right) =>
                left;

            public override CodeExpression CreateMultidimensionalArray(
                string elementType,
                params CodeExpression[] lengths
            ) => null;
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

        private const string StubManagedTypeValue =
            "NovaSharp.Interpreter.Interop.StandardDescriptors.HardwiredDescriptors.HardwiredMemberDescriptor";

        private sealed class RegisteringGenerator : IHardwireGenerator
        {
            private readonly string _managedType;

            internal RegisteringGenerator(string managedType)
            {
                _managedType = managedType;
            }

            internal int InvocationCount { get; private set; }

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
        }

        private sealed class AllowInternalsProbeGenerator : IHardwireGenerator
        {
            private readonly string _managedType;

            internal AllowInternalsProbeGenerator(string managedType)
            {
                _managedType = managedType;
            }

            internal bool AllowInternalsSeen { get; private set; }

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
        }

        private static bool ContainsOrdinal(string source, string value)
        {
            return source != null && source.Contains(value, StringComparison.Ordinal);
        }
    }
}
#pragma warning restore CA2007
