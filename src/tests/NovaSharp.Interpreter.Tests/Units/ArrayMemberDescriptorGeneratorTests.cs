namespace NovaSharp.Interpreter.Tests.Units
{
    using System.CodeDom;
    using System.Linq;
    using NovaSharp.Hardwire;
    using NovaSharp.Hardwire.Generators;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop.BasicDescriptors;
    using NovaSharp.Interpreter.Interop.StandardDescriptors.MemberDescriptors;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ArrayMemberDescriptorGeneratorTests
    {
        [Test]
        public void GenerateWithoutParametersCreatesDescriptorClass()
        {
            ArrayMemberDescriptorGenerator generator = new();
            HardwireCodeGenerationContext context = HardwireTestUtilities.CreateContext();

            Table descriptorTable = new(owner: null);
            descriptorTable.Set("name", DynValue.NewString("Items"));
            descriptorTable.Set("setter", DynValue.True);

            CodeTypeMemberCollection members = new();
            CodeExpression[] expressions = generator.Generate(descriptorTable, context, members);

            Assert.That(expressions, Has.Length.EqualTo(1));
            CodeObjectCreateExpression ctorExpression = AssertCast<CodeObjectCreateExpression>(
                expressions[0]
            );
            Assert.That(ctorExpression.CreateType.BaseType, Does.StartWith("AIDX_"));

            CodeTypeDeclaration generatedClass = members.OfType<CodeTypeDeclaration>().Single();
            Assert.That(
                generatedClass.BaseTypes[0].BaseType,
                Is.EqualTo(typeof(ArrayMemberDescriptor).FullName)
            );

            CodeConstructor ctor = generatedClass.Members.OfType<CodeConstructor>().Single();
            Assert.That(ctor.BaseConstructorArgs.Count, Is.EqualTo(2));
            AssertPrimitive(ctor.BaseConstructorArgs[0], "Items");
            AssertPrimitive(ctor.BaseConstructorArgs[1], true);
        }

        [Test]
        public void GenerateAddsParameterDescriptorsWhenProvided()
        {
            ArrayMemberDescriptorGenerator generator = new();
            HardwireCodeGenerationContext context = HardwireTestUtilities.CreateContext();

            Table descriptorTable = new(owner: null);
            descriptorTable.Set("name", DynValue.NewString("Entries"));
            descriptorTable.Set("setter", DynValue.False);
            descriptorTable.Set("params", DynValue.NewTable(CreateParameterList()));

            CodeTypeMemberCollection members = new();
            _ = generator.Generate(descriptorTable, context, members);

            CodeTypeDeclaration generatedClass = members.OfType<CodeTypeDeclaration>().Single();
            CodeConstructor ctor = generatedClass.Members.OfType<CodeConstructor>().Single();

            Assert.That(ctor.BaseConstructorArgs.Count, Is.EqualTo(3));
            AssertPrimitive(ctor.BaseConstructorArgs[0], "Entries");
            AssertPrimitive(ctor.BaseConstructorArgs[1], false);

            CodeArrayCreateExpression parameterArray = AssertCast<CodeArrayCreateExpression>(
                ctor.BaseConstructorArgs[2]
            );
            Assert.That(
                parameterArray.CreateType.BaseType,
                Is.EqualTo(typeof(ParameterDescriptor).FullName)
            );
            Assert.That(parameterArray.Initializers.Count, Is.EqualTo(1));
            CodeObjectCreateExpression parameterCtor = AssertCast<CodeObjectCreateExpression>(
                parameterArray.Initializers[0]
            );
            Assert.That(
                parameterCtor.CreateType.BaseType,
                Is.EqualTo(typeof(ParameterDescriptor).FullName)
            );
        }

        [Test]
        public void GenerateThrowsWhenDescriptorTableNull()
        {
            ArrayMemberDescriptorGenerator generator = new();

            Assert.That(
                () => generator.Generate(null, HardwireTestUtilities.CreateContext(), new()),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("table")
            );
        }

        [Test]
        public void GenerateThrowsWhenContextNull()
        {
            ArrayMemberDescriptorGenerator generator = new();
            Table descriptorTable = new(owner: null);

            Assert.That(
                () => generator.Generate(descriptorTable, null, new()),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("generatorContext")
            );
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

        private static void AssertPrimitive(CodeExpression expression, object expected)
        {
            CodePrimitiveExpression primitive = AssertCast<CodePrimitiveExpression>(expression);
            Assert.That(primitive.Value, Is.EqualTo(expected));
        }

        private static T AssertCast<T>(CodeExpression expression)
            where T : CodeExpression
        {
            Assert.That(expression, Is.TypeOf<T>());
            return (T)expression;
        }
    }
}
