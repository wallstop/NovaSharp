namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.CodeDom;
    using System.Collections.Generic;
    using NovaSharp.Hardwire.Utils;
    using NovaSharp.Interpreter.DataTypes;
    using NUnit.Framework;

    [TestFixture]
    public sealed class HardwireParameterDescriptorTests
    {
        [Test]
        public void ConstructorMapsFlagsAndTypes()
        {
            HardwireParameterDescriptor descriptor = CreateDescriptor(hasDefault: true);

            Assert.That(descriptor.ParamType, Is.EqualTo(typeof(string).FullName));
            Assert.That(descriptor.HasDefaultValue, Is.True);
            Assert.That(descriptor.IsOut, Is.False);
            Assert.That(descriptor.IsRef, Is.False);
            Assert.That(descriptor.Expression, Is.TypeOf<CodeObjectCreateExpression>());
        }

        [Test]
        public void SetTempVarCapturesVariableForOutParameter()
        {
            HardwireParameterDescriptor descriptor = CreateDescriptor(isOut: true);

            descriptor.SetTempVar("tmp");

            Assert.That(descriptor.TempVarName, Is.EqualTo("tmp"));
        }

        [Test]
        public void SetTempVarThrowsForByValueParameter()
        {
            HardwireParameterDescriptor descriptor = CreateDescriptor();

            Assert.That(
                () => descriptor.SetTempVar("tmp"),
                Throws.InvalidOperationException.With.Message.Contains("byval")
            );
        }

        [Test]
        public void LoadDescriptorsFromTableCreatesSequentialList()
        {
            Table list = new(owner: null);
            list.Append(DynValue.NewTable(CreateParameterTable("first", isRef: true)));
            list.Append(DynValue.NewTable(CreateParameterTable("second", isOut: true)));

            List<HardwireParameterDescriptor> descriptors =
                HardwireParameterDescriptor.LoadDescriptorsFromTable(list);

            Assert.That(descriptors, Has.Count.EqualTo(2));
            Assert.That(descriptors[0].IsRef, Is.True);
            Assert.That(descriptors[1].IsOut, Is.True);
        }

        private static HardwireParameterDescriptor CreateDescriptor(
            bool hasDefault = false,
            bool isOut = false,
            bool isRef = false
        )
        {
            Table parameterTable = CreateParameterTable(
                "value",
                hasDefault: hasDefault,
                isOut: isOut,
                isRef: isRef
            );

            return new HardwireParameterDescriptor(parameterTable);
        }

        private static Table CreateParameterTable(
            string name,
            bool hasDefault = false,
            bool isOut = false,
            bool isRef = false
        )
        {
            Table table = new(owner: null);
            table.Set("name", DynValue.NewString(name));
            table.Set("origtype", DynValue.NewString(typeof(string).FullName));
            table.Set("default", DynValue.NewBoolean(hasDefault));
            table.Set("out", DynValue.NewBoolean(isOut));
            table.Set("ref", DynValue.NewBoolean(isRef));
            table.Set("varargs", DynValue.NewBoolean(false));
            table.Set("type", DynValue.NewString(typeof(string).FullName));
            table.Set("restricted", DynValue.NewBoolean(false));

            return table;
        }
    }
}
