namespace NovaSharp.Interpreter.Tests.Units
{
    using System.Collections.Generic;
    using System.IO;
    using DataTypes;
    using NovaSharp;
    using NUnit.Framework;

    [TestFixture]
    public sealed class SymbolRefTests
    {
        [Test]
        public void GlobalSymbolSerializationRoundtrips()
        {
            SymbolRef original = SymbolRef.Global("foo", SymbolRef.DefaultEnv);
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            original.WriteBinary(writer);
            original.WriteBinaryEnv(
                writer,
                new Dictionary<SymbolRef, int> { [SymbolRef.DefaultEnv] = 0, [original] = 1 }
            );

            writer.Flush();
            stream.Position = 0;

            using BinaryReader reader = new(stream);
            SymbolRef restored = SymbolRef.ReadBinary(reader);
            restored.ReadBinaryEnv(reader, new[] { SymbolRef.DefaultEnv, restored });

            Assert.Multiple(() =>
            {
                Assert.That(restored.Type, Is.EqualTo(SymbolRefType.Global));
                Assert.That(restored.Name, Is.EqualTo("foo"));
                Assert.That(restored.Environment, Is.EqualTo(SymbolRef.DefaultEnv));
            });
        }

        [Test]
        public void LocalSymbolPreservesIndexAndName()
        {
            SymbolRef original = SymbolRef.Local("bar", 3);

            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            original.WriteBinary(writer);
            original.WriteBinaryEnv(writer, new Dictionary<SymbolRef, int>());
            writer.Flush();
            stream.Position = 0;

            using BinaryReader reader = new(stream);
            SymbolRef restored = SymbolRef.ReadBinary(reader);
            restored.ReadBinaryEnv(reader, new SymbolRef[] { restored });

            Assert.Multiple(() =>
            {
                Assert.That(restored.Type, Is.EqualTo(SymbolRefType.Local));
                Assert.That(restored.Index, Is.EqualTo(3));
                Assert.That(restored.Name, Is.EqualTo("bar"));
                Assert.That(restored.Environment, Is.Null);
            });
        }

        [Test]
        public void LocalSymbolExposesConstAndToBeClosedFlags()
        {
            SymbolRef symbol = SymbolRef.Local(
                "constClose",
                7,
                SymbolRefAttributes.Const | SymbolRefAttributes.ToBeClosed
            );

            Assert.Multiple(() =>
            {
                Assert.That(symbol.IsConst, Is.True);
                Assert.That(symbol.IsToBeClosed, Is.True);
            });
        }

        [Test]
        public void ToStringReflectsSymbolType()
        {
            SymbolRef env = SymbolRef.DefaultEnv;
            SymbolRef global = SymbolRef.Global("foo", env);
            SymbolRef local = SymbolRef.Local("bar", 2);

            Assert.Multiple(() =>
            {
                Assert.That(env.ToString(), Is.EqualTo("(default _ENV)"));
                Assert.That(global.ToString(), Does.Contain("foo"));
                Assert.That(local.ToString(), Does.Contain("bar"));
            });
        }

        [Test]
        public void UpvalueSymbolSerializationRetainsIndex()
        {
            SymbolRef original = SymbolRef.Upvalue("baz", 5);

            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            original.WriteBinary(writer);
            original.WriteBinaryEnv(writer, new Dictionary<SymbolRef, int>());
            writer.Flush();
            stream.Position = 0;

            using BinaryReader reader = new(stream);
            SymbolRef restored = SymbolRef.ReadBinary(reader);
            restored.ReadBinaryEnv(reader, new SymbolRef[] { restored });

            Assert.Multiple(() =>
            {
                Assert.That(restored.Type, Is.EqualTo(SymbolRefType.Upvalue));
                Assert.That(restored.Index, Is.EqualTo(5));
                Assert.That(restored.Name, Is.EqualTo("baz"));
                Assert.That(restored.Environment, Is.Null);
            });
        }
    }
}
