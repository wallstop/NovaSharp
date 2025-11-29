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
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop.BasicDescriptors;
    using NovaSharp.Interpreter.Interop.StandardDescriptors.MemberDescriptors;
    using NovaSharp.Interpreter.Tests.Units;

    public sealed class ArrayMemberDescriptorGeneratorTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task GenerateWithoutParametersCreatesDescriptorClass()
        {
            ArrayMemberDescriptorGenerator generator = new();
            HardwireCodeGenerationContext context = HardwireTestUtilities.CreateContext();

            Table descriptorTable = new(owner: null);
            descriptorTable.Set("name", DynValue.NewString("Items"));
            descriptorTable.Set("setter", DynValue.True);

            CodeTypeMemberCollection members = new CodeTypeMemberCollection();
            CodeExpression[] expressions = generator.Generate(descriptorTable, context, members);

            await Assert.That(expressions.Length).IsEqualTo(1);
            CodeObjectCreateExpression ctorExpression = AssertCast<CodeObjectCreateExpression>(
                expressions[0]
            );
            await Assert
                .That(
                    ctorExpression.CreateType.BaseType.StartsWith("AIDX_", StringComparison.Ordinal)
                )
                .IsTrue();

            CodeTypeDeclaration generatedClass = members.OfType<CodeTypeDeclaration>().Single();
            await Assert
                .That(generatedClass.BaseTypes[0].BaseType)
                .IsEqualTo(typeof(ArrayMemberDescriptor).FullName);

            CodeConstructor ctor = generatedClass.Members.OfType<CodeConstructor>().Single();
            await Assert.That(ctor.BaseConstructorArgs.Count).IsEqualTo(2);
            await AssertPrimitive(ctor.BaseConstructorArgs[0], "Items");
            await AssertPrimitive(ctor.BaseConstructorArgs[1], true);
        }

        [global::TUnit.Core.Test]
        public async Task GenerateAddsParameterDescriptorsWhenProvided()
        {
            ArrayMemberDescriptorGenerator generator = new();
            HardwireCodeGenerationContext context = HardwireTestUtilities.CreateContext();

            Table descriptorTable = new(owner: null);
            descriptorTable.Set("name", DynValue.NewString("Entries"));
            descriptorTable.Set("setter", DynValue.False);
            descriptorTable.Set("params", DynValue.NewTable(CreateParameterList()));

            CodeTypeMemberCollection members = new CodeTypeMemberCollection();
            _ = generator.Generate(descriptorTable, context, members);

            CodeTypeDeclaration generatedClass = members.OfType<CodeTypeDeclaration>().Single();
            CodeConstructor ctor = generatedClass.Members.OfType<CodeConstructor>().Single();

            await Assert.That(ctor.BaseConstructorArgs.Count).IsEqualTo(3);
            await AssertPrimitive(ctor.BaseConstructorArgs[0], "Entries");
            await AssertPrimitive(ctor.BaseConstructorArgs[1], false);

            CodeArrayCreateExpression parameterArray = AssertCast<CodeArrayCreateExpression>(
                ctor.BaseConstructorArgs[2]
            );
            await Assert
                .That(parameterArray.CreateType.BaseType)
                .IsEqualTo(typeof(ParameterDescriptor).FullName);
            await Assert.That(parameterArray.Initializers.Count).IsEqualTo(1);
            CodeObjectCreateExpression parameterCtor = AssertCast<CodeObjectCreateExpression>(
                parameterArray.Initializers[0]
            );
            await Assert
                .That(parameterCtor.CreateType.BaseType)
                .IsEqualTo(typeof(ParameterDescriptor).FullName);
        }

        [global::TUnit.Core.Test]
        public async Task GenerateThrowsWhenDescriptorTableNull()
        {
            ArrayMemberDescriptorGenerator generator = new();

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                generator.Generate(
                    null,
                    HardwireTestUtilities.CreateContext(),
                    new CodeTypeMemberCollection()
                )
            );

            await Assert.That(exception.ParamName).IsEqualTo("table");
        }

        [global::TUnit.Core.Test]
        public async Task GenerateThrowsWhenContextNull()
        {
            ArrayMemberDescriptorGenerator generator = new();
            Table descriptorTable = new(owner: null);

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                generator.Generate(descriptorTable, null, new CodeTypeMemberCollection())
            );

            await Assert.That(exception.ParamName).IsEqualTo("generatorContext");
        }

        private static Table CreateParameterList()
        {
            Table list = new(owner: null);
            Table descriptor = new(owner: null);
            descriptor.Set("name", DynValue.NewString("value"));
            descriptor.Set("origtype", DynValue.NewString("System.Int32"));
            descriptor.Set("default", DynValue.False);
            descriptor.Set("out", DynValue.False);
            descriptor.Set("ref", DynValue.False);
            descriptor.Set("varargs", DynValue.False);
            descriptor.Set("restricted", DynValue.False);
            descriptor.Set("type", DynValue.NewString("System.Int32"));
            list.Set(1, DynValue.NewTable(descriptor));
            return list;
        }

        private static async Task AssertPrimitive(CodeExpression expression, object expected)
        {
            CodePrimitiveExpression primitive = AssertCast<CodePrimitiveExpression>(expression);
            await Assert.That(primitive.Value).IsEqualTo(expected);
        }

        private static T AssertCast<T>(CodeExpression expression)
            where T : CodeExpression
        {
            if (expression is T typed)
            {
                return typed;
            }

            throw new InvalidOperationException(
                $"Expected expression of type {typeof(T).FullName} but found {expression.GetType().FullName}."
            );
        }
    }
}
#pragma warning restore CA2007
