namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Reflection;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop.BasicDescriptors;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ParameterDescriptorTests
    {
        [Test]
        public void ConstructorFromParameterInfoAppliesMetadata()
        {
            ParameterInfo optional = typeof(SampleTarget)
                .GetMethod(nameof(SampleTarget.Optional))!
                .GetParameters()[1];
            ParameterDescriptor descriptor = new(optional);

            Assert.Multiple(() =>
            {
                Assert.That(descriptor.Name, Is.EqualTo("text"));
                Assert.That(descriptor.Type, Is.EqualTo(typeof(string)));
                Assert.That(descriptor.HasDefaultValue, Is.True);
                Assert.That(descriptor.DefaultValue, Is.EqualTo("fallback"));
                Assert.That(descriptor.IsOut, Is.False);
                Assert.That(descriptor.IsRef, Is.False);
                Assert.That(descriptor.IsVarArgs, Is.False);
                Assert.That(descriptor.HasBeenRestricted, Is.False);
            });
        }

        [Test]
        public void ConstructorDetectsVarArgsAndByRef()
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

            Assert.Multiple(() =>
            {
                Assert.That(varArgsDescriptor.IsVarArgs, Is.True);
                Assert.That(byRefDescriptor.IsRef, Is.True);
                Assert.That(outDescriptor.IsOut, Is.True);
            });
        }

        [Test]
        public void RestrictTypeTracksOriginalType()
        {
            ParameterDescriptor descriptor = new("value", typeof(object));

            descriptor.RestrictType(typeof(string));

            Assert.Multiple(() =>
            {
                Assert.That(descriptor.Type, Is.EqualTo(typeof(string)));
                Assert.That(descriptor.OriginalType, Is.EqualTo(typeof(object)));
                Assert.That(descriptor.HasBeenRestricted, Is.True);
            });
        }

        [Test]
        public void RestrictTypeThrowsWhenNotAssignable()
        {
            ParameterDescriptor descriptor = new("value", typeof(string));

            Assert.That(
                () => descriptor.RestrictType(typeof(int)),
                Throws.InvalidOperationException.With.Message.Contains("restriction")
            );
        }

        [Test]
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

            Assert.Multiple(() =>
            {
                Assert.That(
                    () => refDescriptor.RestrictType(typeof(string)),
                    Throws.InvalidOperationException.With.Message.Contains("ref/out")
                );
                Assert.That(
                    () => varArgsDescriptor.RestrictType(typeof(string)),
                    Throws.InvalidOperationException.With.Message.Contains("varargs")
                );
            });
        }

        [Test]
        public void PrepareForWiringWritesTableEntries()
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

            Assert.Multiple(() =>
            {
                Assert.That(table.RawGet("name").String, Is.EqualTo("value"));
                Assert.That(table.RawGet("type").String, Is.EqualTo(typeof(string).FullName));
                Assert.That(table.RawGet("origtype").String, Is.EqualTo(typeof(object).FullName));
                Assert.That(table.RawGet("default").Boolean, Is.True);
                Assert.That(table.RawGet("restricted").Boolean, Is.True);
                Assert.That(table.RawGet("out").Boolean, Is.False);
                Assert.That(table.RawGet("ref").Boolean, Is.False);
                Assert.That(table.RawGet("varargs").Boolean, Is.False);
            });
        }

        [Test]
        public void ConstructorWithTypeRestrictionAppliesRestriction()
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

            Assert.Multiple(() =>
            {
                Assert.That(descriptor.Type, Is.EqualTo(typeof(string)));
                Assert.That(descriptor.OriginalType, Is.EqualTo(typeof(object)));
                Assert.That(descriptor.HasBeenRestricted, Is.True);
            });
        }

        [Test]
        public void PrepareForWiringUsesElementTypeForByRefParameters()
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

            Assert.Multiple(() =>
            {
                Assert.That(table.RawGet("type").String, Is.EqualTo(typeof(int).FullName));
                Assert.That(table.RawGet("origtype").String, Is.EqualTo(typeof(int).FullName));
                Assert.That(table.RawGet("ref").Boolean, Is.True);
            });
        }

        [Test]
        public void OriginalTypeMatchesTypeWhenNotRestricted()
        {
            ParameterDescriptor descriptor = new("value", typeof(double));

            Assert.Multiple(() =>
            {
                Assert.That(descriptor.HasBeenRestricted, Is.False);
                Assert.That(descriptor.OriginalType, Is.EqualTo(typeof(double)));
            });
        }

        [Test]
        public void ToStringIncludesTypeNameAndDefaultFlag()
        {
            ParameterDescriptor descriptor = new(
                "count",
                typeof(int),
                hasDefaultValue: true,
                defaultValue: 10
            );

            Assert.That(descriptor.ToString(), Is.EqualTo("Int32 count = ..."));
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
