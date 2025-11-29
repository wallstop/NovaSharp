#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Descriptors
{
    using System;
    using System.CodeDom;
    using System.Linq;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Hardwire;
    using NovaSharp.Hardwire.Generators;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Tests;
    using NovaSharp.Interpreter.Tests.Units;

    public sealed class StandardUserDataDescriptorGeneratorTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task GenerateAddsMembersAndMetaMembers()
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

            await Assert.That(result.Length).IsEqualTo(1);
            CodeObjectCreateExpression createExpression = AssertCast<CodeObjectCreateExpression>(
                result[0]
            );
            await Assert.That(createExpression.CreateType.BaseType).StartsWith("TYPE_");

            CodeTypeDeclaration generatedClass = members.OfType<CodeTypeDeclaration>().Single();
            CodeConstructor ctor = generatedClass.Members.OfType<CodeConstructor>().Single();

            CodeTypeOfExpression typeArgument = AssertCast<CodeTypeOfExpression>(
                ctor.BaseConstructorArgs[0]
            );
            await Assert.That(typeArgument.Type.BaseType).IsEqualTo(userDataType);

            CodeMethodInvokeExpression[] calls = ctor
                .Statements.OfType<CodeExpressionStatement>()
                .Select(s => s.Expression)
                .OfType<CodeMethodInvokeExpression>()
                .ToArray();

            await Assert.That(calls.Length).IsEqualTo(2);
            await Assert.That(calls[0].Method.MethodName).IsEqualTo("AddMember");
            CodePrimitiveExpression memberName = AssertCast<CodePrimitiveExpression>(
                calls[0].Parameters[0]
            );
            await Assert.That(memberName.Value).IsEqualTo("Foo");

            await Assert.That(calls[1].Method.MethodName).IsEqualTo("AddMetaMember");
            CodePrimitiveExpression metaMemberName = AssertCast<CodePrimitiveExpression>(
                calls[1].Parameters[0]
            );
            await Assert.That(metaMemberName.Value).IsEqualTo("__index");
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
            if (expression is T cast)
            {
                return cast;
            }

            throw new InvalidOperationException(
                $"Expected {typeof(T).Name} but received {expression?.GetType().Name ?? "null"}."
            );
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
#pragma warning restore CA2007
