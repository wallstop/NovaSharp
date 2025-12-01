#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.Reflection;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop.BasicDescriptors;

    public sealed class ParameterDescriptorTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ConstructorFromParameterInfoAppliesMetadata()
        {
            ParameterInfo optional = typeof(SampleTarget)
                .GetMethod(nameof(SampleTarget.Optional))!
                .GetParameters()[1];
            ParameterDescriptor descriptor = new(optional);

            await Assert.That(descriptor.Name).IsEqualTo("text");
            await Assert.That(descriptor.Type).IsEqualTo(typeof(string));
            await Assert.That(descriptor.HasDefaultValue).IsTrue();
            await Assert.That(descriptor.DefaultValue).IsEqualTo("fallback");
            await Assert.That(descriptor.IsOut).IsFalse();
            await Assert.That(descriptor.IsRef).IsFalse();
            await Assert.That(descriptor.IsVarArgs).IsFalse();
            await Assert.That(descriptor.HasBeenRestricted).IsFalse();
        }

        [global::TUnit.Core.Test]
        public void ConstructorFromParameterInfoThrowsWhenNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                ParameterDescriptor descriptor = new((ParameterInfo)null);
                _ = descriptor.Name;
            });
        }

        [global::TUnit.Core.Test]
        public async Task ConstructorDetectsVarArgsAndByRef()
        {
            ParameterInfo varArgs = typeof(SampleTarget)
                .GetMethod(nameof(SampleTarget.VarArgs))!
                .GetParameters()[0];
            ParameterInfo byRef = typeof(SampleTarget)
                .GetMethod(nameof(SampleTarget.RefMethod))!
                .GetParameters()[0];
            ParameterInfo outParam = typeof(SampleTarget)
                .GetMethod(nameof(SampleTarget.OutMethod))!
                .GetParameters()[0];

            ParameterDescriptor varArgsDescriptor = new(varArgs);
            ParameterDescriptor byRefDescriptor = new(byRef);
            ParameterDescriptor outDescriptor = new(outParam);

            await Assert.That(varArgsDescriptor.IsVarArgs).IsTrue();
            await Assert.That(byRefDescriptor.IsRef).IsTrue();
            await Assert.That(outDescriptor.IsOut).IsTrue();
        }

        [global::TUnit.Core.Test]
        public void PrepareForWiringThrowsWhenTableIsNull()
        {
            ParameterDescriptor descriptor = new("value", typeof(object));

            Assert.Throws<ArgumentNullException>(() => descriptor.PrepareForWiring(null));
        }

        [global::TUnit.Core.Test]
        public async Task RestrictTypeTracksOriginalType()
        {
            ParameterDescriptor descriptor = new("value", typeof(object));

            descriptor.RestrictType(typeof(string));

            await Assert.That(descriptor.Type).IsEqualTo(typeof(string));
            await Assert.That(descriptor.OriginalType).IsEqualTo(typeof(object));
            await Assert.That(descriptor.HasBeenRestricted).IsTrue();
        }

        [global::TUnit.Core.Test]
        public void RestrictTypeThrowsWhenNotAssignable()
        {
            ParameterDescriptor descriptor = new("value", typeof(string));

            Assert.Throws<InvalidOperationException>(() => descriptor.RestrictType(typeof(int)));
        }

        [global::TUnit.Core.Test]
        public void RestrictTypeThrowsOnRefOrVarArgs()
        {
            ParameterDescriptor refDescriptor = new(
                "ref",
                typeof(string).MakeByRefType(),
                isRef: true
            );
            ParameterDescriptor varArgsDescriptor = new(
                "varargs",
                typeof(string[]),
                isVarArgs: true
            );

            Assert.Throws<InvalidOperationException>(() =>
                refDescriptor.RestrictType(typeof(string))
            );
            Assert.Throws<InvalidOperationException>(() =>
                varArgsDescriptor.RestrictType(typeof(string))
            );
        }

        [global::TUnit.Core.Test]
        public async Task PrepareForWiringWritesTableEntries()
        {
            Script script = new();
            Table table = new(script);
            ParameterDescriptor descriptor = new(
                name: "value",
                type: typeof(object),
                hasDefaultValue: true,
                defaultValue: "abc",
                isOut: false,
                isRef: false,
                isVarArgs: false
            );

            descriptor.RestrictType(typeof(string));
            descriptor.PrepareForWiring(table);

            await Assert.That(table.RawGet("name").String).IsEqualTo("value");
            await Assert.That(table.RawGet("type").String).IsEqualTo(typeof(string).FullName);
            await Assert.That(table.RawGet("origtype").String).IsEqualTo(typeof(object).FullName);
            await Assert.That(table.RawGet("default").Boolean).IsTrue();
            await Assert.That(table.RawGet("restricted").Boolean).IsTrue();
            await Assert.That(table.RawGet("out").Boolean).IsFalse();
            await Assert.That(table.RawGet("ref").Boolean).IsFalse();
            await Assert.That(table.RawGet("varargs").Boolean).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task ConstructorWithTypeRestrictionAppliesRestriction()
        {
            ParameterDescriptor descriptor = new(
                name: "value",
                type: typeof(object),
                hasDefaultValue: false,
                defaultValue: null,
                isOut: false,
                isRef: false,
                isVarArgs: false,
                typeRestriction: typeof(string)
            );

            await Assert.That(descriptor.Type).IsEqualTo(typeof(string));
            await Assert.That(descriptor.OriginalType).IsEqualTo(typeof(object));
            await Assert.That(descriptor.HasBeenRestricted).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task PrepareForWiringUsesElementTypeForByRefParameters()
        {
            Script script = new();
            Table table = new(script);
            ParameterDescriptor descriptor = new(
                name: "value",
                type: typeof(int).MakeByRefType(),
                hasDefaultValue: false,
                defaultValue: null,
                isOut: false,
                isRef: true,
                isVarArgs: false
            );

            descriptor.PrepareForWiring(table);

            await Assert.That(table.RawGet("type").String).IsEqualTo(typeof(int).FullName);
            await Assert.That(table.RawGet("origtype").String).IsEqualTo(typeof(int).FullName);
            await Assert.That(table.RawGet("ref").Boolean).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task OriginalTypeMatchesTypeWhenNotRestricted()
        {
            ParameterDescriptor descriptor = new("value", typeof(double));

            await Assert.That(descriptor.HasBeenRestricted).IsFalse();
            await Assert.That(descriptor.OriginalType).IsEqualTo(typeof(double));
        }

        [global::TUnit.Core.Test]
        public async Task ToStringIncludesTypeNameAndDefaultFlag()
        {
            ParameterDescriptor descriptor = new(
                "count",
                typeof(int),
                hasDefaultValue: true,
                defaultValue: 10
            );

            await Assert.That(descriptor.ToString()).IsEqualTo("Int32 count = ...");
        }

        private static class SampleTarget
        {
            public static void Optional(int value, string text = "fallback") { }

            public static void VarArgs(params string[] values) { }

            public static void RefMethod(ref string value) { }

            public static void OutMethod(out string value)
            {
                value = string.Empty;
            }
        }
    }
}
#pragma warning restore CA2007
