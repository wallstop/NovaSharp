namespace NovaSharp.Interpreter.Tests.Units
{
    using System.CodeDom;
    using System.Linq;
    using NovaSharp.Hardwire;
    using NovaSharp.Hardwire.Generators;
    using NovaSharp.Interpreter.DataTypes;
    using NUnit.Framework;

    [TestFixture]
    public sealed class StandardUserDataDescriptorGeneratorTests
    {
        [Test]
        public void GenerateAddsMembersAndMetaMembers()
        {
            const string userDataType = "NovaSharp.Tests.SampleUserData";
            const string managedType =
                "NovaSharp.Interpreter.Interop.StandardDescriptors.StandardUserDataDescriptor";

            StubMemberGenerator stubGenerator = new();
            HardwireGeneratorRegistry.Register(stubGenerator);

            Table descriptorTable = new(owner: null);
            descriptorTable.Set("class", DynValue.NewString(managedType));
            descriptorTable.Set("$key", DynValue.NewString(userDataType));
            descriptorTable.Set("members", DynValue.NewTable(CreateMemberTable("Foo")));
            descriptorTable.Set("metamembers", DynValue.NewTable(CreateMemberTable("__index")));

            StandardUserDataDescriptorGenerator generator = new();
            HardwireCodeGenerationContext context = HardwireTestUtilities.CreateContext();
            CodeTypeMemberCollection members = new();

            CodeExpression[] result = generator.Generate(descriptorTable, context, members);

            Assert.That(result, Has.Length.EqualTo(1));
            CodeObjectCreateExpression createExpression = AssertCast<CodeObjectCreateExpression>(
                result[0]
            );
            Assert.That(createExpression.CreateType.BaseType, Does.StartWith("TYPE_"));

            CodeTypeDeclaration generatedClass = members.OfType<CodeTypeDeclaration>().Single();

            CodeConstructor ctor = generatedClass.Members.OfType<CodeConstructor>().Single();
            Assert.That(
                ctor.BaseConstructorArgs[0],
                Is.TypeOf<CodeTypeOfExpression>()
                    .With.Property(nameof(CodeTypeOfExpression.Type))
                    .Property(nameof(CodeTypeReference.BaseType))
                    .EqualTo(userDataType)
            );

            CodeMethodInvokeExpression[] calls = ctor
                .Statements.OfType<CodeExpressionStatement>()
                .Select(s => s.Expression)
                .OfType<CodeMethodInvokeExpression>()
                .ToArray();

            Assert.That(calls.Length, Is.EqualTo(2));
            Assert.That(calls[0].Method.MethodName, Is.EqualTo("AddMember"));
            Assert.That(
                calls[0].Parameters[0],
                Is.TypeOf<CodePrimitiveExpression>().With.Property("Value").EqualTo("Foo")
            );
            Assert.That(calls[1].Method.MethodName, Is.EqualTo("AddMetaMember"));
            Assert.That(
                calls[1].Parameters[0],
                Is.TypeOf<CodePrimitiveExpression>().With.Property("Value").EqualTo("__index")
            );
        }

        private static Table CreateMemberTable(string name)
        {
            Table table = new(owner: null);
            Table descriptor = new(owner: null);
            descriptor.Set("class", DynValue.NewString(StubMemberGenerator.ManagedTypeValue));
            table.Set(name, DynValue.NewTable(descriptor));
            return table;
        }

        private static T AssertCast<T>(CodeExpression expression)
            where T : CodeExpression
        {
            Assert.That(expression, Is.TypeOf<T>());
            return (T)expression;
        }

        private sealed class StubMemberGenerator : IHardwireGenerator
        {
            internal const string ManagedTypeValue =
                "NovaSharp.Interpreter.Interop.StandardDescriptors.HardwiredDescriptors.HardwiredMemberDescriptor";

            public string ManagedType => ManagedTypeValue;

            public CodeExpression[] Generate(
                Table table,
                HardwireCodeGenerationContext generatorContext,
                CodeTypeMemberCollection members
            )
            {
                return new CodeExpression[] { new CodePrimitiveExpression("stub-member") };
            }
        }
    }
}
