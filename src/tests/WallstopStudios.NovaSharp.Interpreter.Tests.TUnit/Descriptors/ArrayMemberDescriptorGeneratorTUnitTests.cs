namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Descriptors
{
    using System;
    using System.CodeDom;
    using System.Linq;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Hardwire;
    using WallstopStudios.NovaSharp.Hardwire.Generators;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Interop.BasicDescriptors;
    using WallstopStudios.NovaSharp.Interpreter.Interop.StandardDescriptors.MemberDescriptors;
    using WallstopStudios.NovaSharp.Interpreter.Tests.Units;

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

            await Assert.That(expressions.Length).IsEqualTo(1).ConfigureAwait(false);
            CodeObjectCreateExpression ctorExpression = AssertCast<CodeObjectCreateExpression>(
                expressions[0]
            );
            await Assert
                .That(
                    ctorExpression.CreateType.BaseType.StartsWith("AIDX_", StringComparison.Ordinal)
                )
                .IsTrue()
                .ConfigureAwait(false);

            CodeTypeDeclaration generatedClass = members.OfType<CodeTypeDeclaration>().Single();
            await Assert
                .That(generatedClass.BaseTypes[0].BaseType)
                .IsEqualTo(typeof(ArrayMemberDescriptor).FullName)
                .ConfigureAwait(false);

            CodeConstructor ctor = generatedClass.Members.OfType<CodeConstructor>().Single();
            await Assert.That(ctor.BaseConstructorArgs.Count).IsEqualTo(2).ConfigureAwait(false);
            await AssertPrimitive(ctor.BaseConstructorArgs[0], "Items").ConfigureAwait(false);
            await AssertPrimitive(ctor.BaseConstructorArgs[1], true).ConfigureAwait(false);
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

            await Assert.That(ctor.BaseConstructorArgs.Count).IsEqualTo(3).ConfigureAwait(false);
            await AssertPrimitive(ctor.BaseConstructorArgs[0], "Entries").ConfigureAwait(false);
            await AssertPrimitive(ctor.BaseConstructorArgs[1], false).ConfigureAwait(false);

            CodeArrayCreateExpression parameterArray = AssertCast<CodeArrayCreateExpression>(
                ctor.BaseConstructorArgs[2]
            );
            await Assert
                .That(parameterArray.CreateType.BaseType)
                .IsEqualTo(typeof(ParameterDescriptor).FullName)
                .ConfigureAwait(false);
            await Assert.That(parameterArray.Initializers.Count).IsEqualTo(1).ConfigureAwait(false);
            CodeObjectCreateExpression parameterCtor = AssertCast<CodeObjectCreateExpression>(
                parameterArray.Initializers[0]
            );
            await Assert
                .That(parameterCtor.CreateType.BaseType)
                .IsEqualTo(typeof(ParameterDescriptor).FullName)
                .ConfigureAwait(false);
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

            await Assert.That(exception.ParamName).IsEqualTo("table").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GenerateThrowsWhenContextNull()
        {
            ArrayMemberDescriptorGenerator generator = new();
            Table descriptorTable = new(owner: null);

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                generator.Generate(descriptorTable, null, new CodeTypeMemberCollection())
            );

            await Assert
                .That(exception.ParamName)
                .IsEqualTo("generatorContext")
                .ConfigureAwait(false);
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
            await Assert.That(primitive.Value).IsEqualTo(expected).ConfigureAwait(false);
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
